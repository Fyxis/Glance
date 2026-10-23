using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Windows.Foundation;
using Windows.Media.Control;
using Windows.Storage.Streams;
using Forms = System.Windows.Forms;
using Path = System.Windows.Shapes.Path;

namespace Glance
{
    public class NoteItem
    {
        public string id { get; set; }
        public string text { get; set; }
        public bool done { get; set; }
    }

    public class ClipItem
    {
        public string id { get; set; }
        public string text { get; set; }
        public string time { get; set; }
        public bool pinned { get; set; }
    }

    public class AppConfig
    {
        public int backgroundAlpha { get; set; }
        public bool isPinned { get; set; }
        public string displayMode { get; set; }
    }

    public class LyricLine
    {
        public double TimeSec { get; set; }
        public string Text { get; set; }
    }

    public class DriveStatInfo
    {
        public string Name = "";
        public int Percent = 0;
        public double UsedGB = 0;
        public double TotalGB = 0;
    }

    public class MediaSnapshot
    {
        public string Title = "";
        public string Artist = "";
        public string Status = "None";
        public double Position = 0;
        public double Duration = 0;
        public byte[] ThumbnailBytes = null;
        public bool ThumbnailChanged = false;
    }

    public class AppSearchEntry
    {
        public string Name;
        public string LowerName;
        public string[] Words;
        public string LaunchPath;
        public string Subtitle;
        public ImageSource IconSource;
    }

    // =======================================================
    // CORE AUDIO COM INTERFACES FOR NATIVE WINDOWS VOLUME
    // =======================================================
    [Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioEndpointVolume
    {
        int RegisterControlChangeNotify(IntPtr pNotify);
        int UnregisterControlChangeNotify(IntPtr pNotify);
        int GetChannelCount(out int pnChannelCount);
        int SetMasterVolumeLevel(float fLevelDB, ref Guid pguidEventContext);
        int SetMasterVolumeLevelScalar(float fLevel, ref Guid pguidEventContext);
        int GetMasterVolumeLevel(out float pfLevelDB);
        int GetMasterVolumeLevelScalar(out float pfLevel);
        int SetChannelVolumeLevel(uint nChannel, float fLevelDB, ref Guid pguidEventContext);
        int SetChannelVolumeLevelScalar(uint nChannel, float fLevel, ref Guid pguidEventContext);
        int GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        int GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, ref Guid pguidEventContext);
        int GetMute(out bool pbMute);
    }

    [Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDevice
    {
        int Activate(ref Guid id, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
    }

    // Real-time audio peak meter (used to drive the music waveform bars)
    [Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IAudioMeterInformation
    {
        int GetPeakValue(out float pfPeak);
        int GetMeteringChannelCount(out int pnChannelCount);
        int GetChannelsPeakValues(int u32ChannelCount, [Out, MarshalAs(UnmanagedType.LPArray)] float[] afPeakValues);
        int QueryHardwareSupport(out int pdwHardwareSupportMask);
    }

    // =======================================================
    // REAL-TIME INTERNET SPEED (DOWNLOAD / UPLOAD)
    // =======================================================
    public static class NetworkSpeedMonitor
    {
        private static long prevRx = -1;
        private static long prevTx = -1;
        private static int prevIfaceCount = -1;
        private static readonly Stopwatch sampleWatch = new Stopwatch();
        private static double smoothDown = 0;
        private static double smoothUp = 0;
        private static NetworkInterface[] cachedInterfaces = null;
        private static DateTime lastInterfaceRefresh = DateTime.MinValue;

        private static readonly string[] ignoredAdapterKeywords =
        {
            "virtual", "hyper-v", "vethernet", "vmware", "virtualbox", "pseudo", "loopback", "wan miniport", "filter", "npcap", "bluetooth"
        };

        private static void RefreshInterfacesIfNeeded()
        {
            if (cachedInterfaces != null && (DateTime.UtcNow - lastInterfaceRefresh).TotalSeconds < 10.0)
            {
                return;
            }

            try
            {
                var list = new List<NetworkInterface>();
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    string desc = ((ni.Description ?? "") + " " + (ni.Name ?? "")).ToLowerInvariant();
                    if (ignoredAdapterKeywords.Any(k => desc.Contains(k))) continue;

                    list.Add(ni);
                }
                cachedInterfaces = list.ToArray();
                lastInterfaceRefresh = DateTime.UtcNow;
            }
            catch
            {
                cachedInterfaces = new NetworkInterface[0];
            }
        }

        public static void Sample(out double downBytesPerSec, out double upBytesPerSec)
        {
            RefreshInterfacesIfNeeded();

            long rx = 0, tx = 0;
            int ifaceCount = 0;
            try
            {
                var ifaces = cachedInterfaces;
                if (ifaces != null)
                {
                    for (int i = 0; i < ifaces.Length; i++)
                    {
                        var ni = ifaces[i];
                        if (ni.OperationalStatus != OperationalStatus.Up) continue;
                        IPv4InterfaceStatistics st = ni.GetIPv4Statistics();
                        rx += st.BytesReceived;
                        tx += st.BytesSent;
                        ifaceCount++;
                    }
                }
            }
            catch
            {
                cachedInterfaces = null; // force refresh on next sample if error
            }

            double elapsed = sampleWatch.IsRunning ? sampleWatch.Elapsed.TotalSeconds : 0;
            sampleWatch.Restart();

            // Skip a sample when adapters appear/disappear to avoid huge fake spikes
            if (prevRx >= 0 && elapsed > 0.05 && ifaceCount == prevIfaceCount)
            {
                double d = Math.Max(0, (rx - prevRx) / elapsed);
                double u = Math.Max(0, (tx - prevTx) / elapsed);
                smoothDown = smoothDown * 0.35 + d * 0.65;
                smoothUp = smoothUp * 0.35 + u * 0.65;
            }

            prevRx = rx;
            prevTx = tx;
            prevIfaceCount = ifaceCount;
            downBytesPerSec = smoothDown;
            upBytesPerSec = smoothUp;
        }

        public static string Format(double bytesPerSec)
        {
            double kb = bytesPerSec / 1024.0;
            if (kb < 1) return "0 KB/s";
            if (kb < 10) return kb.ToString("F1", CultureInfo.InvariantCulture) + " KB/s";
            if (kb < 1024) return kb.ToString("F0", CultureInfo.InvariantCulture) + " KB/s";
            double mb = kb / 1024.0;
            return mb.ToString(mb < 100 ? "F1" : "F0", CultureInfo.InvariantCulture) + " MB/s";
        }
    }

    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IMMDeviceEnumerator
    {
        int EnumAudioEndpoints(int dataFlow, int dwStateMask, out IntPtr ppDevices);
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice ppEndpoint);
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    internal class MMDeviceEnumeratorComObject { }

    public static class HardwareController
    {
        [DllImport("wlanapi.dll")]
        private static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);
        [DllImport("wlanapi.dll")]
        private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);
        [DllImport("wlanapi.dll")]
        private static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);
        [DllImport("wlanapi.dll")]
        private static extern uint WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid, int OpCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, IntPtr pWlanOpcodeValueType);
        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr pMemory);

        private static readonly object audioLock = new object();
        private static IAudioEndpointVolume cachedVolume = null;
        private static IAudioMeterInformation cachedMeter = null;
        private static DateTime lastAudioInit = DateTime.MinValue;

        private static void SafeReleaseCom(ref object comObj)
        {
            if (comObj != null)
            {
                try
                {
                    if (Marshal.IsComObject(comObj))
                    {
                        Marshal.ReleaseComObject(comObj);
                    }
                }
                catch { }
                comObj = null;
            }
        }

        private static void EnsureAudioEndpoints(bool forceRefresh = false)
        {
            lock (audioLock)
            {
                if (!forceRefresh && cachedVolume != null && cachedMeter != null && (DateTime.UtcNow - lastAudioInit).TotalSeconds < 30.0)
                {
                    return;
                }

                object enumObj = null;
                IMMDeviceEnumerator enumerator = null;
                IMMDevice dev = null;
                try
                {
                    enumObj = new MMDeviceEnumeratorComObject();
                    enumerator = enumObj as IMMDeviceEnumerator;
                    if (enumerator != null)
                    {
                        enumerator.GetDefaultAudioEndpoint(0, 1, out dev);
                        if (dev != null)
                        {
                            object oldVol = cachedVolume;
                            cachedVolume = null;
                            SafeReleaseCom(ref oldVol);

                            object oldMeter = cachedMeter;
                            cachedMeter = null;
                            SafeReleaseCom(ref oldMeter);

                            Guid iidVol = typeof(IAudioEndpointVolume).GUID;
                            object oVol;
                            dev.Activate(ref iidVol, 23, IntPtr.Zero, out oVol);
                            cachedVolume = oVol as IAudioEndpointVolume;

                            Guid iidMeter = typeof(IAudioMeterInformation).GUID;
                            object oMeter;
                            dev.Activate(ref iidMeter, 23, IntPtr.Zero, out oMeter);
                            cachedMeter = oMeter as IAudioMeterInformation;

                            lastAudioInit = DateTime.UtcNow;
                        }
                    }
                }
                catch { }
                finally
                {
                    object d = dev;
                    SafeReleaseCom(ref d);
                    object e = enumerator;
                    SafeReleaseCom(ref e);
                    SafeReleaseCom(ref enumObj);
                }
            }
        }

        /// <summary>
        /// Returns the current output peak level (0..1) of the default speaker, or -1 if unavailable.
        /// </summary>
        public static float GetPeakLevel()
        {
            try
            {
                EnsureAudioEndpoints();
                IAudioMeterInformation meter = cachedMeter;
                if (meter == null) return -1f;

                float peak;
                if (meter.GetPeakValue(out peak) != 0)
                {
                    EnsureAudioEndpoints(forceRefresh: true);
                    return -1f;
                }
                return peak;
            }
            catch
            {
                return -1f;
            }
        }

        public static void GetAudioStatus(out int volumePercent, out bool isMuted)
        {
            volumePercent = 50;
            isMuted = false;
            try
            {
                EnsureAudioEndpoints();
                IAudioEndpointVolume vol = cachedVolume;
                if (vol != null)
                {
                    float level;
                    bool muted;
                    vol.GetMasterVolumeLevelScalar(out level);
                    vol.GetMute(out muted);
                    volumePercent = (int)Math.Round(level * 100.0f);
                    isMuted = muted;
                }
            }
            catch
            {
                EnsureAudioEndpoints(forceRefresh: true);
            }
        }

        public static void SetAudioVolume(int volumePercent)
        {
            try
            {
                EnsureAudioEndpoints();
                IAudioEndpointVolume vol = cachedVolume;
                if (vol != null)
                {
                    float scalar = Math.Max(0f, Math.Min(1f, volumePercent / 100.0f));
                    Guid ctx = Guid.Empty;
                    vol.SetMasterVolumeLevelScalar(scalar, ref ctx);
                    if (volumePercent > 0)
                    {
                        vol.SetMute(false, ref ctx);
                    }
                }
            }
            catch { }
        }

        public static void ToggleAudioMute()
        {
            try
            {
                EnsureAudioEndpoints();
                IAudioEndpointVolume vol = cachedVolume;
                if (vol != null)
                {
                    bool muted;
                    vol.GetMute(out muted);
                    Guid ctx = Guid.Empty;
                    vol.SetMute(!muted, ref ctx);
                }
            }
            catch { }
        }

        public static int GetScreenBrightness()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        using (obj)
                        {
                            return Convert.ToInt32(obj["CurrentBrightness"]);
                        }
                    }
                }
            }
            catch { }
            return 100;
        }

        public static void SetScreenBrightness(int percent)
        {
            try
            {
                int val = Math.Max(5, Math.Min(100, percent));
                using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        using (obj)
                        {
                            obj.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, (byte)val });
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        public static void GetWifiStatus(out bool connected, out string ssid, out int signalPercent)
        {
            connected = false;
            ssid = "Tidak Terhubung";
            signalPercent = 0;

            try
            {
                uint ver;
                IntPtr client;
                if (WlanOpenHandle(2, IntPtr.Zero, out ver, out client) == 0)
                {
                    IntPtr listPtr;
                    if (WlanEnumInterfaces(client, IntPtr.Zero, out listPtr) == 0)
                    {
                        uint count = (uint)Marshal.ReadInt32(listPtr, 0);
                        for (int i = 0; i < count; i++)
                        {
                            IntPtr itemPtr = new IntPtr(listPtr.ToInt64() + 8 + (i * 532));
                            byte[] guidBytes = new byte[16];
                            Marshal.Copy(itemPtr, guidBytes, 0, 16);
                            Guid ifGuid = new Guid(guidBytes);
                            int state = Marshal.ReadInt32(itemPtr, 528);
                            if (state == 1)
                            {
                                uint dataSize;
                                IntPtr dataPtr;
                                if (WlanQueryInterface(client, ref ifGuid, 7, IntPtr.Zero, out dataSize, out dataPtr, IntPtr.Zero) == 0)
                                {
                                    int ssidLen = Marshal.ReadInt32(dataPtr, 520);
                                    if (ssidLen > 0 && ssidLen <= 32)
                                    {
                                        byte[] ssidBytes = new byte[ssidLen];
                                        Marshal.Copy(new IntPtr(dataPtr.ToInt64() + 524), ssidBytes, 0, ssidLen);
                                        ssid = Encoding.UTF8.GetString(ssidBytes);
                                    }
                                    signalPercent = Math.Max(0, Math.Min(100, Marshal.ReadInt32(dataPtr, 576)));
                                    connected = true;
                                    WlanFreeMemory(dataPtr);
                                    break;
                                }
                            }
                        }
                        WlanFreeMemory(listPtr);
                    }
                    WlanCloseHandle(client, IntPtr.Zero);
                }
            }
            catch { }
        }
    }

    public static class NativeMethods
    {
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_NOREPEAT = 0x4000;
        public const uint VK_P = 0x50;

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("psapi.dll")]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("kernel32.dll")]
        public static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);
    }

    public static class MemoryOptimizer
    {
        private static DateTime lastTrim = DateTime.MinValue;
        private static readonly object trimLock = new object();

        public static void TrimMemory(bool force = false)
        {
            try
            {
                lock (trimLock)
                {
                    if (!force && (DateTime.UtcNow - lastTrim).TotalSeconds < 2.0)
                    {
                        return;
                    }
                    lastTrim = DateTime.UtcNow;
                }

                GC.Collect(2, GCCollectionMode.Forced, false);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, false);

                IntPtr handle = Process.GetCurrentProcess().Handle;
                NativeMethods.SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));
                NativeMethods.EmptyWorkingSet(handle);
            }
            catch { }
        }
    }

    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppValueName = "Glance";
        private const string LegacyAppValueName = "WindowsDynamicIsland";

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(AppValueName) ?? key.GetValue(LegacyAppValueName);
                    return val != null && !string.IsNullOrWhiteSpace(val.ToString());
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetStartup(bool enable, string exePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Process.GetCurrentProcess().MainModule.FileName;
                }
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return;
                    try { key.DeleteValue(LegacyAppValueName, false); } catch { }
                    if (enable)
                    {
                        key.SetValue(AppValueName, "\"" + exePath + "\"");
                    }
                    else
                    {
                        key.DeleteValue(AppValueName, false);
                    }
                }
            }
            catch { }
        }
    }

    // =======================================================
    // MACBOOK SPOTLIGHT SEARCH OVERLAY (CTRL + SHIFT + P)
    // =======================================================
    public class SpotlightWindow : Window
    {
        private static List<AppSearchEntry> cachedApps = null;
        private static readonly object indexLock = new object();

        private Image blurredScreenImage;
        private Border searchCardBorder;
        private TextBox searchInput;
        private TextBlock placeholderText;
        private Border resultsDivider;
        private StackPanel resultsStack;
        private List<AppSearchEntry> currentMatches = new List<AppSearchEntry>();
        private List<Border> resultRowBorders = new List<Border>();
        private int selectedIndex = 0;

        public static void PrewarmIndexAsync()
        {
            Task.Run(() =>
            {
                EnsureAppsIndexed();
                MemoryOptimizer.TrimMemory();
            });
        }

        private static void EnsureAppsIndexed()
        {
            lock (indexLock)
            {
                if (cachedApps != null && cachedApps.Count > 0) return;

                Dictionary<string, AppSearchEntry> map = new Dictionary<string, AppSearchEntry>(StringComparer.OrdinalIgnoreCase);

                string[] scanDirs = new string[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu),
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)
                };

                string[] ignoreKeywords = new string[]
                {
                    "uninstall", "hapus", "remove", "readme", "help", "documentation", "manual", "website", "license"
                };

                foreach (string dir in scanDirs)
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;
                    try
                    {
                        foreach (string file in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                        {
                            string ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                            if (ext != ".lnk" && ext != ".exe" && ext != ".appref-ms") continue;

                            string name = System.IO.Path.GetFileNameWithoutExtension(file).Trim();
                            if (string.IsNullOrEmpty(name) || name.Length < 2) continue;

                            string lower = name.ToLowerInvariant();
                            if (ignoreKeywords.Any(k => lower.Contains(k))) continue;

                            if (!map.ContainsKey(name))
                            {
                                string folderName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(file));
                                string subtitle = (folderName.Equals("Programs", StringComparison.OrdinalIgnoreCase) || folderName.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
                                    ? "Aplikasi Terpasang"
                                    : ("Aplikasi • " + folderName);

                                map[name] = new AppSearchEntry
                                {
                                    Name = name,
                                    LowerName = lower,
                                    Words = Regex.Split(lower, @"[\s\-_\.]+").Where(w => w.Length > 0).ToArray(),
                                    LaunchPath = file,
                                    Subtitle = subtitle,
                                    IconSource = null
                                };
                            }
                        }
                    }
                    catch { }
                }

                // Also scan Registry App Paths for apps not in Start Menu
                RegistryKey[] roots = new RegistryKey[] { Registry.LocalMachine, Registry.CurrentUser };
                foreach (var root in roots)
                {
                    try
                    {
                        using (var appPaths = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths", false))
                        {
                            if (appPaths == null) continue;
                            foreach (string subName in appPaths.GetSubKeyNames())
                            {
                                using (var sub = appPaths.OpenSubKey(subName, false))
                                {
                                    if (sub == null) continue;
                                    string exePath = (sub.GetValue("") as string ?? "").Trim('\"', ' ');
                                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                                    {
                                        string friendly = System.IO.Path.GetFileNameWithoutExtension(subName);
                                        try
                                        {
                                            var vi = FileVersionInfo.GetVersionInfo(exePath);
                                            if (!string.IsNullOrWhiteSpace(vi.FileDescription))
                                            {
                                                friendly = vi.FileDescription.Trim();
                                            }
                                        }
                                        catch { }

                                        if (!map.ContainsKey(friendly))
                                        {
                                            string lower = friendly.ToLowerInvariant();
                                            map[friendly] = new AppSearchEntry
                                            {
                                                Name = friendly,
                                                LowerName = lower,
                                                Words = Regex.Split(lower, @"[\s\-_\.]+").Where(w => w.Length > 0).ToArray(),
                                                LaunchPath = exePath,
                                                Subtitle = "Aplikasi Sistem",
                                                IconSource = null
                                            };
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                cachedApps = map.Values.OrderBy(a => a.Name).ToList();
            }
        }

        private static ImageSource GetOrLoadIcon(AppSearchEntry entry)
        {
            if (entry.IconSource != null) return entry.IconSource;
            try
            {
                using (System.Drawing.Icon ico = System.Drawing.Icon.ExtractAssociatedIcon(entry.LaunchPath))
                {
                    if (ico != null)
                    {
                        BitmapSource bmp = Imaging.CreateBitmapSourceFromHIcon(
                            ico.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(32, 32)
                        );
                        bmp.Freeze();
                        entry.IconSource = bmp;
                    }
                }
            }
            catch { }
            return entry.IconSource;
        }

        public SpotlightWindow()
        {
            Title = "Glance Search";
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            Closed += (s, e) => MemoryOptimizer.TrimMemory();
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            BuildSpotlightUI();

            Loaded += (s, e) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOOLWINDOW);
            };

            Deactivated += (s, e) =>
            {
                if (IsVisible) HideSpotlight();
            };

            PreviewKeyDown += OnSpotlightPreviewKeyDown;
        }

        private void BuildSpotlightUI()
        {
            Grid root = new Grid();

            // 1. Fullscreen Blurred Desktop Background Image
            blurredScreenImage = new Image
            {
                Stretch = Stretch.Fill,
                Effect = new BlurEffect
                {
                    Radius = 14,
                    KernelType = KernelType.Gaussian,
                    RenderingBias = RenderingBias.Performance
                }
            };
            root.Children.Add(blurredScreenImage);

            // 2. Dark Translucent Glass Backdrop (Click outside to close)
            Border darkBackdrop = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(135, 6, 6, 12))
            };
            darkBackdrop.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                HideSpotlight();
            };
            root.Children.Add(darkBackdrop);

            // 3. Spotlight Search Box positioned in Upper-Middle of Screen (like MacBook Spotlight)
            double topOffset = Math.Round(SystemParameters.PrimaryScreenHeight * 0.20);
            searchCardBorder = new Border
            {
                Width = 600,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, topOffset, 0, 0),
                Background = new SolidColorBrush(Color.FromArgb(220, 20, 20, 26)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(75, 255, 255, 255)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(16),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 38,
                    ShadowDepth = 12,
                    Opacity = 0.72,
                    Color = Colors.Black
                }
            };
            searchCardBorder.MouseLeftButtonDown += (s, e) => e.Handled = true;

            StackPanel cardStack = new StackPanel();

            // Search Input Bar Row
            Grid inputRow = new Grid
            {
                Height = 56,
                Margin = new Thickness(18, 0, 16, 0)
            };
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });

            // Magnifying Glass Icon
            Path searchIcon = new Path
            {
                Data = Geometry.Parse("M 15.5,14 L 14.71,14 L 14.43,13.73 C 15.41,12.59 16,11.11 16,9.5 C 16,5.91 13.09,3 9.5,3 C 5.91,3 3,5.91 3,9.5 C 3,13.09 5.91,16 9.5,16 C 11.11,16 12.59,15.41 13.73,14.43 L 14,14.71 L 14,15.5 L 19,20.49 L 20.49,19 L 15.5,14 Z M 9.5,14 C 7.01,14 5,11.99 5,9.5 C 5,7.01 7.01,5 9.5,5 C 11.99,5 14,7.01 14,9.5 C 14,11.99 11.99,14 9.5,14 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(180, 180, 195)),
                Width = 20,
                Height = 20,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(searchIcon, 0);

            Grid textHost = new Grid { VerticalAlignment = VerticalAlignment.Center };
            placeholderText = new TextBlock
            {
                Text = "Glance Search — Ketik nama aplikasi (misal: ch, chrome)...",
                Foreground = new SolidColorBrush(Color.FromRgb(125, 125, 138)),
                FontSize = 17,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };

            searchInput = new TextBox
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                CaretBrush = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                BorderThickness = new Thickness(0),
                FontSize = 18,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            searchInput.TextChanged += OnSearchInputChanged;

            textHost.Children.Add(placeholderText);
            textHost.Children.Add(searchInput);
            Grid.SetColumn(textHost, 1);

            Border escBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(7, 3, 7, 3),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand
            };
            escBadge.Child = new TextBlock
            {
                Text = "ESC",
                Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 182)),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold
            };
            escBadge.MouseLeftButtonUp += (s, e) => { e.Handled = true; HideSpotlight(); };
            Grid.SetColumn(escBadge, 2);

            inputRow.Children.Add(searchIcon);
            inputRow.Children.Add(textHost);
            inputRow.Children.Add(escBadge);
            cardStack.Children.Add(inputRow);

            // Divider line between search input & dropdown results
            resultsDivider = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                Visibility = Visibility.Collapsed
            };
            cardStack.Children.Add(resultsDivider);

            // Results Stack
            resultsStack = new StackPanel
            {
                Margin = new Thickness(8, 6, 8, 8),
                Visibility = Visibility.Collapsed
            };
            cardStack.Children.Add(resultsStack);

            searchCardBorder.Child = cardStack;
            root.Children.Add(searchCardBorder);

            Content = root;
        }

        public void ShowSpotlight()
        {
            EnsureAppsIndexed();

            Forms.Screen targetScr = Forms.Screen.FromPoint(Forms.Cursor.Position);
            if (targetScr == null) targetScr = Forms.Screen.PrimaryScreen;

            int screenX = targetScr.Bounds.X;
            int screenY = targetScr.Bounds.Y;
            int sw = targetScr.Bounds.Width;
            int sh = targetScr.Bounds.Height;
            int dw = Math.Max(320, sw / 4);
            int dh = Math.Max(180, sh / 4);

            // Capture current desktop screen downscaled so the background has a true Gaussian blur with minimal memory footprint
            try
            {
                using (var fullBmp = new System.Drawing.Bitmap(sw, sh, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                {
                    using (var g = System.Drawing.Graphics.FromImage(fullBmp))
                    {
                        g.CopyFromScreen(screenX, screenY, 0, 0, new System.Drawing.Size(sw, sh), System.Drawing.CopyPixelOperation.SourceCopy);
                    }

                    using (var smallBmp = new System.Drawing.Bitmap(fullBmp, new System.Drawing.Size(dw, dh)))
                    {
                        IntPtr hBitmap = smallBmp.GetHbitmap();
                        try
                        {
                            BitmapSource src = Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions()
                            );
                            src.Freeze();
                            blurredScreenImage.Source = src;
                        }
                        finally
                        {
                            NativeMethods.DeleteObject(hBitmap);
                        }
                    }
                }
            }
            catch { }

            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            try
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source != null && source.CompositionTarget != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }
            }
            catch { }

            Left = screenX / dpiScaleX;
            Top = screenY / dpiScaleY;
            Width = sw / dpiScaleX;
            Height = sh / dpiScaleY;

            searchInput.Text = "";
            placeholderText.Visibility = Visibility.Visible;
            resultsDivider.Visibility = Visibility.Collapsed;
            resultsStack.Visibility = Visibility.Collapsed;
            resultsStack.Children.Clear();
            currentMatches.Clear();

            Opacity = 0;
            Show();
            Activate();
            searchInput.Focus();

            BeginAnimation(OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(140)));
        }

        public void HideSpotlight()
        {
            Hide();
            blurredScreenImage.Source = null;
            resultsStack.Children.Clear();
            currentMatches.Clear();
            MemoryOptimizer.TrimMemory();
        }

        private void OnSearchInputChanged(object sender, TextChangedEventArgs e)
        {
            string query = (searchInput.Text ?? "").Trim().ToLowerInvariant();
            placeholderText.Visibility = string.IsNullOrEmpty(searchInput.Text) ? Visibility.Visible : Visibility.Collapsed;

            if (string.IsNullOrEmpty(query))
            {
                resultsDivider.Visibility = Visibility.Collapsed;
                resultsStack.Visibility = Visibility.Collapsed;
                resultsStack.Children.Clear();
                currentMatches.Clear();
                return;
            }

            EnsureAppsIndexed();

            // Rank applications:
            // 1. App name starts with query
            // 2. Any word in app name starts with query (e.g. "ch" matches "Google Chrome"!)
            // 3. App name contains query anywhere
            var scored = new List<Tuple<int, AppSearchEntry>>();
            if (cachedApps != null)
            {
                foreach (var app in cachedApps)
                {
                    if (app.LowerName.StartsWith(query))
                    {
                        scored.Add(Tuple.Create(1, app));
                    }
                    else if (app.Words != null && app.Words.Any(w => w.StartsWith(query)))
                    {
                        scored.Add(Tuple.Create(2, app));
                    }
                    else if (app.LowerName.Contains(query))
                    {
                        scored.Add(Tuple.Create(3, app));
                    }
                }
            }

            currentMatches = scored
                .OrderBy(t => t.Item1)
                .ThenBy(t => t.Item2.Name.Length)
                .ThenBy(t => t.Item2.Name)
                .Select(t => t.Item2)
                .Take(7)
                .ToList();

            RenderSearchResults();
        }

        private void RenderSearchResults()
        {
            resultsStack.Children.Clear();
            resultRowBorders.Clear();
            selectedIndex = 0;

            resultsDivider.Visibility = Visibility.Visible;
            resultsStack.Visibility = Visibility.Visible;

            if (currentMatches.Count == 0)
            {
                resultsStack.Children.Add(new TextBlock
                {
                    Text = "Tidak ditemukan aplikasi dengan nama tersebut.",
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 152)),
                    FontSize = 13,
                    Margin = new Thickness(12, 10, 12, 10)
                });
                return;
            }

            for (int i = 0; i < currentMatches.Count; i++)
            {
                int idx = i;
                var app = currentMatches[i];

                Border row = new Border
                {
                    Height = 46,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(0, 1, 0, 1),
                    Cursor = Cursors.Hand,
                    Background = Brushes.Transparent
                };

                Grid g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(75) });

                ImageSource iconSrc = GetOrLoadIcon(app);
                if (iconSrc != null)
                {
                    Image img = new Image
                    {
                        Source = iconSrc,
                        Width = 26,
                        Height = 26,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
                    Grid.SetColumn(img, 0);
                    g.Children.Add(img);
                }
                else
                {
                    Border fallbackIcon = new Border
                    {
                        Width = 26,
                        Height = 26,
                        CornerRadius = new CornerRadius(6),
                        Background = new SolidColorBrush(Color.FromArgb(60, 28, 231, 85)),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    fallbackIcon.Child = new TextBlock
                    {
                        Text = app.Name.Substring(0, 1).ToUpperInvariant(),
                        Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                        FontWeight = FontWeights.Bold,
                        FontSize = 12,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(fallbackIcon, 0);
                    g.Children.Add(fallbackIcon);
                }

                StackPanel textCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                textCol.Children.Add(new TextBlock
                {
                    Text = app.Name,
                    Foreground = Brushes.White,
                    FontSize = 13.5,
                    FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                textCol.Children.Add(new TextBlock
                {
                    Text = app.Subtitle,
                    Foreground = new SolidColorBrush(Color.FromRgb(145, 145, 158)),
                    FontSize = 10.5,
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                Grid.SetColumn(textCol, 1);
                g.Children.Add(textCol);

                TextBlock enterHint = new TextBlock
                {
                    Text = "Buka ↵",
                    Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                    FontSize = 11.5,
                    FontWeight = FontWeights.Bold,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(enterHint, 2);
                g.Children.Add(enterHint);

                row.Child = g;

                row.MouseEnter += (s, e) =>
                {
                    selectedIndex = idx;
                    UpdateResultSelectionVisuals();
                };

                row.MouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    LaunchApp(app);
                };

                resultRowBorders.Add(row);
                resultsStack.Children.Add(row);
            }

            UpdateResultSelectionVisuals();
        }

        private void UpdateResultSelectionVisuals()
        {
            for (int i = 0; i < resultRowBorders.Count; i++)
            {
                bool active = (i == selectedIndex);
                Border row = resultRowBorders[i];
                row.Background = active
                    ? new SolidColorBrush(Color.FromArgb(55, 28, 231, 85))
                    : Brushes.Transparent;
                row.BorderBrush = active
                    ? new SolidColorBrush(Color.FromArgb(95, 28, 231, 85))
                    : Brushes.Transparent;
                row.BorderThickness = new Thickness(1);

                Grid g = row.Child as Grid;
                if (g != null && g.Children.Count >= 3)
                {
                    TextBlock hint = g.Children[g.Children.Count - 1] as TextBlock;
                    if (hint != null)
                    {
                        hint.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }

        private void OnSpotlightPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                HideSpotlight();
            }
            else if (e.Key == Key.Down)
            {
                if (currentMatches.Count > 0)
                {
                    e.Handled = true;
                    selectedIndex = (selectedIndex + 1) % currentMatches.Count;
                    UpdateResultSelectionVisuals();
                }
            }
            else if (e.Key == Key.Up)
            {
                if (currentMatches.Count > 0)
                {
                    e.Handled = true;
                    selectedIndex = (selectedIndex - 1 + currentMatches.Count) % currentMatches.Count;
                    UpdateResultSelectionVisuals();
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (currentMatches.Count > 0 && selectedIndex >= 0 && selectedIndex < currentMatches.Count)
                {
                    e.Handled = true;
                    LaunchApp(currentMatches[selectedIndex]);
                }
            }
        }

        private void LaunchApp(AppSearchEntry app)
        {
            HideSpotlight();
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = app.LaunchPath,
                    UseShellExecute = true
                });
            }
            catch { }
        }
    }

    public class SettingsWindow : Window
    {
        private CheckBox chkStartup;
        private CheckBox chkPin;
        private Slider sliderAlpha;
        private TextBlock lblAlphaVal;
        private RadioButton rbScreen1;
        private RadioButton rbScreen2;
        private RadioButton rbScreenBoth;
        private MainWindow parentIsland;

        public SettingsWindow(MainWindow owner)
        {
            parentIsland = owner;
            Title = "Glance — Pengaturan";
            Width = 430;
            Height = 525;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            Closed += (s, e) => MemoryOptimizer.TrimMemory();

            Border outer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(230, 16, 16, 20)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 56)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(22),
                Effect = new DropShadowEffect { BlurRadius = 24, ShadowDepth = 6, Opacity = 0.6, Color = Colors.Black }
            };

            outer.MouseLeftButtonDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };

            StackPanel root = new StackPanel();

            // Header
            DockPanel header = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
            TextBlock title = new TextBlock
            {
                Text = "Pengaturan Glance",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Button closeBtn = new Button
            {
                Content = "✕",
                Width = 26,
                Height = 26,
                Background = new SolidColorBrush(Color.FromRgb(36, 36, 42)),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(closeBtn, Dock.Right);
            closeBtn.Click += (s, e) => Close();
            header.Children.Add(closeBtn);
            header.Children.Add(title);
            root.Children.Add(header);

            // Option 1: Run at Startup Windows
            Border cardStartup = CreateOptionCard();
            chkStartup = new CheckBox
            {
                Content = "Run at Startup Windows (Otomatis saat menyala)",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                IsChecked = StartupManager.IsStartupEnabled(),
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            cardStartup.Child = chkStartup;
            root.Children.Add(cardStartup);

            // Option 2: Pin Expanded
            Border cardPin = CreateOptionCard();
            chkPin = new CheckBox
            {
                Content = "Kunci Posisi Glance Tetap Terbuka (Pin)",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                IsChecked = parentIsland != null && parentIsland.IsPinned,
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            cardPin.Child = chkPin;
            root.Children.Add(cardPin);

            // Option 3: Multi-Monitor Display Option
            Border cardScreen = CreateOptionCard();
            StackPanel spScreen = new StackPanel();

            TextBlock lblScreenTitle = new TextBlock
            {
                Text = "Tampilkan Glance di Layar / Monitor:",
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            spScreen.Children.Add(lblScreenTitle);

            int screenCount = Forms.Screen.AllScreens.Length;
            string curMode = MainWindow.DisplayMode ?? "screen1";

            rbScreen1 = new RadioButton
            {
                Content = "Layar 1 Saja (Monitor Utama)",
                GroupName = "DisplayScreenGroup",
                Foreground = Brushes.White,
                FontSize = 12,
                IsChecked = (curMode == "screen1"),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand
            };
            rbScreen2 = new RadioButton
            {
                Content = screenCount > 1 ? "Layar 2 Saja (Monitor Sekunder)" : "Layar 2 Saja (Tidak terdeteksi)",
                GroupName = "DisplayScreenGroup",
                Foreground = screenCount > 1 ? Brushes.White : new SolidColorBrush(Color.FromRgb(130, 130, 140)),
                FontSize = 12,
                IsEnabled = screenCount > 1,
                IsChecked = (curMode == "screen2" && screenCount > 1),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = screenCount > 1 ? Cursors.Hand : Cursors.No
            };
            rbScreenBoth = new RadioButton
            {
                Content = screenCount > 1 ? "Kedua Layar (Layar 1 & Layar 2)" : "Kedua Layar (Perlu minimal 2 monitor)",
                GroupName = "DisplayScreenGroup",
                Foreground = screenCount > 1 ? Brushes.White : new SolidColorBrush(Color.FromRgb(130, 130, 140)),
                FontSize = 12,
                IsEnabled = screenCount > 1,
                IsChecked = (curMode == "both" && screenCount > 1),
                Cursor = screenCount > 1 ? Cursors.Hand : Cursors.No
            };

            rbScreen1.Checked += (s, e) => MainWindow.ApplyDisplayMode("screen1");
            rbScreen2.Checked += (s, e) => { if (screenCount > 1) MainWindow.ApplyDisplayMode("screen2"); };
            rbScreenBoth.Checked += (s, e) => { if (screenCount > 1) MainWindow.ApplyDisplayMode("both"); };

            spScreen.Children.Add(rbScreen1);
            spScreen.Children.Add(rbScreen2);
            spScreen.Children.Add(rbScreenBoth);
            cardScreen.Child = spScreen;
            root.Children.Add(cardScreen);

            // Option 4: Background Transparency Slider
            Border cardAlpha = CreateOptionCard();
            StackPanel spAlpha = new StackPanel();
            DockPanel dpAlpha = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            int curAlpha = parentIsland != null ? parentIsland.BackgroundAlpha : 175;
            int curPct = (int)Math.Round(curAlpha * 100.0 / 255.0);
            lblAlphaVal = new TextBlock
            {
                Text = curPct + "%",
                Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(lblAlphaVal, Dock.Right);
            dpAlpha.Children.Add(lblAlphaVal);
            dpAlpha.Children.Add(new TextBlock
            {
                Text = "Tingkat Kepekatan / Transparansi Background",
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold
            });

            sliderAlpha = new Slider
            {
                Minimum = 70,
                Maximum = 255,
                Value = curAlpha,
                Cursor = Cursors.Hand
            };
            sliderAlpha.ValueChanged += (s, e) =>
            {
                int a = (int)Math.Round(sliderAlpha.Value);
                lblAlphaVal.Text = (int)Math.Round(a * 100.0 / 255.0) + "%";
                if (parentIsland != null)
                {
                    parentIsland.SetBackgroundAlpha((byte)a);
                }
            };
            spAlpha.Children.Add(dpAlpha);
            spAlpha.Children.Add(sliderAlpha);
            cardAlpha.Child = spAlpha;
            root.Children.Add(cardAlpha);

            TextBlock shortcutHint = new TextBlock
            {
                Text = "💡 Tekan Ctrl + Shift + P kapan saja untuk membuka Glance Search (Pencarian Aplikasi).",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 178)),
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 2, 2, 10)
            };
            root.Children.Add(shortcutHint);

            // Save Button
            Button saveBtn = new Button
            {
                Content = "Simpan Pengaturan",
                Height = 36,
                Background = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            saveBtn.Click += (s, e) =>
            {
                StartupManager.SetStartup(chkStartup.IsChecked == true);
                string selectedMode = "screen1";
                if (rbScreen2.IsChecked == true) selectedMode = "screen2";
                else if (rbScreenBoth.IsChecked == true) selectedMode = "both";
                MainWindow.ApplyDisplayMode(selectedMode);

                if (parentIsland != null)
                {
                    parentIsland.SetPinned(chkPin.IsChecked == true);
                    parentIsland.SaveAppConfig();
                }
                Close();
            };
            root.Children.Add(saveBtn);

            outer.Child = root;
            Content = outer;
        }

        private Border CreateOptionCard()
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 28, 28, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 56)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 10, 12, 10),
                Margin = new Thickness(0, 0, 0, 10)
            };
        }
    }

    public class MainWindow : Window
    {
        private const int HOTKEY_ID_SPOTLIGHT = 9001;
        private const double CollapsedWidth = 150;
        private const double CollapsedHeight = 38;
        private const double ExpandedWidth = 416;
        private const double ExpandedHeight = 212;
        private const int SlideCount = 5;

        public static MainWindow PrimaryInstance { get; private set; }
        public static MainWindow CompanionInstance { get; private set; }
        private static string _displayMode = "screen1";
        public static string DisplayMode
        {
            get { return _displayMode; }
            set { _displayMode = value; }
        }
        private static string pendingCommand = null;
        private static double? pendingSeekSec = null;

        public bool IsCompanionWindow { get; private set; }
        public Forms.Screen TargetScreen { get; private set; }

        private Forms.ToolStripMenuItem trayItemScr1;
        private Forms.ToolStripMenuItem trayItemScr2;
        private Forms.ToolStripMenuItem trayItemBoth;

        public bool IsPinned { get; private set; }
        public byte BackgroundAlpha { get; private set; }
        private bool isExpanded = false;
        private bool isInputFocused = false;
        private int currentSlide = 0;

        private SolidColorBrush notchBgBrush;
        private static SpotlightWindow spotlightWin;

        // UI Elements
        private Border notchBody;
        private Grid collapsedView;
        private Grid expandedView;
        private TextBlock collapsedTimeText;
        private TextBlock expandedDateText;
        private TextBlock expandedTimeText;
        private Path collapsedBatteryArc;
        private Path expandedBatteryArc;
        private Border pinBtnBorder;
        private TextBlock pinBtnIcon;

        // Slides & Dots (5 slides)
        private Grid[] slidePanels = new Grid[SlideCount];
        private Border[] dotPills = new Border[SlideCount];

        // Slide 0: Media UI
        private Grid mediaStandardPanel;
        private Grid lyricsPanel;
        private Border albumArtBox;
        private ImageBrush albumImageBrush;
        private Path albumPlaceholderIcon;
        private TextBlock mediaTitleText;
        private TextBlock mediaArtistText;
        private TextBlock timeCurrentText;
        private TextBlock timeRemainingText;
        private Border progressTrackBorder;
        private Border progressFillBorder;
        private Path playPauseIconPath;
        private Border lyricsBtnBorder;
        private Path lyricsBtnIcon;
        private StackPanel lyricsStack;
        private ScrollViewer lyricsScroll;
        private TextBlock lyricsHeaderText;

        // Slide 1: System & Multi-Drive Storage UI
        private TextBlock sysBatVal, sysBatSub;
        private Border sysBatBar;
        private TextBlock sysCpuVal, sysCpuSub;
        private Border sysCpuBar;
        private TextBlock sysRamVal, sysRamSub;
        private Border sysRamBar;
        private Grid drivesContainerGrid;
        private string lastDrivesSignature = "";
        private List<TextBlock> driveValTexts = new List<TextBlock>();
        private List<Border> driveFillBars = new List<Border>();
        private List<TextBlock> driveSubTexts = new List<TextBlock>();

        // Slide 2: Control Center (Wi-Fi, Sound, Brightness) UI
        private TextBlock wifiSsidText;
        private TextBlock wifiSubText;
        private Border wifiSignalPill;
        private TextBlock soundIconText;
        private TextBlock soundValText;
        private Border soundTrackBorder;
        private Border soundFillBorder;
        private TextBlock brightValText;
        private Border brightTrackBorder;
        private Border brightFillBorder;
        private bool isDraggingSound = false;
        private bool isDraggingBright = false;

        // Internet speed UI
        private TextBlock netDownText;
        private TextBlock netUpText;

        // Collapsed music mode (album art + live waveform, like iPhone)
        private const double MusicCollapsedWidth = 196;
        private Grid collapsedIdleLayout;
        private Grid collapsedMusicLayout;
        private Border collapsedArtBox;
        private ImageBrush collapsedArtBrush;
        private Path collapsedArtPlaceholder;
        private TextBlock collapsedMusicTimeText;
        private Rectangle[] waveBars;
        private double[] waveLevels;
        private readonly double[] waveProfile = { 0.62, 0.92, 1.0, 0.8, 0.58 };
        private DispatcherTimer waveformTimer;
        private bool isMusicMode = false;
        private DateTime lastPlayingSeenUtc = DateTime.MinValue;
        private readonly Random waveRng = new Random();
        private readonly SolidColorBrush waveBrush = new SolidColorBrush(Color.FromRgb(28, 231, 85));

        private double CurrentCollapsedWidth
        {
            get { return isMusicMode ? MusicCollapsedWidth : CollapsedWidth; }
        }

        // Slide 3 & 4: Clipboard & Notes UI
        private StackPanel clipListPanel;
        private TextBox noteInputBox;
        private StackPanel notesListPanel;

        // Timers & State
        private DispatcherTimer clockTimer;
        private DispatcherTimer collapseDelayTimer;
        private DispatcherTimer clipboardTimer;
        private Forms.NotifyIcon trayIcon;

        // Media & Lyrics State
        private string currentTitle = "";
        private string currentArtist = "";
        private string currentStatus = "None";
        private double currentPos = 0;
        private double currentDur = 0;
        private DateTime lastPosSync = DateTime.UtcNow;
        private bool showLyrics = false;
        private string loadedLyricsKey = "";
        private List<LyricLine> syncedLyrics = new List<LyricLine>();
        private int activeLyricIndex = -1;

        // CPU delta tracking
        private long prevIdleTime = 0;
        private long prevKernelTime = 0;
        private long prevUserTime = 0;

        // Persistence paths
        private string dataDir;
        private string notesFilePath;
        private string clipFilePath;
        private string configFilePath;
        private List<NoteItem> notesList = new List<NoteItem>();
        private List<ClipItem> clipList = new List<ClipItem>();
        private string lastClipboardText = "";
        private JavaScriptSerializer json = new JavaScriptSerializer();

        public MainWindow(Forms.Screen targetScreen = null, bool isCompanion = false)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            IsCompanionWindow = isCompanion;
            if (!isCompanion)
            {
                PrimaryInstance = this;
            }

            BackgroundAlpha = 175;
            string oldDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsDynamicIsland");
            dataDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Glance");
            Directory.CreateDirectory(dataDir);
            if (Directory.Exists(oldDir))
            {
                try
                {
                    foreach (var f in Directory.GetFiles(oldDir))
                    {
                        string dest = System.IO.Path.Combine(dataDir, System.IO.Path.GetFileName(f));
                        if (!File.Exists(dest)) File.Copy(f, dest);
                    }
                }
                catch { }
            }
            notesFilePath = System.IO.Path.Combine(dataDir, "notes.json");
            clipFilePath = System.IO.Path.Combine(dataDir, "clipboard.json");
            configFilePath = System.IO.Path.Combine(dataDir, "config.json");

            if (!IsCompanionWindow)
            {
                LoadSavedData();
                SpotlightWindow.PrewarmIndexAsync();
            }
            else if (PrimaryInstance != null)
            {
                BackgroundAlpha = PrimaryInstance.BackgroundAlpha;
                IsPinned = PrimaryInstance.IsPinned;
                notesList = new List<NoteItem>(PrimaryInstance.notesList);
                clipList = new List<ClipItem>(PrimaryInstance.clipList);
            }

            Title = isCompanion ? "Glance (Companion)" : "Glance";
            Width = 490;
            Height = 255;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            Closed += (s, e) => MemoryOptimizer.TrimMemory();
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.NoResize;

            Forms.Screen initialScreen = targetScreen;
            if (initialScreen == null)
            {
                initialScreen = (DisplayMode == "screen2" && Forms.Screen.AllScreens.Length > 1)
                    ? GetScreen(2)
                    : GetScreen(1);
            }
            PositionOnScreen(initialScreen);

            if (!IsCompanionWindow)
            {
                SystemEvents.DisplaySettingsChanged += (s, e) => Dispatcher.Invoke(new Action(RepositionAllWindows));
            }

            BuildUI();

            if (!IsCompanionWindow)
            {
                SetupTrayIcon();
            }

            Loaded += OnWindowLoaded;
            Closing += (s, e) =>
            {
                if (!IsCompanionWindow)
                {
                    try
                    {
                        IntPtr hwnd = new WindowInteropHelper(this).Handle;
                        NativeMethods.UnregisterHotKey(hwnd, HOTKEY_ID_SPOTLIGHT);
                    }
                    catch { }

                    if (trayIcon != null)
                    {
                        trayIcon.Visible = false;
                        trayIcon.Dispose();
                    }

                    if (CompanionInstance != null)
                    {
                        try { CompanionInstance.Close(); } catch { }
                        CompanionInstance = null;
                    }
                }
            };
        }

        public void SetBackgroundAlpha(byte alpha)
        {
            BackgroundAlpha = alpha;
            if (notchBgBrush != null)
            {
                notchBgBrush.Color = Color.FromArgb(BackgroundAlpha, 6, 6, 10);
            }
            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.SetBackgroundAlpha(alpha);
            }
        }

        public void SaveAppConfig()
        {
            try
            {
                var cfg = new AppConfig
                {
                    backgroundAlpha = BackgroundAlpha,
                    isPinned = IsPinned,
                    displayMode = DisplayMode
                };
                File.WriteAllText(configFilePath, json.Serialize(cfg), Encoding.UTF8);
            }
            catch { }
        }

        public void PositionOnScreen(Forms.Screen screen)
        {
            if (screen == null) screen = Forms.Screen.PrimaryScreen;
            TargetScreen = screen;

            double dpiScaleX = 1.0;
            double dpiScaleY = 1.0;
            try
            {
                PresentationSource source = PresentationSource.FromVisual(this);
                if (source != null && source.CompositionTarget != null)
                {
                    dpiScaleX = source.CompositionTarget.TransformToDevice.M11;
                    dpiScaleY = source.CompositionTarget.TransformToDevice.M22;
                }
            }
            catch { }

            double screenDipLeft = screen.Bounds.X / dpiScaleX;
            double screenDipTop = screen.Bounds.Y / dpiScaleY;
            double screenDipWidth = screen.Bounds.Width / dpiScaleX;

            Left = screenDipLeft + Math.Round((screenDipWidth - Width) / 2.0);
            Top = screenDipTop;
        }

        public static Forms.Screen GetScreen(int index)
        {
            var screens = Forms.Screen.AllScreens;
            if (screens == null || screens.Length == 0) return Forms.Screen.PrimaryScreen;
            if (index == 1)
            {
                var pri = screens.FirstOrDefault(s => s.Primary);
                return pri ?? screens[0];
            }
            else if (index == 2)
            {
                var nonPri = screens.FirstOrDefault(s => !s.Primary);
                if (nonPri != null) return nonPri;
                if (screens.Length > 1) return screens[1];
                return screens[0];
            }
            return screens[0];
        }

        public static void ApplyDisplayMode(string mode)
        {
            if (string.IsNullOrEmpty(mode)) mode = "screen1";
            int screenCount = Forms.Screen.AllScreens.Length;
            if (screenCount < 2 && (mode == "screen2" || mode == "both"))
            {
                mode = "screen1";
            }

            DisplayMode = mode;
            if (PrimaryInstance == null) return;

            PrimaryInstance.Dispatcher.Invoke(() =>
            {
                if (DisplayMode == "screen2")
                {
                    PrimaryInstance.PositionOnScreen(GetScreen(2));
                    PrimaryInstance.Show();

                    if (CompanionInstance != null)
                    {
                        try { CompanionInstance.Close(); } catch { }
                        CompanionInstance = null;
                        MemoryOptimizer.TrimMemory();
                    }
                }
                else if (DisplayMode == "both")
                {
                    PrimaryInstance.PositionOnScreen(GetScreen(1));
                    PrimaryInstance.Show();

                    if (CompanionInstance == null)
                    {
                        CompanionInstance = new MainWindow(GetScreen(2), isCompanion: true);
                        CompanionInstance.Show();
                        if (PrimaryInstance.albumImageBrush != null)
                        {
                            BitmapSource curBmp = PrimaryInstance.albumImageBrush.ImageSource as BitmapSource;
                            if (curBmp != null)
                            {
                                CompanionInstance.SetAlbumArtVisuals(curBmp);
                            }
                        }
                    }
                    else
                    {
                        CompanionInstance.PositionOnScreen(GetScreen(2));
                        CompanionInstance.Show();
                    }
                }
                else // "screen1"
                {
                    PrimaryInstance.PositionOnScreen(GetScreen(1));
                    PrimaryInstance.Show();

                    if (CompanionInstance != null)
                    {
                        try { CompanionInstance.Close(); } catch { }
                        CompanionInstance = null;
                        MemoryOptimizer.TrimMemory();
                    }
                }

                PrimaryInstance.UpdateTrayDisplayModeMenu();
                PrimaryInstance.SaveAppConfig();
            });
        }

        public static void RepositionAllWindows()
        {
            if (PrimaryInstance != null)
            {
                ApplyDisplayMode(DisplayMode);
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TOOLWINDOW);

                if (TargetScreen != null)
                {
                    PositionOnScreen(TargetScreen);
                }

                collapseDelayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(240) };
                collapseDelayTimer.Tick += (s, ev) =>
                {
                    collapseDelayTimer.Stop();
                    if (!IsPinned && !isInputFocused && !notchBody.IsMouseOver)
                    {
                        AnimateToCollapsed();
                    }
                };

                waveformTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(45) };
                waveformTimer.Tick += OnWaveformTick;

                if (IsPinned)
                {
                    SetPinned(true);
                }

                if (!IsCompanionWindow)
                {
                    // Register Global Hotkey: Ctrl + Shift + P
                    HwndSource src = HwndSource.FromHwnd(hwnd);
                    if (src != null)
                    {
                        src.AddHook(WndProc);
                    }
                    NativeMethods.RegisterHotKey(
                        hwnd,
                        HOTKEY_ID_SPOTLIGHT,
                        NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT | NativeMethods.MOD_NOREPEAT,
                        NativeMethods.VK_P
                    );

                    clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                    clockTimer.Tick += OnFastTick;
                    clockTimer.Start();
                    OnFastTick(null, null);

                    clipboardTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
                    clipboardTimer.Tick += OnClipboardTick;
                    clipboardTimer.Start();

                    // Startup one-shot memory trimmer (flushes initial WPF / DirectX / font cache setup bloat)
                    var startupTrimTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.0) };
                    startupTrimTimer.Tick += (s, ev) =>
                    {
                        startupTrimTimer.Stop();
                        MemoryOptimizer.TrimMemory(force: true);
                    };
                    startupTrimTimer.Start();

                    // Periodic background memory trimmer (every 30 seconds when collapsed/idle)
                    var maintenanceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30.0) };
                    maintenanceTimer.Tick += (s, ev) =>
                    {
                        if (!isExpanded && (spotlightWin == null || !spotlightWin.IsVisible))
                        {
                            MemoryOptimizer.TrimMemory();
                        }
                    };
                    maintenanceTimer.Start();

                    Task.Run(() => BackgroundMediaAndSystemLoop());

                    ApplyDisplayMode(DisplayMode);
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText("d:\\Coding-an\\windows-notch\\error.log", "OnWindowLoaded error: " + ex.ToString());
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID_SPOTLIGHT)
            {
                handled = true;
                ToggleSpotlightSearch();
            }
            return IntPtr.Zero;
        }

        public void ToggleSpotlightSearch()
        {
            if (spotlightWin == null)
            {
                spotlightWin = new SpotlightWindow();
            }

            if (spotlightWin.IsVisible)
            {
                spotlightWin.HideSpotlight();
            }
            else
            {
                spotlightWin.ShowSpotlight();
            }
        }

        public void SetPinned(bool pinned)
        {
            IsPinned = pinned;
            pinBtnBorder.Background = IsPinned
                ? new SolidColorBrush(Color.FromArgb(55, 28, 231, 85))
                : Brushes.Transparent;
            pinBtnIcon.Foreground = IsPinned
                ? new SolidColorBrush(Color.FromRgb(28, 231, 85))
                : new SolidColorBrush(Color.FromRgb(140, 140, 150));

            if (!IsCompanionWindow)
            {
                SaveAppConfig();
                if (CompanionInstance != null && CompanionInstance.IsPinned != pinned)
                {
                    CompanionInstance.SetPinned(pinned);
                }
            }
            else if (PrimaryInstance != null && PrimaryInstance.IsPinned != pinned)
            {
                PrimaryInstance.SetPinned(pinned);
            }

            if (IsPinned)
            {
                AnimateToExpanded();
            }
            else if (!notchBody.IsMouseOver && !isInputFocused)
            {
                AnimateToCollapsed();
            }
        }

        private void BuildUI()
        {
            notchBgBrush = new SolidColorBrush(Color.FromArgb(BackgroundAlpha, 6, 6, 10));

            Grid rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            StackPanel notchRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top
            };

            Path leftEar = new Path
            {
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Top,
                Fill = notchBgBrush,
                Data = Geometry.Parse("M 0,0 L 16,0 L 16,16 A 16,16 0 0 0 0,0 Z"),
                SnapsToDevicePixels = true
            };

            Path rightEar = new Path
            {
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Top,
                Fill = notchBgBrush,
                Data = Geometry.Parse("M 0,0 L 16,0 A 16,16 0 0 0 0,16 Z"),
                SnapsToDevicePixels = true
            };

            notchBody = new Border
            {
                Width = CollapsedWidth,
                Height = CollapsedHeight,
                Background = notchBgBrush,
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(0, 0, 19, 19),
                VerticalAlignment = VerticalAlignment.Top,
                SnapsToDevicePixels = true
            };

            notchBody.MouseEnter += (s, e) =>
            {
                collapseDelayTimer.Stop();
                AnimateToExpanded();
            };

            notchBody.MouseLeave += (s, e) =>
            {
                if (!IsPinned && !isInputFocused)
                {
                    collapseDelayTimer.Start();
                }
            };

            Grid innerContainer = new Grid();
            collapsedView = BuildCollapsedView();
            expandedView = BuildExpandedView();
            expandedView.Opacity = 0;
            expandedView.IsHitTestVisible = false;

            innerContainer.Children.Add(collapsedView);
            innerContainer.Children.Add(expandedView);
            notchBody.Child = innerContainer;

            notchRow.Children.Add(leftEar);
            notchRow.Children.Add(notchBody);
            notchRow.Children.Add(rightEar);

            rootGrid.Children.Add(notchRow);
            Content = rootGrid;
        }

        private Grid BuildCollapsedView()
        {
            Grid root = new Grid();

            // ---- Idle layout: battery ring + clock (original look) ----
            collapsedIdleLayout = new Grid
            {
                Margin = new Thickness(15, 0, 22, 2),
                VerticalAlignment = VerticalAlignment.Stretch
            };

            Grid batRing = CreateBatteryRingWidget(24, 3.3, out collapsedBatteryArc);
            batRing.HorizontalAlignment = HorizontalAlignment.Left;
            batRing.VerticalAlignment = VerticalAlignment.Center;

            collapsedTimeText = new TextBlock
            {
                Text = "11:53",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            collapsedIdleLayout.Children.Add(batRing);
            collapsedIdleLayout.Children.Add(collapsedTimeText);

            // ---- Music layout: mini album art + clock + live waveform (like iPhone) ----
            collapsedMusicLayout = new Grid
            {
                Margin = new Thickness(10, 0, 16, 2),
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Collapsed
            };

            collapsedArtBox = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            collapsedArtBrush = new ImageBrush { Stretch = Stretch.UniformToFill };
            collapsedArtPlaceholder = new Path
            {
                Data = Geometry.Parse("M 12,3 L 12,13.55 C 11.41,13.21 10.73,13 10,13 C 7.79,13 6,14.79 6,17 C 6,19.21 7.79,21 10,21 C 12.21,21 14,19.21 14,17 L 14,7 L 18,7 L 18,3 L 12,3 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(190, 190, 200)),
                Width = 12,
                Height = 12,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            collapsedArtBox.Child = collapsedArtPlaceholder;

            collapsedMusicTimeText = new TextBlock
            {
                Text = "11:53",
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            StackPanel wavePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 18,
                ToolTip = "Musik sedang diputar"
            };
            int barCount = waveProfile.Length;
            waveBars = new Rectangle[barCount];
            waveLevels = new double[barCount];
            for (int i = 0; i < barCount; i++)
            {
                waveLevels[i] = 0.15;
                Rectangle bar = new Rectangle
                {
                    Width = 3,
                    Height = 5,
                    RadiusX = 1.5,
                    RadiusY = 1.5,
                    Fill = waveBrush,
                    Margin = new Thickness(1.1, 0, 1.1, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                waveBars[i] = bar;
                wavePanel.Children.Add(bar);
            }

            collapsedMusicLayout.Children.Add(collapsedArtBox);
            collapsedMusicLayout.Children.Add(collapsedMusicTimeText);
            collapsedMusicLayout.Children.Add(wavePanel);

            root.Children.Add(collapsedIdleLayout);
            root.Children.Add(collapsedMusicLayout);
            return root;
        }

        private Grid BuildExpandedView()
        {
            Grid g = new Grid();

            // 1. Top Status Bar
            Grid topHeader = new Grid
            {
                Height = 28,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(14, 6, 14, 0)
            };

            // Top-Left Search Button (Quick trigger for Ctrl + Shift + P Spotlight Search)
            Border searchQuickBtn = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ToolTip = "Spotlight Search Aplikasi (Ctrl + Shift + P)"
            };
            TextBlock searchQuickIcon = new TextBlock
            {
                Text = "🔍",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            searchQuickBtn.Child = searchQuickIcon;
            searchQuickBtn.MouseEnter += (s, e) => searchQuickIcon.Foreground = Brushes.White;
            searchQuickBtn.MouseLeave += (s, e) => searchQuickIcon.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160));
            searchQuickBtn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                ToggleSpotlightSearch();
            };
            topHeader.Children.Add(searchQuickBtn);

            StackPanel centerStatus = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid smallBatRing = CreateBatteryRingWidget(18, 2.8, out expandedBatteryArc);
            smallBatRing.Margin = new Thickness(0, 0, 7, 0);

            expandedDateText = new TextBlock
            {
                Text = "Fri 23 Oct 2026",
                Foreground = new SolidColorBrush(Color.FromRgb(235, 235, 240)),
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };

            expandedTimeText = new TextBlock
            {
                Text = "11:54",
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            centerStatus.Children.Add(smallBatRing);
            centerStatus.Children.Add(expandedDateText);
            centerStatus.Children.Add(expandedTimeText);
            topHeader.Children.Add(centerStatus);

            // Top-Right Action Icons (Settings Gear + Pin)
            StackPanel rightActions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            Border settingsBtn = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = "Pengaturan Transparansi & Run at Startup Windows",
                Margin = new Thickness(0, 0, 4, 0)
            };
            TextBlock gearIcon = new TextBlock
            {
                Text = "⚙",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            settingsBtn.Child = gearIcon;
            settingsBtn.MouseEnter += (s, e) => gearIcon.Foreground = Brushes.White;
            settingsBtn.MouseLeave += (s, e) => gearIcon.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160));
            settingsBtn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                OpenSettingsDialog();
            };

            pinBtnBorder = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = "Kunci agar tetap terbuka (Pin)"
            };
            pinBtnIcon = new TextBlock
            {
                Text = "📌",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            pinBtnBorder.Child = pinBtnIcon;
            pinBtnBorder.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                SetPinned(!IsPinned);
            };

            rightActions.Children.Add(settingsBtn);
            rightActions.Children.Add(pinBtnBorder);
            topHeader.Children.Add(rightActions);

            g.Children.Add(topHeader);

            // 2. Left "<" and Right ">" Carousel Circular Buttons
            Border prevSlideBtn = CreateNavCircleButton(true);
            prevSlideBtn.HorizontalAlignment = HorizontalAlignment.Left;
            prevSlideBtn.VerticalAlignment = VerticalAlignment.Center;
            prevSlideBtn.Margin = new Thickness(12, 0, 0, 0);
            prevSlideBtn.MouseLeftButtonUp += (s, e) => { e.Handled = true; SwitchSlide(currentSlide - 1); };

            Border nextSlideBtn = CreateNavCircleButton(false);
            nextSlideBtn.HorizontalAlignment = HorizontalAlignment.Right;
            nextSlideBtn.VerticalAlignment = VerticalAlignment.Center;
            nextSlideBtn.Margin = new Thickness(0, 0, 12, 0);
            nextSlideBtn.MouseLeftButtonUp += (s, e) => { e.Handled = true; SwitchSlide(currentSlide + 1); };

            g.Children.Add(prevSlideBtn);
            g.Children.Add(nextSlideBtn);

            // 3. Slides Container (5 Slides)
            Grid slidesHost = new Grid
            {
                Margin = new Thickness(42, 34, 42, 24)
            };

            slidePanels[0] = BuildMediaSlide();
            slidePanels[1] = BuildSystemAndDrivesSlide();
            slidePanels[2] = BuildControlCenterSlide();
            slidePanels[3] = BuildClipboardSlide();
            slidePanels[4] = BuildNotesSlide();

            for (int i = 0; i < SlideCount; i++)
            {
                slidePanels[i].Visibility = (i == 0) ? Visibility.Visible : Visibility.Collapsed;
                slidesHost.Children.Add(slidePanels[i]);
            }
            g.Children.Add(slidesHost);

            // 4. Bottom Pagination Dots
            StackPanel dotsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 9)
            };

            string[] slideTitles =
            {
                "Media Player & Lirik",
                "Baterai, CPU, RAM & Disk (C: / D:)",
                "Wi-Fi, Sound & Brightness",
                "Riwayat Clipboard",
                "Catatan Cepat"
            };
            for (int i = 0; i < SlideCount; i++)
            {
                int idx = i;
                Border dot = new Border
                {
                    Width = (i == 0) ? 18 : 5,
                    Height = 5,
                    CornerRadius = new CornerRadius(2.5),
                    Background = (i == 0) ? Brushes.White : new SolidColorBrush(Color.FromArgb(140, 130, 130, 140)),
                    Margin = new Thickness(3, 0, 3, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = slideTitles[i]
                };
                dot.MouseLeftButtonUp += (s, e) => { e.Handled = true; SwitchSlide(idx); };
                dotPills[i] = dot;
                dotsPanel.Children.Add(dot);
            }
            g.Children.Add(dotsPanel);

            return g;
        }

        // =======================================================
        // SLIDE 0: MEDIA PLAYER + LYRICS
        // =======================================================
        private Grid BuildMediaSlide()
        {
            Grid slide = new Grid();
            slide.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            slide.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });

            mediaStandardPanel = new Grid();
            mediaStandardPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(72) });
            mediaStandardPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

            Grid topRow = new Grid { Margin = new Thickness(0, 2, 0, 0) };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            albumArtBox = new Border
            {
                Width = 66,
                Height = 66,
                CornerRadius = new CornerRadius(13),
                Background = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            albumImageBrush = new ImageBrush { Stretch = Stretch.UniformToFill };

            albumPlaceholderIcon = new Path
            {
                Data = Geometry.Parse("M 12,3 L 12,13.55 C 11.41,13.21 10.73,13 10,13 C 7.79,13 6,14.79 6,17 C 6,19.21 7.79,21 10,21 C 12.21,21 14,19.21 14,17 L 14,7 L 18,7 L 18,3 L 12,3 Z"),
                Fill = new SolidColorBrush(Color.FromArgb(160, 180, 180, 190)),
                Width = 30,
                Height = 30,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            albumArtBox.Child = albumPlaceholderIcon;
            Grid.SetColumn(albumArtBox, 0);

            StackPanel titleCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            mediaTitleText = new TextBlock
            {
                Text = "Tidak ada media",
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            mediaArtistText = new TextBlock
            {
                Text = "",
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 2, 0, 0),
                Visibility = Visibility.Collapsed
            };
            titleCol.Children.Add(mediaTitleText);
            titleCol.Children.Add(mediaArtistText);
            Grid.SetColumn(titleCol, 2);

            topRow.Children.Add(albumArtBox);
            topRow.Children.Add(titleCol);
            Grid.SetRow(topRow, 0);

            Grid progRow = new Grid { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) };
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });

            timeCurrentText = new TextBlock
            {
                Text = "00:00",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timeCurrentText, 0);

            progressTrackBorder = new Border
            {
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                Background = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ClipToBounds = true
            };
            progressFillBorder = new Border
            {
                Height = 5,
                Width = 0,
                CornerRadius = new CornerRadius(2.5),
                Background = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            progressTrackBorder.Child = progressFillBorder;
            progressTrackBorder.MouseLeftButtonDown += OnProgressTrackClick;
            Grid.SetColumn(progressTrackBorder, 2);

            timeRemainingText = new TextBlock
            {
                Text = "-00:00",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
                FontSize = 12.5,
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timeRemainingText, 4);

            progRow.Children.Add(timeCurrentText);
            progRow.Children.Add(progressTrackBorder);
            progRow.Children.Add(timeRemainingText);
            Grid.SetRow(progRow, 1);

            mediaStandardPanel.Children.Add(topRow);
            mediaStandardPanel.Children.Add(progRow);
            Grid.SetRow(mediaStandardPanel, 0);

            // Lyrics Overlay Panel
            lyricsPanel = new Grid { Visibility = Visibility.Collapsed };
            lyricsPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
            lyricsPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            lyricsHeaderText = new TextBlock
            {
                Text = "Lirik Lagu",
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetRow(lyricsHeaderText, 0);

            lyricsScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 2, 0, 2)
            };
            lyricsStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            lyricsScroll.Content = lyricsStack;
            Grid.SetRow(lyricsScroll, 1);

            lyricsPanel.Children.Add(lyricsHeaderText);
            lyricsPanel.Children.Add(lyricsScroll);
            Grid.SetRow(lyricsPanel, 0);

            // Bottom Media Controls Row
            Grid controlsRow = new Grid { Margin = new Thickness(0, 2, 0, 0) };

            lyricsBtnBorder = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(7),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Tampilkan / Sembunyikan Lirik Lagu"
            };
            lyricsBtnIcon = new Path
            {
                Data = Geometry.Parse("M 4,4 L 20,4 C 21.1,4 22,4.9 22,6 L 22,16 C 22,17.1 21.1,18 20,18 L 8,18 L 4,22 L 4,6 C 4,4.9 4.9,4 4,4 Z M 8,9 L 11,9 L 11,12 L 9.5,14 L 8,14 L 9.2,12 L 8,12 Z M 13,9 L 16,9 L 16,12 L 14.5,14 L 13,14 L 14.2,12 L 13,12 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                Width = 17,
                Height = 17,
                Stretch = Stretch.Uniform
            };
            lyricsBtnBorder.Child = lyricsBtnIcon;
            lyricsBtnBorder.MouseEnter += (s, e) => { if (!showLyrics) lyricsBtnIcon.Fill = Brushes.White; };
            lyricsBtnBorder.MouseLeave += (s, e) => { if (!showLyrics) lyricsBtnIcon.Fill = new SolidColorBrush(Color.FromRgb(150, 150, 160)); };
            lyricsBtnBorder.MouseLeftButtonUp += (s, e) => { e.Handled = true; ToggleLyricsView(); };
            controlsRow.Children.Add(lyricsBtnBorder);

            StackPanel transport = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Border btnPrev = CreateMediaIconBtn("M 11,18 L 11,6 L 2.5,12 Z M 11.5,12 L 20,18 L 20,6 Z", 19, "Lagu Sebelumnya");
            btnPrev.MouseLeftButtonUp += (s, e) => { e.Handled = true; pendingCommand = "prev"; };

            Border btnPlay = CreateMediaIconBtn("M 8,5 L 8,19 L 19,12 Z", 20, "Putar / Jeda", out playPauseIconPath);
            btnPlay.Margin = new Thickness(24, 0, 24, 0);
            btnPlay.MouseLeftButtonUp += (s, e) => { e.Handled = true; pendingCommand = "play-pause"; };

            Border btnNext = CreateMediaIconBtn("M 4,18 L 12.5,12 L 4,6 Z M 13,6 L 13,18 L 21.5,12 Z", 19, "Lagu Berikutnya");
            btnNext.MouseLeftButtonUp += (s, e) => { e.Handled = true; pendingCommand = "next"; };

            transport.Children.Add(btnPrev);
            transport.Children.Add(btnPlay);
            transport.Children.Add(btnNext);
            controlsRow.Children.Add(transport);

            Grid.SetRow(controlsRow, 1);

            slide.Children.Add(mediaStandardPanel);
            slide.Children.Add(lyricsPanel);
            slide.Children.Add(controlsRow);
            return slide;
        }

        // =======================================================
        // SLIDE 1: SYSTEM + SEPARATED STORAGE DRIVES (C:, D:, etc.)
        // =======================================================
        private Grid BuildSystemAndDrivesSlide()
        {
            Grid g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(7) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Top Row: 3 Columns [Baterai] [CPU] [RAM]
            Grid topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border batCard = CreateTransparentStatCard("Baterai", Color.FromRgb(28, 231, 85), out sysBatVal, out sysBatBar, out sysBatSub);
            Grid.SetColumn(batCard, 0);

            Border cpuCard = CreateTransparentStatCard("CPU", Color.FromRgb(56, 189, 248), out sysCpuVal, out sysCpuBar, out sysCpuSub);
            Grid.SetColumn(cpuCard, 2);

            Border ramCard = CreateTransparentStatCard("RAM", Color.FromRgb(192, 132, 252), out sysRamVal, out sysRamBar, out sysRamSub);
            Grid.SetColumn(ramCard, 4);

            topRow.Children.Add(batCard);
            topRow.Children.Add(cpuCard);
            topRow.Children.Add(ramCard);
            Grid.SetRow(topRow, 0);

            drivesContainerGrid = new Grid();
            Grid.SetRow(drivesContainerGrid, 2);

            g.Children.Add(topRow);
            g.Children.Add(drivesContainerGrid);
            return g;
        }

        private Border CreateTransparentStatCard(string label, Color accent, out TextBlock valText, out Border fillBar, out TextBlock subText)
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(18, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(9, 6, 9, 6)
            };

            StackPanel sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            DockPanel top = new DockPanel();
            TextBlock lbl = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(210, 210, 220)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            valText = new TextBlock
            {
                Text = "0%",
                Foreground = new SolidColorBrush(accent),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(valText, Dock.Right);
            top.Children.Add(valText);
            top.Children.Add(lbl);

            Border barTrack = new Border
            {
                Height = 4.5,
                CornerRadius = new CornerRadius(2.2),
                Background = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                Margin = new Thickness(0, 5, 0, 5),
                ClipToBounds = true
            };
            fillBar = new Border
            {
                Height = 4.5,
                Width = 0,
                CornerRadius = new CornerRadius(2.2),
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            barTrack.Child = fillBar;

            subText = new TextBlock
            {
                Text = "...",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 172)),
                FontSize = 10,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            sp.Children.Add(top);
            sp.Children.Add(barTrack);
            sp.Children.Add(subText);
            card.Child = sp;
            return card;
        }

        private void RebuildDrivesGrid(List<DriveStatInfo> drives)
        {
            drivesContainerGrid.Children.Clear();
            drivesContainerGrid.ColumnDefinitions.Clear();
            driveValTexts.Clear();
            driveFillBars.Clear();
            driveSubTexts.Clear();

            Color[] driveAccents =
            {
                Color.FromRgb(251, 191, 36),
                Color.FromRgb(45, 212, 191),
                Color.FromRgb(244, 114, 182),
                Color.FromRgb(96, 165, 250)
            };

            int count = Math.Max(1, drives.Count);
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    drivesContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
                }
                drivesContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                string drvLabel = i < drives.Count ? ("Disk (" + drives[i].Name.TrimEnd('\\') + ")") : "Disk";
                Color accent = driveAccents[i % driveAccents.Length];

                TextBlock valTxt, subTxt;
                Border fillBar;
                Border card = CreateTransparentStatCard(drvLabel, accent, out valTxt, out fillBar, out subTxt);
                Grid.SetColumn(card, i * 2);
                drivesContainerGrid.Children.Add(card);

                driveValTexts.Add(valTxt);
                driveFillBars.Add(fillBar);
                driveSubTexts.Add(subTxt);
            }
        }

        // =======================================================
        // SLIDE 2: CONTROL CENTER (WI-FI, SOUND, & BRIGHTNESS)
        // =======================================================
        private Grid BuildControlCenterSlide()
        {
            Grid g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

            Border wifiCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(18, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(11, 6, 11, 6),
                Cursor = Cursors.Hand,
                ToolTip = "Klik untuk membuka pengaturan Wi-Fi Windows"
            };
            wifiCard.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                try
                {
                    Process.Start(new ProcessStartInfo("ms-availablenetworks:") { UseShellExecute = true });
                }
                catch
                {
                    try { Process.Start(new ProcessStartInfo("ms-settings:network-wifi") { UseShellExecute = true }); } catch { }
                }
            };

            DockPanel wifiDock = new DockPanel { VerticalAlignment = VerticalAlignment.Center };

            // Right side: real-time internet speed (Download / Upload)
            StackPanel speedStack = new StackPanel
            {
                Width = 96,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Kecepatan internet real-time (Download / Upload)"
            };
            netDownText = new TextBlock
            {
                Text = "↓ 0 KB/s",
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };
            netUpText = new TextBlock
            {
                Text = "↑ 0 KB/s",
                Foreground = new SolidColorBrush(Color.FromRgb(251, 146, 60)),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 1, 0, 0)
            };
            speedStack.Children.Add(netDownText);
            speedStack.Children.Add(netUpText);
            DockPanel.SetDock(speedStack, Dock.Right);

            // Small signal-strength pill shown next to the "Wi-Fi" label
            wifiSignalPill = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(45, 28, 231, 85)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5, 0, 5, 0),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            wifiSubText = new TextBlock
            {
                Text = "0%",
                Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold
            };
            wifiSignalPill.Child = wifiSubText;

            StackPanel wifiLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border wifiIconCircle = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(55, 56, 189, 248)),
                Margin = new Thickness(0, 0, 10, 0)
            };
            Path wifiIconPath = new Path
            {
                Data = Geometry.Parse("M 1,9 L 3,11 C 7.97,6.03 16.03,6.03 21,11 L 23,9 C 16.93,2.93 7.08,2.93 1,9 Z M 9,17 L 12,20 L 15,17 C 13.35,15.34 10.66,15.34 9,17 Z M 5,13 L 7,15 C 9.76,12.24 14.24,12.24 17,15 L 19,13 C 15.14,9.14 8.87,9.14 5,13 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                Width = 15,
                Height = 15,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            wifiIconCircle.Child = wifiIconPath;

            StackPanel wifiTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            StackPanel wifiLabelRow = new StackPanel { Orientation = Orientation.Horizontal };
            wifiLabelRow.Children.Add(new TextBlock
            {
                Text = "Wi-Fi",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 175)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
            wifiLabelRow.Children.Add(wifiSignalPill);
            wifiTextStack.Children.Add(wifiLabelRow);
            wifiSsidText = new TextBlock
            {
                Text = "Memeriksa...",
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 150
            };
            wifiTextStack.Children.Add(wifiSsidText);

            wifiLeft.Children.Add(wifiIconCircle);
            wifiLeft.Children.Add(wifiTextStack);

            wifiDock.Children.Add(speedStack);
            wifiDock.Children.Add(wifiLeft);
            wifiCard.Child = wifiDock;
            Grid.SetRow(wifiCard, 0);

            // Sound Slider Card
            Border soundCard = CreateSliderControlCard(
                "🔊",
                "Sound",
                Color.FromRgb(56, 189, 248),
                out soundIconText,
                out soundTrackBorder,
                out soundFillBorder,
                out soundValText
            );
            soundIconText.Cursor = Cursors.Hand;
            soundIconText.ToolTip = "Klik untuk Mute / Unmute Suara";
            soundIconText.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                Task.Run(() => HardwareController.ToggleAudioMute());
            };
            AttachInteractiveSlider(soundTrackBorder, soundFillBorder, soundValText, true);
            Grid.SetRow(soundCard, 2);

            // Brightness Slider Card
            TextBlock dummyBrightIcon;
            Border brightCard = CreateSliderControlCard(
                "☀",
                "Brightness",
                Color.FromRgb(251, 191, 36),
                out dummyBrightIcon,
                out brightTrackBorder,
                out brightFillBorder,
                out brightValText
            );
            AttachInteractiveSlider(brightTrackBorder, brightFillBorder, brightValText, false);
            Grid.SetRow(brightCard, 4);

            g.Children.Add(wifiCard);
            g.Children.Add(soundCard);
            g.Children.Add(brightCard);
            return g;
        }

        private Border CreateSliderControlCard(
            string iconChar,
            string titleLabel,
            Color accent,
            out TextBlock iconTextBlock,
            out Border trackBorder,
            out Border fillBorder,
            out TextBlock percentTextBlock)
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(18, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(11),
                Padding = new Thickness(11, 5, 11, 5)
            };

            Grid row = new Grid { VerticalAlignment = VerticalAlignment.Center };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });

            iconTextBlock = new TextBlock
            {
                Text = iconChar,
                Foreground = new SolidColorBrush(accent),
                FontSize = 13.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconTextBlock, 0);

            TextBlock lbl = new TextBlock
            {
                Text = titleLabel,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 230)),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 1);

            trackBorder = new Border
            {
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ClipToBounds = true
            };
            fillBorder = new Border
            {
                Height = 8,
                Width = 0,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            trackBorder.Child = fillBorder;
            Grid.SetColumn(trackBorder, 2);

            percentTextBlock = new TextBlock
            {
                Text = "50%",
                Foreground = new SolidColorBrush(accent),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(percentTextBlock, 4);

            row.Children.Add(iconTextBlock);
            row.Children.Add(lbl);
            row.Children.Add(trackBorder);
            row.Children.Add(percentTextBlock);
            card.Child = row;
            return card;
        }

        private void AttachInteractiveSlider(Border track, Border fill, TextBlock valLabel, bool isAudioSlider)
        {
            Action<MouseEventArgs> updateFromMouse = (e) =>
            {
                if (track.ActualWidth <= 0) return;
                Point pt = e.GetPosition(track);
                double ratio = Math.Max(0.0, Math.Min(1.0, pt.X / track.ActualWidth));
                int pct = (int)Math.Round(ratio * 100.0);
                fill.Width = track.ActualWidth * ratio;
                valLabel.Text = pct + "%";

                if (isAudioSlider)
                {
                    Task.Run(() => HardwareController.SetAudioVolume(pct));
                }
                else
                {
                    Task.Run(() => HardwareController.SetScreenBrightness(pct));
                }
            };

            track.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (isAudioSlider) isDraggingSound = true;
                else isDraggingBright = true;
                track.CaptureMouse();
                updateFromMouse(e);
            };

            track.MouseMove += (s, e) =>
            {
                if ((isAudioSlider && isDraggingSound) || (!isAudioSlider && isDraggingBright))
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        updateFromMouse(e);
                    }
                }
            };

            track.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                if (isAudioSlider) isDraggingSound = false;
                else isDraggingBright = false;
                track.ReleaseMouseCapture();
            };
        }

        // =======================================================
        // SLIDE 3: CLIPBOARD MANAGER
        // =======================================================
        private Grid BuildClipboardSlide()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            DockPanel header = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            TextBlock title = new TextBlock
            {
                Text = "Riwayat Clipboard (Ctrl+C)",
                Foreground = new SolidColorBrush(Color.FromRgb(228, 228, 235)),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Border clearBtn = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            TextBlock clearTxt = new TextBlock
            {
                Text = "Bersihkan",
                Foreground = new SolidColorBrush(Color.FromRgb(210, 210, 220)),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            clearBtn.Child = clearTxt;
            clearBtn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                clipList.RemoveAll(c => !c.pinned);
                SaveClipData();
                RenderClipboardList();
            };
            DockPanel.SetDock(clearBtn, Dock.Right);
            header.Children.Add(clearBtn);
            header.Children.Add(title);
            Grid.SetRow(header, 0);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            clipListPanel = new StackPanel();
            sv.Content = clipListPanel;
            Grid.SetRow(sv, 1);

            g.Children.Add(header);
            g.Children.Add(sv);
            RenderClipboardList();
            return g;
        }

        // =======================================================
        // SLIDE 4: QUICK NOTES
        // =======================================================
        private Grid BuildNotesSlide()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid inputRow = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            noteInputBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Foreground = Brushes.White,
                CaretBrush = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(7, 3, 7, 3),
                FontSize = 11.5,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            noteInputBox.GotFocus += (s, e) => isInputFocused = true;
            noteInputBox.LostFocus += (s, e) => isInputFocused = false;
            noteInputBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    AddNoteFromInput();
                }
            };
            Grid.SetColumn(noteInputBox, 0);

            Border addBtn = new Border
            {
                Width = 26,
                Height = 25,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                Cursor = Cursors.Hand
            };
            addBtn.Child = new TextBlock
            {
                Text = "+",
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            addBtn.MouseLeftButtonUp += (s, e) => { e.Handled = true; AddNoteFromInput(); };
            Grid.SetColumn(addBtn, 2);

            inputRow.Children.Add(noteInputBox);
            inputRow.Children.Add(addBtn);
            Grid.SetRow(inputRow, 0);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            notesListPanel = new StackPanel();
            sv.Content = notesListPanel;
            Grid.SetRow(sv, 1);

            g.Children.Add(inputRow);
            g.Children.Add(sv);
            RenderNotesList();
            return g;
        }

        // =======================================================
        // WIDGET HELPERS
        // =======================================================
        private Grid CreateBatteryRingWidget(double size, double strokeThickness, out Path fgArc)
        {
            Grid wrap = new Grid { Width = size, Height = size };
            double radius = (size - strokeThickness) / 2.0;
            Point center = new Point(size / 2.0, size / 2.0);

            Ellipse bgRing = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(Color.FromArgb(60, 28, 231, 85)),
                StrokeThickness = strokeThickness
            };

            fgArc = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = BuildArcGeometry(center, radius, 100)
            };

            Path bolt = new Path
            {
                Data = Geometry.Parse("M 13,2 L 4.5,13.5 L 11,13.5 L 10,22 L 19.5,10.5 L 13,10.5 Z"),
                Fill = Brushes.White,
                Width = size * 0.46,
                Height = size * 0.46,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            wrap.Children.Add(bgRing);
            wrap.Children.Add(fgArc);
            wrap.Children.Add(bolt);
            return wrap;
        }

        private Geometry BuildArcGeometry(Point center, double radius, double percent)
        {
            double pct = Math.Max(2, Math.Min(99.9, percent));
            double angleRad = (pct / 100.0) * 2.0 * Math.PI;
            Point start = new Point(center.X, center.Y - radius);
            Point end = new Point(
                center.X + radius * Math.Sin(angleRad),
                center.Y - radius * Math.Cos(angleRad)
            );
            bool isLargeArc = pct > 50.0;

            PathFigure fig = new PathFigure { StartPoint = start, IsClosed = false };
            fig.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            });
            PathGeometry geo = new PathGeometry();
            geo.Figures.Add(fig);
            return geo;
        }

        private Border CreateNavCircleButton(bool isLeft)
        {
            Border b = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromArgb(75, 255, 255, 255)),
                Cursor = Cursors.Hand,
                ToolTip = isLeft ? "Halaman Sebelumnya" : "Halaman Berikutnya"
            };
            Path arrow = new Path
            {
                Data = Geometry.Parse(isLeft ? "M 14,6 L 8,12 L 14,18" : "M 10,6 L 16,12 L 10,18"),
                Stroke = Brushes.White,
                StrokeThickness = 2.2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Width = 11,
                Height = 11,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            b.Child = arrow;
            b.MouseEnter += (s, e) => b.Background = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255));
            b.MouseLeave += (s, e) => b.Background = new SolidColorBrush(Color.FromArgb(75, 255, 255, 255));
            return b;
        }

        private Border CreateMediaIconBtn(string pathData, double iconSize, string tooltip)
        {
            Path dummy;
            return CreateMediaIconBtn(pathData, iconSize, tooltip, out dummy);
        }

        private Border CreateMediaIconBtn(string pathData, double iconSize, string tooltip, out Path iconPath)
        {
            Border b = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(7),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = tooltip
            };
            Path p = new Path
            {
                Data = Geometry.Parse(pathData),
                Fill = new SolidColorBrush(Color.FromRgb(165, 165, 175)),
                Width = iconSize,
                Height = iconSize,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconPath = p;
            b.Child = p;
            b.MouseEnter += (s, e) => p.Fill = Brushes.White;
            b.MouseLeave += (s, e) => p.Fill = new SolidColorBrush(Color.FromRgb(165, 165, 175));
            return b;
        }

        // =======================================================
        // ANIMATIONS
        // =======================================================
        private void AnimateToExpanded()
        {
            if (isExpanded) return;
            isExpanded = true;

            notchBody.CornerRadius = new CornerRadius(0, 0, 32, 32);

            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            Duration dur = new Duration(TimeSpan.FromMilliseconds(300));

            notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(ExpandedWidth, dur) { EasingFunction = ease });
            notchBody.BeginAnimation(HeightProperty, new DoubleAnimation(ExpandedHeight, dur) { EasingFunction = ease });

            collapsedView.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(120)));
            collapsedView.IsHitTestVisible = false;

            expandedView.IsHitTestVisible = true;
            expandedView.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220))
            {
                BeginTime = TimeSpan.FromMilliseconds(50)
            });
        }

        private void AnimateToCollapsed()
        {
            if (!isExpanded) return;
            isExpanded = false;

            notchBody.CornerRadius = new CornerRadius(0, 0, 19, 19);

            CubicEase ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            Duration dur = new Duration(TimeSpan.FromMilliseconds(280));

            var widthAnim = new DoubleAnimation(CurrentCollapsedWidth, dur) { EasingFunction = ease };
            widthAnim.Completed += (s, e) => MemoryOptimizer.TrimMemory();

            notchBody.BeginAnimation(WidthProperty, widthAnim);
            notchBody.BeginAnimation(HeightProperty, new DoubleAnimation(CollapsedHeight, dur) { EasingFunction = ease });

            expandedView.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(110)));
            expandedView.IsHitTestVisible = false;

            collapsedView.IsHitTestVisible = true;
            collapsedView.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200))
            {
                BeginTime = TimeSpan.FromMilliseconds(60)
            });
        }

        private void SwitchSlide(int index)
        {
            currentSlide = (index + SlideCount) % SlideCount;
            for (int i = 0; i < SlideCount; i++)
            {
                slidePanels[i].Visibility = (i == currentSlide) ? Visibility.Visible : Visibility.Collapsed;
                dotPills[i].Width = (i == currentSlide) ? 18 : 5;
                dotPills[i].Background = (i == currentSlide)
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromArgb(140, 130, 130, 140));
            }
        }

        // =======================================================
        // FAST UI TICK
        // =======================================================
        private void OnFastTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            string timeStr = now.ToString("HH:mm", CultureInfo.InvariantCulture);
            string dateStr = now.ToString("ddd dd MMM yyyy", CultureInfo.InvariantCulture);

            collapsedTimeText.Text = timeStr;
            expandedTimeText.Text = timeStr;
            if (collapsedMusicTimeText != null) collapsedMusicTimeText.Text = timeStr;
            expandedDateText.Text = dateStr;

            UpdateMediaTimelineUI();

            if (!IsCompanionWindow && CompanionInstance != null && CompanionInstance.IsVisible)
            {
                CompanionInstance.OnFastTick(sender, e);
            }
        }

        private void UpdateMediaTimelineUI()
        {
            if (string.IsNullOrEmpty(currentTitle) || currentDur <= 0)
            {
                timeCurrentText.Text = "00:00";
                timeRemainingText.Text = "-00:00";
                progressFillBorder.Width = 0;
                return;
            }

            double pos = currentPos;
            if (currentStatus == "Playing")
            {
                pos = Math.Min(currentDur, currentPos + (DateTime.UtcNow - lastPosSync).TotalSeconds);
            }

            double rem = Math.Max(0, currentDur - pos);
            timeCurrentText.Text = FormatMinSec(pos);
            timeRemainingText.Text = "-" + FormatMinSec(rem);

            double trackWidth = progressTrackBorder.ActualWidth;
            if (trackWidth > 0)
            {
                double ratio = Math.Max(0, Math.Min(1.0, pos / currentDur));
                progressFillBorder.Width = trackWidth * ratio;
            }

            if (showLyrics && syncedLyrics.Count > 0)
            {
                int idx = -1;
                for (int i = 0; i < syncedLyrics.Count; i++)
                {
                    if (pos >= syncedLyrics[i].TimeSec) idx = i;
                    else break;
                }
                if (idx != activeLyricIndex && idx >= 0 && idx < lyricsStack.Children.Count)
                {
                    activeLyricIndex = idx;
                    for (int i = 0; i < lyricsStack.Children.Count; i++)
                    {
                        TextBlock tb = lyricsStack.Children[i] as TextBlock;
                        if (tb == null) continue;
                        bool active = (i == activeLyricIndex);
                        tb.Foreground = active ? Brushes.White : new SolidColorBrush(Color.FromRgb(130, 130, 142));
                        tb.FontWeight = active ? FontWeights.Bold : FontWeights.SemiBold;
                        tb.FontSize = active ? 13.5 : 12.5;
                        if (active) tb.BringIntoView();
                    }
                }
            }
        }

        // =======================================================
        // COLLAPSED MUSIC MODE + WAVEFORM ANIMATION
        // =======================================================
        private void UpdateCollapsedMusicMode()
        {
            if (collapsedMusicLayout == null) return;

            bool hasTrack = !string.IsNullOrWhiteSpace(currentTitle);
            bool playingNow = hasTrack && currentStatus == "Playing";
            if (playingNow) lastPlayingSeenUtc = DateTime.UtcNow;

            // Keep the music layout for a few seconds after pausing / track change to avoid flicker
            bool wantMusic = playingNow ||
                             (isMusicMode && hasTrack && (DateTime.UtcNow - lastPlayingSeenUtc).TotalSeconds < 5.0);
            if (wantMusic == isMusicMode) return;

            isMusicMode = wantMusic;
            collapsedIdleLayout.Visibility = isMusicMode ? Visibility.Collapsed : Visibility.Visible;
            collapsedMusicLayout.Visibility = isMusicMode ? Visibility.Visible : Visibility.Collapsed;

            if (waveformTimer != null)
            {
                if (isMusicMode) waveformTimer.Start();
                else waveformTimer.Stop();
            }

            if (!isExpanded)
            {
                notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(CurrentCollapsedWidth, TimeSpan.FromMilliseconds(340))
                {
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.35 }
                });
            }
        }

        private void OnWaveformTick(object sender, EventArgs e)
        {
            if (waveBars == null || isExpanded) return;

            bool playing = currentStatus == "Playing";
            float peak = playing ? HardwareController.GetPeakLevel() : 0f;

            for (int i = 0; i < waveBars.Length; i++)
            {
                double target;
                if (!playing)
                {
                    target = 0.1; // flat line while paused
                }
                else if (peak > 0.003f)
                {
                    // React to the real audio output level, with per-bar jitter for an organic look
                    double level = Math.Min(1.0, Math.Sqrt(peak) * 1.25);
                    double jitter = 0.55 + waveRng.NextDouble() * 0.45;
                    target = 0.15 + 0.85 * level * jitter * waveProfile[i];
                }
                else
                {
                    // Meter unavailable / silent gap: gentle random motion
                    target = 0.18 + waveRng.NextDouble() * 0.5 * waveProfile[i];
                }

                double k = target > waveLevels[i] ? 0.6 : 0.28; // fast attack, slower release
                waveLevels[i] += (target - waveLevels[i]) * k;
                waveBars[i].Height = 3 + waveLevels[i] * 14;
            }
        }

        private void SetAlbumArtVisuals(BitmapSource bmp)
        {
            albumImageBrush.ImageSource = bmp;
            albumArtBox.Background = albumImageBrush;
            albumPlaceholderIcon.Visibility = Visibility.Collapsed;

            collapsedArtBrush.ImageSource = bmp;
            collapsedArtBox.Background = collapsedArtBrush;
            collapsedArtPlaceholder.Visibility = Visibility.Collapsed;

            AnimateWaveColor(ComputeAccentColor(bmp));
        }

        private void ResetAlbumArtVisuals()
        {
            albumArtBox.Background = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255));
            albumPlaceholderIcon.Visibility = Visibility.Visible;

            collapsedArtBox.Background = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255));
            collapsedArtPlaceholder.Visibility = Visibility.Visible;

            AnimateWaveColor(Color.FromRgb(28, 231, 85));
        }

        private void AnimateWaveColor(Color target)
        {
            waveBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(target, TimeSpan.FromMilliseconds(450)));
        }

        /// <summary>
        /// Picks a vivid, bright accent color from the album art (like iPhone's waveform tint).
        /// </summary>
        private static Color ComputeAccentColor(BitmapSource src)
        {
            Color fallback = Color.FromRgb(28, 231, 85);
            try
            {
                double scale = Math.Min(1.0, 48.0 / Math.Max(1, Math.Max(src.PixelWidth, src.PixelHeight)));
                BitmapSource small = new TransformedBitmap(src, new ScaleTransform(scale, scale));
                FormatConvertedBitmap conv = new FormatConvertedBitmap(small, PixelFormats.Bgra32, null, 0);
                int w = conv.PixelWidth, h = conv.PixelHeight, stride = w * 4;
                byte[] px = new byte[stride * h];
                conv.CopyPixels(px, stride, 0);

                double rs = 0, gs = 0, bs = 0, ws = 0;
                for (int i = 0; i + 3 < px.Length; i += 4)
                {
                    double b = px[i] / 255.0, g = px[i + 1] / 255.0, r = px[i + 2] / 255.0;
                    double max = Math.Max(r, Math.Max(g, b));
                    double min = Math.Min(r, Math.Min(g, b));
                    double sat = max <= 0 ? 0 : (max - min) / max;
                    double weight = 0.03 + sat * sat * max; // favour vivid & bright pixels
                    rs += r * weight;
                    gs += g * weight;
                    bs += b * weight;
                    ws += weight;
                }
                if (ws <= 0) return fallback;

                double R = rs / ws, G = gs / ws, B = bs / ws;
                double mx = Math.Max(R, Math.Max(G, B));
                double mn = Math.Min(R, Math.Min(G, B));
                if (mx < 0.02 || (mx - mn) / mx < 0.12)
                {
                    return Color.FromRgb(235, 235, 240); // grayscale cover -> soft white bars
                }

                double boost = 0.96 / mx; // brighten so the bars pop on the dark notch
                return Color.FromRgb(
                    (byte)Math.Min(255, R * boost * 255),
                    (byte)Math.Min(255, G * boost * 255),
                    (byte)Math.Min(255, B * boost * 255));
            }
            catch
            {
                return fallback;
            }
        }

        private string FormatMinSec(double sec)
        {
            if (double.IsNaN(sec) || sec < 0) sec = 0;
            int total = (int)Math.Floor(sec);
            int m = total / 60;
            int s = total % 60;
            return string.Format("{0:D2}:{1:D2}", m, s);
        }

        private void OnProgressTrackClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (currentDur <= 0 || progressTrackBorder.ActualWidth <= 0) return;
            Point pt = e.GetPosition(progressTrackBorder);
            double ratio = Math.Max(0, Math.Min(1.0, pt.X / progressTrackBorder.ActualWidth));
            double targetSec = ratio * currentDur;
            currentPos = targetSec;
            lastPosSync = DateTime.UtcNow;
            UpdateMediaTimelineUI();
            pendingSeekSec = targetSec;
        }

        // =======================================================
        // LYRICS FETCHER
        // =======================================================
        private void ToggleLyricsView()
        {
            showLyrics = !showLyrics;
            lyricsBtnBorder.Background = showLyrics
                ? new SolidColorBrush(Color.FromArgb(45, 28, 231, 85))
                : Brushes.Transparent;
            lyricsBtnIcon.Fill = showLyrics
                ? new SolidColorBrush(Color.FromRgb(28, 231, 85))
                : Brushes.White;

            mediaStandardPanel.Visibility = showLyrics ? Visibility.Collapsed : Visibility.Visible;
            lyricsPanel.Visibility = showLyrics ? Visibility.Visible : Visibility.Collapsed;

            if (showLyrics) FetchLyricsIfNeeded();
        }

        private void FetchLyricsIfNeeded()
        {
            string title = currentTitle;
            string artist = currentArtist;
            if (string.IsNullOrWhiteSpace(title))
            {
                loadedLyricsKey = "";
                syncedLyrics.Clear();
                lyricsHeaderText.Text = "Tidak ada media yang diputar";
                lyricsStack.Children.Clear();
                lyricsStack.Children.Add(new TextBlock
                {
                    Text = "Putar lagu di Spotify / Browser untuk melihat lirik otomatis",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 18, 0, 0)
                });
                return;
            }

            string key = title + "|" + artist;
            if (key == loadedLyricsKey) return;
            loadedLyricsKey = key;
            syncedLyrics.Clear();
            activeLyricIndex = -1;

            lyricsHeaderText.Text = string.IsNullOrEmpty(artist) ? title : (title + " — " + artist);
            lyricsStack.Children.Clear();
            lyricsStack.Children.Add(new TextBlock
            {
                Text = "Mencari lirik lagu...",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 22, 0, 0)
            });

            Task.Run(() =>
            {
                string cleanTitle = Regex.Replace(title, @"\s*(\((feat|ft|with|official|video|audio|lyrics).*?\)|\[.*?\])", "", RegexOptions.IgnoreCase).Trim();
                string cleanArtist = Regex.Replace(artist ?? "", @"\s*[,&].*$", "").Trim();
                string url = "https://lrclib.net/api/search?q=" + Uri.EscapeDataString((cleanTitle + " " + cleanArtist).Trim());

                List<LyricLine> parsedSynced = new List<LyricLine>();
                List<string> parsedPlain = new List<string>();

                try
                {
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.UserAgent = "Glance/1.0";
                    req.Timeout = 6000;
                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    {
                        string body = sr.ReadToEnd();
                        var items = json.Deserialize<List<Dictionary<string, object>>>(body);
                        if (items != null && items.Count > 0)
                        {
                            var best = items.FirstOrDefault(x => x.ContainsKey("syncedLyrics") && x["syncedLyrics"] is string && !string.IsNullOrWhiteSpace((string)x["syncedLyrics"]))
                                       ?? items[0];

                            if (best.ContainsKey("syncedLyrics") && best["syncedLyrics"] is string)
                            {
                                string rawSynced = (string)best["syncedLyrics"];
                                foreach (string line in rawSynced.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    Match m = Regex.Match(line, @"^\[(\d+):(\d+(?:\.\d+)?)\]\s*(.*)$");
                                    if (m.Success)
                                    {
                                        double sec = int.Parse(m.Groups[1].Value) * 60.0 + double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                                        string txt = m.Groups[3].Value.Trim();
                                        if (!string.IsNullOrEmpty(txt))
                                        {
                                            parsedSynced.Add(new LyricLine { TimeSec = sec, Text = txt });
                                        }
                                    }
                                }
                            }
                            else if (best.ContainsKey("plainLyrics") && best["plainLyrics"] is string)
                            {
                                string rawPlain = (string)best["plainLyrics"];
                                parsedPlain.AddRange(rawPlain.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                            }
                        }
                    }
                }
                catch { }

                Dispatcher.Invoke(() =>
                {
                    if (loadedLyricsKey != key) return;
                    lyricsStack.Children.Clear();
                    if (parsedSynced.Count > 0)
                    {
                        syncedLyrics = parsedSynced;
                        foreach (var l in syncedLyrics)
                        {
                            double jumpSec = l.TimeSec;
                            TextBlock tb = new TextBlock
                            {
                                Text = l.Text,
                                Foreground = new SolidColorBrush(Color.FromRgb(130, 130, 142)),
                                FontSize = 12.5,
                                FontWeight = FontWeights.SemiBold,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(4, 3, 4, 3),
                                Cursor = Cursors.Hand
                            };
                            tb.MouseLeftButtonUp += (s, ev) =>
                            {
                                ev.Handled = true;
                                pendingSeekSec = jumpSec;
                            };
                            lyricsStack.Children.Add(tb);
                        }
                        UpdateMediaTimelineUI();
                    }
                    else if (parsedPlain.Count > 0)
                    {
                        foreach (string line in parsedPlain)
                        {
                            lyricsStack.Children.Add(new TextBlock
                            {
                                Text = line,
                                Foreground = Brushes.White,
                                FontSize = 12,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(4, 2, 4, 2)
                            });
                        }
                    }
                    else
                    {
                        lyricsStack.Children.Add(new TextBlock
                        {
                            Text = "Lirik belum tersedia untuk lagu ini",
                            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                            FontSize = 12,
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 22, 0, 0)
                        });
                    }
                });
            });
        }

        // =======================================================
        // BACKGROUND WINRT GSMTC + HARDWARE + MULTI-DRIVE LOOP
        // =======================================================
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
            string cachedWifiSsid = "Memeriksa...";
            int cachedWifiSig = 0;
            List<DriveStatInfo> cachedDrivesList = new List<DriveStatInfo>();

            while (true)
            {
                try
                {
                    if (mgr == null)
                    {
                        mgr = WaitWinRT(GlobalSystemMediaTransportControlsSessionManager.RequestAsync());
                    }

                    var session = mgr != null ? mgr.GetCurrentSession() : null;

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
                            snap.Position = Math.Max(0, tl.Position.TotalSeconds);
                            snap.Duration = Math.Max(0, tl.EndTime.TotalSeconds);
                            if (snap.Status == "Playing" && tl.LastUpdatedTime.Year > 2000)
                            {
                                double diff = (DateTimeOffset.UtcNow - tl.LastUpdatedTime).TotalSeconds;
                                if (diff > 0 && diff < 300)
                                {
                                    snap.Position = Math.Min(snap.Duration, snap.Position + diff);
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

                    var mem = new NativeMethods.MEMORYSTATUSEX();
                    NativeMethods.GlobalMemoryStatusEx(mem);
                    int ramPct = (int)mem.dwMemoryLoad;
                    double totalRamGB = mem.ullTotalPhys / (1024.0 * 1024 * 1024);
                    double usedRamGB = (mem.ullTotalPhys - mem.ullAvailPhys) / (1024.0 * 1024 * 1024);

                    if (loopCounter % 8 == 0 || cachedDrivesList.Count == 0)
                    {
                        List<DriveStatInfo> freshDrives = new List<DriveStatInfo>();
                        try
                        {
                            foreach (var drv in DriveInfo.GetDrives())
                            {
                                if (drv.IsReady && (drv.DriveType == DriveType.Fixed || drv.DriveType == DriveType.Removable))
                                {
                                    double tot = drv.TotalSize / (1024.0 * 1024 * 1024);
                                    double used = (drv.TotalSize - drv.AvailableFreeSpace) / (1024.0 * 1024 * 1024);
                                    int pct = tot > 0 ? (int)Math.Round((used / tot) * 100.0) : 0;
                                    freshDrives.Add(new DriveStatInfo
                                    {
                                        Name = drv.Name,
                                        Percent = pct,
                                        UsedGB = used,
                                        TotalGB = tot
                                    });
                                }
                            }
                            cachedDrivesList = freshDrives;
                        }
                        catch { }
                    }
                    List<DriveStatInfo> drivesList = cachedDrivesList;

                    int volPct;
                    bool isMuted;
                    HardwareController.GetAudioStatus(out volPct, out isMuted);

                    if (loopCounter % 4 == 0)
                    {
                        if (!isDraggingBright)
                        {
                            cachedBrightness = HardwareController.GetScreenBrightness();
                        }
                        HardwareController.GetWifiStatus(out cachedWifiConn, out cachedWifiSsid, out cachedWifiSig);
                    }
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

                Thread.Sleep(350);
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
            currentTitle = snap.Title;
            currentArtist = snap.Artist;
            currentStatus = snap.Status;
            currentPos = snap.Position;
            currentDur = snap.Duration;
            lastPosSync = DateTime.UtcNow;

            if (!string.IsNullOrWhiteSpace(currentTitle))
            {
                mediaTitleText.Text = currentTitle;
                mediaArtistText.Text = currentArtist;
                mediaArtistText.Visibility = string.IsNullOrWhiteSpace(currentArtist) ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                mediaTitleText.Text = "Tidak ada media";
                mediaArtistText.Text = "";
                mediaArtistText.Visibility = Visibility.Collapsed;
            }

            playPauseIconPath.Data = Geometry.Parse(
                currentStatus == "Playing"
                    ? "M 6,19 L 10,19 L 10,5 L 6,5 Z M 14,5 L 14,19 L 18,19 L 18,5 Z"
                    : "M 8,5 L 8,19 L 19,12 Z"
            );

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

            if (showLyrics && (currentTitle + "|" + currentArtist) != oldKey)
            {
                FetchLyricsIfNeeded();
            }

            // Battery Rings
            collapsedBatteryArc.Data = BuildArcGeometry(new Point(12, 12), (24 - 3.3) / 2.0, batPct);
            expandedBatteryArc.Data = BuildArcGeometry(new Point(9, 9), (18 - 2.8) / 2.0, batPct);

            // Slide 1: Top Row
            sysBatVal.Text = batPct + "%";
            sysBatSub.Text = isCharging ? "Mengisi Daya ⚡" : "Mode Baterai";
            sysBatBar.Width = Math.Max(4, 85.0 * batPct / 100.0);

            sysCpuVal.Text = cpuPct + "%";
            sysCpuSub.Text = "Prosesor Aktif";
            sysCpuBar.Width = Math.Max(4, 85.0 * cpuPct / 100.0);

            sysRamVal.Text = ramPct + "%";
            sysRamSub.Text = string.Format(CultureInfo.InvariantCulture, "{0:F1} / {1:F1} GB", usedRamGB, totalRamGB);
            sysRamBar.Width = Math.Max(4, 85.0 * ramPct / 100.0);

            // Slide 1: Bottom Row (Multi-Drive Storage)
            string sig = string.Join(",", drives.Select(d => d.Name));
            if (sig != lastDrivesSignature)
            {
                lastDrivesSignature = sig;
                RebuildDrivesGrid(drives);
            }

            double maxDriveBarWidth = drives.Count > 0 ? Math.Max(60.0, (290.0 / drives.Count) - 24.0) : 130.0;
            for (int i = 0; i < drives.Count && i < driveValTexts.Count; i++)
            {
                var d = drives[i];
                driveValTexts[i].Text = d.Percent + "%";
                driveSubTexts[i].Text = string.Format(CultureInfo.InvariantCulture, "{0:F0} / {1:F0} GB", d.UsedGB, d.TotalGB);
                driveFillBars[i].Width = Math.Max(4, maxDriveBarWidth * d.Percent / 100.0);
            }

            // Slide 2: Control Center
            if (wifiConnected)
            {
                wifiSsidText.Text = wifiSsid;
                wifiSubText.Text = wifiSignal + "%";
                wifiSubText.Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85));
                wifiSignalPill.Background = new SolidColorBrush(Color.FromArgb(45, 28, 231, 85));
            }
            else
            {
                wifiSsidText.Text = "Tidak Terhubung";
                wifiSubText.Text = "Offline";
                wifiSubText.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                wifiSignalPill.Background = new SolidColorBrush(Color.FromArgb(45, 248, 113, 113));
            }

            netDownText.Text = "↓ " + NetworkSpeedMonitor.Format(netDownBps);
            netUpText.Text = "↑ " + NetworkSpeedMonitor.Format(netUpBps);

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

        // =======================================================
        // CLIPBOARD & QUICK NOTES LOGIC
        // =======================================================
        private void LoadSavedData()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    var cfg = json.Deserialize<AppConfig>(File.ReadAllText(configFilePath, Encoding.UTF8));
                    if (cfg != null)
                    {
                        if (cfg.backgroundAlpha >= 60 && cfg.backgroundAlpha <= 255)
                        {
                            BackgroundAlpha = (byte)cfg.backgroundAlpha;
                        }
                        IsPinned = cfg.isPinned;
                        if (!string.IsNullOrEmpty(cfg.displayMode))
                        {
                            DisplayMode = cfg.displayMode;
                        }
                    }
                }
            }
            catch { }

            try
            {
                if (File.Exists(notesFilePath))
                {
                    notesList = json.Deserialize<List<NoteItem>>(File.ReadAllText(notesFilePath, Encoding.UTF8)) ?? new List<NoteItem>();
                }
                else
                {
                    notesList.Add(new NoteItem
                    {
                        id = "n1",
                        text = "Selamat datang di Glance! Tekan Ctrl+Shift+P untuk Glance Search.",
                        done = false
                    });
                    SaveNotesData();
                }
            }
            catch { notesList = new List<NoteItem>(); }

            try
            {
                if (File.Exists(clipFilePath))
                {
                    clipList = json.Deserialize<List<ClipItem>>(File.ReadAllText(clipFilePath, Encoding.UTF8)) ?? new List<ClipItem>();
                }
            }
            catch { clipList = new List<ClipItem>(); }
        }

        private void SaveNotesData()
        {
            try { File.WriteAllText(notesFilePath, json.Serialize(notesList), Encoding.UTF8); } catch { }
            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.notesList = new List<NoteItem>(notesList);
                CompanionInstance.RenderNotesList();
            }
            else if (IsCompanionWindow && PrimaryInstance != null)
            {
                PrimaryInstance.notesList = new List<NoteItem>(notesList);
                PrimaryInstance.RenderNotesList();
            }
        }

        private void SaveClipData()
        {
            try { File.WriteAllText(clipFilePath, json.Serialize(clipList.Take(25).ToList()), Encoding.UTF8); } catch { }
            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.clipList = new List<ClipItem>(clipList);
                CompanionInstance.RenderClipboardList();
            }
            else if (IsCompanionWindow && PrimaryInstance != null)
            {
                PrimaryInstance.clipList = new List<ClipItem>(clipList);
                PrimaryInstance.RenderClipboardList();
            }
        }

        private void OnClipboardTick(object sender, EventArgs e)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    string txt = (Clipboard.GetText() ?? "").Trim();
                    if (!string.IsNullOrEmpty(txt) && txt != lastClipboardText)
                    {
                        lastClipboardText = txt;
                        clipList.RemoveAll(c => c.text == txt);
                        clipList.Insert(0, new ClipItem
                        {
                            id = Guid.NewGuid().ToString("N"),
                            text = txt.Length > 300 ? txt.Substring(0, 300) : txt,
                            time = DateTime.Now.ToString("HH:mm"),
                            pinned = false
                        });
                        clipList = clipList.OrderByDescending(c => c.pinned).Take(25).ToList();
                        SaveClipData();
                        RenderClipboardList();
                    }
                }
            }
            catch { }
        }

        private void RenderClipboardList()
        {
            if (clipListPanel == null) return;
            clipListPanel.Children.Clear();
            if (clipList.Count == 0)
            {
                clipListPanel.Children.Add(new TextBlock
                {
                    Text = "Salin teks (Ctrl+C) untuk menyimpan di sini",
                    Foreground = new SolidColorBrush(Color.FromRgb(145, 145, 155)),
                    FontSize = 11.5,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 26, 0, 0)
                });
                return;
            }

            foreach (var item in clipList)
            {
                Border row = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand,
                    ToolTip = "Klik untuk menyalin kembali"
                };

                DockPanel dp = new DockPanel();
                TextBlock delBtn = new TextBlock
                {
                    Text = "✕",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                    FontSize = 11,
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                delBtn.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    clipList.RemoveAll(x => x.id == item.id);
                    SaveClipData();
                    RenderClipboardList();
                };
                DockPanel.SetDock(delBtn, Dock.Right);

                TextBlock txt = new TextBlock
                {
                    Text = item.text.Replace("\r", " ").Replace("\n", " "),
                    Foreground = Brushes.White,
                    FontSize = 11.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                dp.Children.Add(delBtn);
                dp.Children.Add(txt);
                row.Child = dp;

                row.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    try
                    {
                        lastClipboardText = item.text;
                        Clipboard.SetText(item.text);
                        txt.Text = "✓ Tersalin ke Clipboard!";
                        txt.Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85));
                    }
                    catch { }
                };

                clipListPanel.Children.Add(row);
            }
        }

        private void AddNoteFromInput()
        {
            string val = (noteInputBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(val)) return;
            noteInputBox.Text = "";
            notesList.Insert(0, new NoteItem
            {
                id = Guid.NewGuid().ToString("N"),
                text = val,
                done = false
            });
            SaveNotesData();
            RenderNotesList();
        }

        private void RenderNotesList()
        {
            if (notesListPanel == null) return;
            notesListPanel.Children.Clear();
            if (notesList.Count == 0)
            {
                notesListPanel.Children.Add(new TextBlock
                {
                    Text = "Belum ada catatan cepat",
                    Foreground = new SolidColorBrush(Color.FromRgb(145, 145, 155)),
                    FontSize = 11.5,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 22, 0, 0)
                });
                return;
            }

            foreach (var note in notesList)
            {
                Border row = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand
                };

                DockPanel dp = new DockPanel();
                TextBlock checkTxt = new TextBlock
                {
                    Text = note.done ? "☑" : "☐",
                    Foreground = note.done
                        ? new SolidColorBrush(Color.FromRgb(28, 231, 85))
                        : new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 7, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(checkTxt, Dock.Left);

                TextBlock delTxt = new TextBlock
                {
                    Text = "✕",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                    FontSize = 11,
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                delTxt.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    notesList.RemoveAll(n => n.id == note.id);
                    SaveNotesData();
                    RenderNotesList();
                };
                DockPanel.SetDock(delTxt, Dock.Right);

                TextBlock bodyTxt = new TextBlock
                {
                    Text = note.text,
                    Foreground = note.done
                        ? new SolidColorBrush(Color.FromRgb(140, 140, 150))
                        : Brushes.White,
                    TextDecorations = note.done ? TextDecorations.Strikethrough : null,
                    FontSize = 11.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                dp.Children.Add(checkTxt);
                dp.Children.Add(delTxt);
                dp.Children.Add(bodyTxt);
                row.Child = dp;

                row.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    note.done = !note.done;
                    SaveNotesData();
                    RenderNotesList();
                };

                notesListPanel.Children.Add(row);
            }
        }

        // =======================================================
        // SYSTEM TRAY & SETTINGS WINDOW
        // =======================================================
        private void SetupTrayIcon()
        {
            trayIcon = new Forms.NotifyIcon();
            try
            {
                trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            }
            catch
            {
                trayIcon.Icon = System.Drawing.SystemIcons.Application;
            }
            trayIcon.Text = "Glance";
            trayIcon.Visible = true;

            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            menu.Items.Add("Glance Search (Ctrl+Shift+P)", null, (s, e) => Dispatcher.Invoke(ToggleSpotlightSearch));
            menu.Items.Add("Pengaturan Glance...", null, (s, e) => Dispatcher.Invoke(OpenSettingsDialog));

            int screenCount = Forms.Screen.AllScreens.Length;
            Forms.ToolStripMenuItem screenSubMenu = new Forms.ToolStripMenuItem("Pilih Layar / Monitor");

            trayItemScr1 = new Forms.ToolStripMenuItem("Layar 1 Saja (Utama)", null, (s, e) => ApplyDisplayMode("screen1"));
            trayItemScr2 = new Forms.ToolStripMenuItem(screenCount > 1 ? "Layar 2 Saja (Sekunder)" : "Layar 2 Saja (Tidak terdeteksi)", null, (s, e) => { if (screenCount > 1) ApplyDisplayMode("screen2"); });
            trayItemBoth = new Forms.ToolStripMenuItem(screenCount > 1 ? "Kedua Layar" : "Kedua Layar (Perlu 2 monitor)", null, (s, e) => { if (screenCount > 1) ApplyDisplayMode("both"); });

            trayItemScr2.Enabled = screenCount > 1;
            trayItemBoth.Enabled = screenCount > 1;

            screenSubMenu.DropDownItems.Add(trayItemScr1);
            screenSubMenu.DropDownItems.Add(trayItemScr2);
            screenSubMenu.DropDownItems.Add(trayItemBoth);
            menu.Items.Add(screenSubMenu);

            menu.Items.Add("Kunci / Lepas Posisi Terbuka (Pin)", null, (s, e) => Dispatcher.Invoke(() => SetPinned(!IsPinned)));
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Keluar dari Glance", null, (s, e) => Dispatcher.Invoke(() => Application.Current.Shutdown()));

            trayIcon.ContextMenuStrip = menu;
            trayIcon.DoubleClick += (s, e) => Dispatcher.Invoke(OpenSettingsDialog);

            UpdateTrayDisplayModeMenu();
        }

        private void UpdateTrayDisplayModeMenu()
        {
            if (trayItemScr1 == null || trayItemScr2 == null || trayItemBoth == null) return;
            trayItemScr1.Checked = (DisplayMode == "screen1");
            trayItemScr2.Checked = (DisplayMode == "screen2");
            trayItemBoth.Checked = (DisplayMode == "both");
        }

        public void OpenSettingsDialog()
        {
            SettingsWindow sw = new SettingsWindow(PrimaryInstance ?? this);
            sw.ShowDialog();
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    File.WriteAllText("d:\\Coding-an\\windows-notch\\error.log", e.ExceptionObject.ToString());
                };

                int currentPid = Process.GetCurrentProcess().Id;
                var existingProcs = Process.GetProcessesByName("Glance")
                    .Concat(Process.GetProcessesByName("DynamicIsland"));
                foreach (var p in existingProcs)
                {
                    if (p.Id != currentPid)
                    {
                        try { p.Kill(); } catch { }
                    }
                }

                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.DispatcherUnhandledException += (s, e) =>
                {
                    File.WriteAllText("d:\\Coding-an\\windows-notch\\error.log", e.Exception.ToString());
                };

                MainWindow mainWin = new MainWindow();
                app.MainWindow = mainWin;
                mainWin.Show();

                if (args != null && args.Any(a => a.Equals("--settings", StringComparison.OrdinalIgnoreCase)))
                {
                    mainWin.Dispatcher.BeginInvoke(new Action(() => mainWin.OpenSettingsDialog()));
                }

                app.Run();
            }
            catch (Exception ex)
            {
                File.WriteAllText("d:\\Coding-an\\windows-notch\\error.log", ex.ToString());
            }
        }
    }
}
