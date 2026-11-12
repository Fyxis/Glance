using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Forms = System.Windows.Forms;
using Point = System.Windows.Point;

namespace Glance
{
    /// <summary>
    /// Bagian partial MainWindow untuk Background Loop WinRT GSMTC & Hardware Telemetry,
    /// serta sinkronisasi snapshot data ke tampilan UI.
    /// </summary>
    public partial class MainWindow
    {
        private static T WaitWinRT<T>(IAsyncOperation<T> op)
        {
            if (op == null) return default(T);
            int tries = 0;
            while (op.Status == AsyncStatus.Started && tries++ < 150)
            {
                Thread.Sleep(10);
            }
            return op.Status == AsyncStatus.Completed ? op.GetResults() : default(T);
        }

        private void BackgroundMediaAndSystemLoop()
        {
            GlobalSystemMediaTransportControlsSessionManager mgr = null;
            string lastThumbKey = "";
            int loopCounter = 0;
            int cachedBrightness = 100;
            bool cachedWifiConn = false;
            string cachedWifiSsid = "Checking...";
            int cachedWifiSig = 0;
            List<DriveStatInfo> cachedDrivesList = GetFreshDrives();
            NativeMethods.MEMORYSTATUSEX mem = new NativeMethods.MEMORYSTATUSEX();

            while (true)
            {
                try
                {
                    if (mgr == null)
                    {
                        mgr = WaitWinRT(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());
                    }

                    var session = mgr != null ? mgr.GetCurrentSession() : null;
                    if (session == null && mgr != null)
                    {
                        try
                        {
                            var sessions = mgr.GetSessions();
                            if (sessions != null && sessions.Count > 0)
                            {
                                session = sessions.FirstOrDefault(s =>
                                {
                                    try { return s.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
                                    catch { return false; }
                                }) ?? sessions.FirstOrDefault();
                            }
                        }
                        catch { }
                    }

                    if (session != null)
                    {
                        string cmd = Interlocked.Exchange(ref pendingCommand, null);
                        if (cmd == "play-pause") WaitWinRT(session.TryTogglePlayPauseAsync());
                        else if (cmd == "next") { WaitWinRT(session.TrySkipNextAsync()); Thread.Sleep(120); }
                        else if (cmd == "prev") { WaitWinRT(session.TrySkipPreviousAsync()); Thread.Sleep(120); }

                        if (pendingSeekSec.HasValue)
                        {
                            double sec = pendingSeekSec.Value;
                            pendingSeekSec = null;
                            long ticks = (long)(sec * 10000000L);
                            WaitWinRT(session.TryChangePlaybackPositionAsync(ticks));
                        }
                    }

                    MediaSnapshot snap = new MediaSnapshot();
                    if (session != null)
                    {
                        var props = WaitWinRT(session.TryGetMediaPropertiesAsync());
                        var info = session.GetPlaybackInfo();
                        var tl = session.GetTimelineProperties();

                        if (props != null)
                        {
                            snap.Title = props.Title ?? "";
                            snap.Artist = props.Artist ?? "";
                        }
                        if (info != null)
                        {
                            snap.Status = info.PlaybackStatus.ToString();
                        }
                        if (tl != null)
                        {
                            snap.RawPosition = Math.Max(0, tl.Position.TotalSeconds);
                            snap.Position = snap.RawPosition;
                            snap.Duration = Math.Max(0, tl.EndTime.TotalSeconds);
                            if (snap.Status == "Playing" && tl.LastUpdatedTime.Year > 2000)
                            {
                                double diff = (DateTimeOffset.UtcNow - tl.LastUpdatedTime).TotalSeconds;
                                if (diff >= 0 && diff < 300)
                                {
                                    snap.HasValidLastUpdated = true;
                                    snap.Position = snap.Duration > 0
                                        ? Math.Min(snap.Duration, snap.RawPosition + diff)
                                        : (snap.RawPosition + diff);
                                }
                            }
                        }

                        string thumbKey = snap.Title + "|" + snap.Artist;
                        if (thumbKey != lastThumbKey)
                        {
                            lastThumbKey = thumbKey;
                            snap.ThumbnailChanged = true;
                            if (props != null && props.Thumbnail != null)
                            {
                                try
                                {
                                    var stream = WaitWinRT(props.Thumbnail.OpenReadAsync());
                                    if (stream != null && stream.Size > 0)
                                    {
                                        var reader = new DataReader(stream.GetInputStreamAt(0));
                                        uint loaded = WaitWinRT(reader.LoadAsync((uint)stream.Size));
                                        byte[] bytes = new byte[loaded];
                                        reader.ReadBytes(bytes);
                                        snap.ThumbnailBytes = bytes;
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    else if (lastThumbKey != "")
                    {
                        lastThumbKey = "";
                        snap.ThumbnailChanged = true;
                    }

                    var power = Forms.SystemInformation.PowerStatus;
                    int batPct = (power.BatteryLifePercent >= 0 && power.BatteryLifePercent <= 1.0f)
                        ? (int)Math.Round(power.BatteryLifePercent * 100)
                        : 100;
                    bool isCharging = (power.PowerLineStatus == Forms.PowerLineStatus.Online);

                    int cpuPct = SampleCpuPercent();

                    NativeMethods.GlobalMemoryStatusEx(mem);
                    int ramPct = (int)mem.dwMemoryLoad;
                    double totalRamGB = mem.ullTotalPhys / (1024.0 * 1024 * 1024);
                    double usedRamGB = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024 * 1024);

                    if (isExpanded)
                    {
                        if (loopCounter % 15 == 0 || cachedDrivesList == null || cachedDrivesList.Count == 0)
                        {
                            cachedDrivesList = GetFreshDrives();
                        }

                        if (loopCounter % 4 == 0)
                        {
                            if (!isDraggingBright)
                            {
                                cachedBrightness = HardwareController.GetScreenBrightness();
                            }
                            HardwareController.GetWifiStatus(out cachedWifiConn, out cachedWifiSsid, out cachedWifiSig);
                        }
                    }
                    List<DriveStatInfo> drivesList = cachedDrivesList;

                    int volPct;
                    bool isMuted;
                    HardwareController.GetAudioStatus(out volPct, out isMuted);
                    loopCounter++;

                    double netDown, netUp;
                    NetworkSpeedMonitor.Sample(out netDown, out netUp);

                    int brightSnap = cachedBrightness;
                    bool wifiConnSnap = cachedWifiConn;
                    string wifiSsidSnap = cachedWifiSsid;
                    int wifiSigSnap = cachedWifiSig;

                    Dispatcher.Invoke(() =>
                    {
                        ApplySnapshotToUI(
                            snap,
                            batPct,
                            isCharging,
                            cpuPct,
                            ramPct,
                            usedRamGB,
                            totalRamGB,
                            drivesList,
                            volPct,
                            isMuted,
                            brightSnap,
                            wifiConnSnap,
                            wifiSsidSnap,
                            wifiSigSnap,
                            netDown,
                            netUp
                        );
                    });
                }
                catch { }

                Thread.Sleep((!isExpanded && !isMusicMode) ? 750 : 300);
            }
        }

        private int SampleCpuPercent()
        {
            long idle, kernel, user;
            if (!NativeMethods.GetSystemTimes(out idle, out kernel, out user)) return 10;
            long idleDiff = idle - prevIdleTime;
            long kernelDiff = kernel - prevKernelTime;
            long userDiff = user - prevUserTime;
            prevIdleTime = idle;
            prevKernelTime = kernel;
            prevUserTime = user;
            long total = kernelDiff + userDiff;
            if (total <= 0) return 5;
            int busy = (int)Math.Round(((total - idleDiff) * 100.0) / total);
            return Math.Max(1, Math.Min(100, busy));
        }

        private void ApplySnapshotToUI(
            MediaSnapshot snap,
            int batPct,
            bool isCharging,
            int cpuPct,
            int ramPct,
            double usedRamGB,
            double totalRamGB,
            List<DriveStatInfo> drives,
            int volPct,
            bool isMuted,
            int brightnessPct,
            bool wifiConnected,
            string wifiSsid,
            int wifiSignal,
            double netDownBps,
            double netUpBps)
        {
            string oldKey = currentTitle + "|" + currentArtist;
            string newKey = snap.Title + "|" + snap.Artist;
            bool wasPlaying = (currentStatus == "Playing");

            currentTitle = snap.Title;
            currentArtist = snap.Artist;
            currentStatus = snap.Status;
            currentDur = snap.Duration;

            if (newKey == oldKey && currentStatus == "Playing" && wasPlaying)
            {
                double localPredPos = currentPos + (DateTime.UtcNow - lastPosSync).TotalSeconds;
                bool rawPosChanged = Math.Abs(snap.RawPosition - lastRawSmtcPos) > 0.05;
                if (rawPosChanged || snap.Position > localPredPos + 0.15 || Math.Abs(snap.Position - localPredPos) > 3.0)
                {
                    currentPos = snap.Position;
                    lastPosSync = DateTime.UtcNow;
                }
            }
            else
            {
                currentPos = snap.Position;
                lastPosSync = DateTime.UtcNow;
            }
            lastRawSmtcPos = snap.RawPosition;

            if (!string.IsNullOrWhiteSpace(currentTitle))
            {
                if (newKey != oldKey)
                {
                    FetchLyricsIfNeeded();
                }
                else if (newKey == loadedLyricsKey && currentDur > 10.0 && Math.Abs(currentDur - loadedLyricsTrackDur) > 2.5)
                {
                    loadedLyricsKey = "";
                    FetchLyricsIfNeeded();
                }
            }

            if (isExpanded)
            {
                UpdateMediaTimelineUI();
            }

            if (snap.ThumbnailChanged)
            {
                if (snap.ThumbnailBytes != null && snap.ThumbnailBytes.Length > 0)
                {
                    try
                    {
                        BitmapImage bmp = new BitmapImage();
                        using (MemoryStream ms = new MemoryStream(snap.ThumbnailBytes))
                        {
                            bmp.BeginInit();
                            bmp.DecodePixelWidth = 128;
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = ms;
                            bmp.EndInit();
                            bmp.Freeze();
                        }
                        SetAlbumArtVisuals(bmp);
                    }
                    catch
                    {
                        ResetAlbumArtVisuals();
                    }
                }
                else
                {
                    ResetAlbumArtVisuals();
                }
            }

            UpdateCollapsedMusicMode();

            if (batPct != lastBatPct)
            {
                lastBatPct = batPct;
                collapsedBatteryArc.Data = BuildArcGeometry(
                    new Point(CollapsedBatterySize / 2.0, CollapsedBatterySize / 2.0),
                    (CollapsedBatterySize - CollapsedBatteryStroke) / 2.0,
                    batPct);
                expandedBatteryArc.Data = BuildArcGeometry(
                    new Point(ExpandedBatterySize / 2.0, ExpandedBatterySize / 2.0),
                    (ExpandedBatterySize - ExpandedBatteryStroke) / 2.0,
                    batPct);
            }

            string downStr = "↓ " + NetworkSpeedMonitor.Format(netDownBps);
            string upStr = "↑ " + NetworkSpeedMonitor.Format(netUpBps);
            if (collapsedNetDownText != null && collapsedNetDownText.Text != downStr) collapsedNetDownText.Text = downStr;
            if (collapsedNetUpText != null && collapsedNetUpText.Text != upStr) collapsedNetUpText.Text = upStr;

            if (isExpanded)
            {
                if (!string.IsNullOrWhiteSpace(currentTitle))
                {
                    if (mediaTitleText.Text != currentTitle) mediaTitleText.Text = currentTitle;
                    if (mediaArtistText.Text != currentArtist) mediaArtistText.Text = currentArtist;
                    mediaArtistText.Visibility = string.IsNullOrWhiteSpace(currentArtist) ? Visibility.Collapsed : Visibility.Visible;
                }
                else
                {
                    if (mediaTitleText.Text != "No media playing") mediaTitleText.Text = "No media playing";
                    if (mediaArtistText.Text != "") mediaArtistText.Text = "";
                    mediaArtistText.Visibility = Visibility.Collapsed;
                }

                playPauseIconPath.Data = Geometry.Parse(
                    currentStatus == "Playing"
                        ? "M 6,19 L 10,19 L 10,5 L 6,5 Z M 14,5 L 14,19 L 18,19 L 18,5 Z"
                        : "M 8,5 L 8,19 L 19,12 Z"
                );

                sysBatVal.Text = batPct + "%";
                sysBatSub.Text = isCharging ? "Charging ⚡" : "On Battery";
                sysBatBar.Width = Math.Max(4, 85.0 * batPct / 100.0);

                sysCpuVal.Text = cpuPct + "%";
                sysCpuSub.Text = "Active Load";
                sysCpuBar.Width = Math.Max(4, 85.0 * cpuPct / 100.0);

                sysRamVal.Text = ramPct + "%";
                sysRamSub.Text = string.Format(CultureInfo.InvariantCulture, "{0:F1} / {1:F1} GB", usedRamGB, totalRamGB);
                sysRamBar.Width = Math.Max(4, 85.0 * ramPct / 100.0);

                if (drives != null && drives.Count > 0)
                {
                    string sig = string.Join(",", drives.Select(d => d.Name));
                    if (sig != lastDrivesSignature || driveValTexts.Count == 0)
                    {
                        lastDrivesSignature = sig;
                        RebuildDrivesGrid(drives);
                    }

                    double maxDriveBarWidth = Math.Max(60.0, (290.0 / drives.Count) - 24.0);
                    for (int i = 0; i < drives.Count && i < driveValTexts.Count; i++)
                    {
                        var d = drives[i];
                        driveValTexts[i].Text = d.Percent + "%";
                        driveSubTexts[i].Text = string.Format(CultureInfo.InvariantCulture, "{0:F0} / {1:F0} GB", d.UsedGB, d.TotalGB);
                        driveFillBars[i].Width = Math.Max(4, maxDriveBarWidth * d.Percent / 100.0);
                    }
                }
                else if (driveValTexts.Count == 0)
                {
                    var fresh = GetFreshDrives();
                    if (fresh.Count > 0)
                    {
                        lastDrivesSignature = string.Join(",", fresh.Select(d => d.Name));
                        RebuildDrivesGrid(fresh);
                    }
                }

                if (wifiConnected)
                {
                    wifiSsidText.Text = wifiSsid;
                    wifiSubText.Text = wifiSignal + "%";
                    wifiSubText.Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85));
                    wifiSignalPill.Background = new SolidColorBrush(Color.FromArgb(45, 28, 231, 85));
                }
                else
                {
                    wifiSsidText.Text = "Not Connected";
                    wifiSubText.Text = "Offline";
                    wifiSubText.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                    wifiSignalPill.Background = new SolidColorBrush(Color.FromArgb(45, 248, 113, 113));
                }

                netDownText.Text = downStr;
                netUpText.Text = upStr;

                soundIconText.Text = (isMuted || volPct == 0) ? "🔇" : (volPct < 50 ? "🔉" : "🔊");
                if (!isDraggingSound)
                {
                    int displayVol = isMuted ? 0 : volPct;
                    soundValText.Text = isMuted ? "Mute" : (displayVol + "%");
                    double sw = soundTrackBorder.ActualWidth > 0 ? soundTrackBorder.ActualWidth : 155.0;
                    soundFillBorder.Width = sw * (displayVol / 100.0);
                }

                if (!isDraggingBright)
                {
                    brightValText.Text = brightnessPct + "%";
                    double bw = brightTrackBorder.ActualWidth > 0 ? brightTrackBorder.ActualWidth : 155.0;
                    brightFillBorder.Width = bw * (brightnessPct / 100.0);
                }
            }

            if (!IsCompanionWindow && CompanionInstance != null && CompanionInstance.IsVisible)
            {
                CompanionInstance.ApplySnapshotToUI(
                    snap,
                    batPct,
                    isCharging,
                    cpuPct,
                    ramPct,
                    usedRamGB,
                    totalRamGB,
                    drives,
                    volPct,
                    isMuted,
                    brightnessPct,
                    wifiConnected,
                    wifiSsid,
                    wifiSignal,
                    netDownBps,
                    netUpBps
                );
            }
        }
    }
}
