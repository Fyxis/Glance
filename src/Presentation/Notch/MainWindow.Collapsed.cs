using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Glance
{
    /// <summary>
    /// Bagian partial MainWindow untuk tampilan Notch saat kecil (Collapsed):
    /// Mode Idle (Baterai, Jam, Kecepatan Wi-Fi) & Mode Musik (Album Art, Lirik Berjalan, Jam, Waveform).
    /// </summary>
    public partial class MainWindow
    {
        private Grid BuildCollapsedView()
        {
            Grid root = new Grid();

            // ---- Idle layout: [Kiri: Baterai]  [Tengah: Jam]  [Kanan: Speed Wi-Fi (↓ / ↑)] ----
            collapsedIdleLayout = new Grid
            {
                Margin = new Thickness(12, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Stretch
            };
            collapsedIdleLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            collapsedIdleLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            collapsedIdleLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 1. Kiri: Battery Ring Widget
            Grid batRing = CreateBatteryRingWidget(CollapsedBatterySize, CollapsedBatteryStroke, out collapsedBatteryArc, out collapsedBatteryBgRing);
            batRing.HorizontalAlignment = HorizontalAlignment.Left;
            batRing.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(batRing, 0);

            // 2. Tengah: Jam
            collapsedTimeText = new TextBlock
            {
                Text = "16:06",
                Foreground = Brushes.White,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(collapsedTimeText, 1);

            // 3. Kanan: Speed wifi (download and upload)
            StackPanel netSpeedPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            collapsedNetDownText = new TextBlock
            {
                Text = "↓ 0 KB/s",
                Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                FontSize = 8.8,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Right
            };
            collapsedNetUpText = new TextBlock
            {
                Text = "↑ 0 KB/s",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 178)),
                FontSize = 8.8,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Right
            };
            netSpeedPanel.Children.Add(collapsedNetDownText);
            netSpeedPanel.Children.Add(collapsedNetUpText);
            Grid.SetColumn(netSpeedPanel, 2);

            collapsedIdleLayout.Children.Add(batRing);
            collapsedIdleLayout.Children.Add(collapsedTimeText);
            collapsedIdleLayout.Children.Add(netSpeedPanel);

            // ---- Music layout: [Logo Musik]  [Lirik / Jam]  [Waveform] ----
            collapsedMusicLayout = new Grid
            {
                Margin = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Collapsed
            };
            collapsedMusicLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            collapsedMusicLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            collapsedMusicLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 1. Kiri: Logo Musik / Album Art Box (menggantikan baterai saat musik aktif)
            collapsedArtBox = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
                ClipToBounds = true
            };
            collapsedArtBrush = new ImageBrush { Stretch = Stretch.UniformToFill };
            collapsedArtPlaceholder = new Path
            {
                Data = Geometry.Parse("M 12,3 L 12,13.55 C 11.41,13.21 10.73,13 10,13 C 7.79,13 6,14.79 6,17 C 6,19.21 7.79,21 10,21 C 12.21,21 14,19.21 14,17 L 14,7 L 18,7 L 18,3 L 12,3 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(200, 200, 210)),
                Width = 11,
                Height = 11,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            collapsedArtBox.Child = collapsedArtPlaceholder;
            Grid.SetColumn(collapsedArtBox, 0);

            // 2. Tengah: Grid Penampung Lirik & Jam
            collapsedMusicMidGrid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            collapsedMusicMidGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            collapsedMusicMidGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 2a. Lirik Berjalan (sebelah logo musik jika ada lirik)
            collapsedMusicLyricText = new TextBlock
            {
                Text = "",
                Foreground = Brushes.White,
                FontSize = 11.5,
                FontWeight = FontWeights.Medium,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 6, 0),
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(collapsedMusicLyricText, 0);

            // 2b. Jam (di sebelah lirik jika ada lirik, atau di tengah jika lirik tidak ada)
            collapsedMusicTimeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture),
                Foreground = Brushes.White,
                FontSize = 13.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(collapsedMusicTimeText, 0);
            Grid.SetColumnSpan(collapsedMusicTimeText, 2);

            collapsedMusicMidGrid.Children.Add(collapsedMusicLyricText);
            collapsedMusicMidGrid.Children.Add(collapsedMusicTimeText);
            Grid.SetColumn(collapsedMusicMidGrid, 1);

            // 3. Kanan: Wave Equalizer Audio
            StackPanel wavePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 18,
                Margin = new Thickness(6, 0, 0, 0),
                ToolTip = "Media playing"
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
            Grid.SetColumn(wavePanel, 2);

            collapsedMusicLayout.Children.Add(collapsedArtBox);
            collapsedMusicLayout.Children.Add(collapsedMusicMidGrid);
            collapsedMusicLayout.Children.Add(wavePanel);

            // ---- Sleep / Always-On Display (AOD) layout: [Tengah: Hanya Jam Minimalis] ----
            collapsedSleepLayout = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Visibility = Visibility.Collapsed,
                Opacity = 0
            };
            collapsedSleepTimeText = new TextBlock
            {
                Text = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture),
                Foreground = new SolidColorBrush(Color.FromRgb(225, 225, 235)),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            collapsedSleepLayout.Children.Add(collapsedSleepTimeText);

            // ---- Live Activity Toast layout: [Icon + Label]  [Progress Bar (opsional)]  [Value] ----
            collapsedToastLayout = new Grid
            {
                Margin = new Thickness(11, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Stretch,
                Visibility = Visibility.Collapsed,
                Opacity = 0
            };
            collapsedToastLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            collapsedToastLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            collapsedToastLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            collapsedToastLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            toastIconBadge = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(Color.FromArgb(45, 28, 231, 85)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            toastIconText = new TextBlock
            {
                Text = "🔊",
                FontSize = 11.5,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            toastIconBadge.Child = toastIconText;
            Grid.SetColumn(toastIconBadge, 0);

            toastTitleText = new TextBlock
            {
                Text = "Volume",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(toastTitleText, 1);

            toastBarTrack = new Border
            {
                Width = 64,
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                Background = new SolidColorBrush(Color.FromArgb(48, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                ClipToBounds = true
            };
            toastBarFill = new Border
            {
                Width = 32,
                Height = 5,
                CornerRadius = new CornerRadius(2.5),
                Background = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            toastBarTrack.Child = toastBarFill;
            Grid.SetColumn(toastBarTrack, 2);

            toastValueText = new TextBlock
            {
                Text = "100%",
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetColumn(toastValueText, 3);

            collapsedToastLayout.Children.Add(toastIconBadge);
            collapsedToastLayout.Children.Add(toastTitleText);
            collapsedToastLayout.Children.Add(toastBarTrack);
            collapsedToastLayout.Children.Add(toastValueText);

            root.Children.Add(collapsedIdleLayout);
            root.Children.Add(collapsedMusicLayout);
            root.Children.Add(collapsedSleepLayout);
            root.Children.Add(collapsedToastLayout);
            return root;
        }

        private Grid CreateBatteryRingWidget(double size, double strokeThickness, out Path fgArc, out Ellipse bgRingOut)
        {
            Grid wrap = new Grid { Width = size, Height = size, ClipToBounds = false };
            double radius = (size - strokeThickness) / 2.0;
            Point center = new Point(size / 2.0, size / 2.0);

            Color colBattery = ColorFromHex(ColorBattery, Color.FromRgb(28, 231, 85));

            Ellipse bgRing = new Ellipse
            {
                Width = size,
                Height = size,
                Stroke = new SolidColorBrush(Color.FromArgb(60, colBattery.R, colBattery.G, colBattery.B)),
                StrokeThickness = strokeThickness
            };
            bgRingOut = bgRing;

            fgArc = new Path
            {
                Stroke = new SolidColorBrush(colBattery),
                StrokeThickness = strokeThickness,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Data = BuildArcGeometry(center, radius, 100)
            };

            Path bolt = new Path
            {
                Data = Geometry.Parse("M 13,2 L 4.5,13.5 L 11,13.5 L 10,22 L 19.5,10.5 L 13,10.5 Z"),
                Fill = Brushes.White,
                Width = size * 0.44,
                Height = size * 0.44,
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
            if (percent <= 1.0)
            {
                return Geometry.Empty;
            }

            if (percent >= 99.5)
            {
                return new EllipseGeometry(center, radius, radius);
            }

            double pct = Math.Max(2.0, Math.Min(99.0, percent));
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

        private string GetCurrentLyricText()
        {
            if (syncedLyrics == null || syncedLyrics.Count == 0) return "";
            double pos = currentPos;
            if (currentStatus == "Playing")
            {
                pos = Math.Min(currentDur, currentPos + (DateTime.UtcNow - lastPosSync).TotalSeconds);
            }
            double lyricPos = pos + 0.25;
            string lyric = "";
            double lineTime = -1;
            for (int i = 0; i < syncedLyrics.Count; i++)
            {
                if (lyricPos >= syncedLyrics[i].TimeSec)
                {
                    lyric = syncedLyrics[i].Text;
                    lineTime = syncedLyrics[i].TimeSec;
                }
                else break;
            }
            if (lineTime >= 0 && (pos - lineTime) > 12.0)
            {
                return "";
            }
            return lyric ?? "";
        }

        private void UpdateCollapsedMusicUI()
        {
            if (collapsedMusicLayout == null || !isMusicMode) return;

            string lyric = GetCurrentLyricText();
            bool hasLyric = !string.IsNullOrWhiteSpace(lyric);

            if (hasLyric)
            {
                if (collapsedMusicLyricText.Text != lyric)
                {
                    collapsedMusicLyricText.Text = lyric;
                }
                if (collapsedMusicLyricText.Visibility != Visibility.Visible)
                {
                    collapsedMusicLyricText.Visibility = Visibility.Visible;
                }

                // Posisi Jam di sebelah lirik (sebelah kanan lirik, sebelum waveform)
                Grid.SetColumn(collapsedMusicTimeText, 1);
                Grid.SetColumnSpan(collapsedMusicTimeText, 1);
                collapsedMusicTimeText.HorizontalAlignment = HorizontalAlignment.Right;
                collapsedMusicTimeText.FontSize = 12.0;
                collapsedMusicTimeText.Foreground = new SolidColorBrush(Color.FromRgb(185, 185, 195));
            }
            else
            {
                if (collapsedMusicLyricText.Visibility != Visibility.Collapsed)
                {
                    collapsedMusicLyricText.Visibility = Visibility.Collapsed;
                }

                // Posisi Jam di tengah (sebelah logo musik, sebelum waveform)
                Grid.SetColumn(collapsedMusicTimeText, 0);
                Grid.SetColumnSpan(collapsedMusicTimeText, 2);
                collapsedMusicTimeText.HorizontalAlignment = HorizontalAlignment.Center;
                collapsedMusicTimeText.FontSize = 13.5;
                collapsedMusicTimeText.Foreground = Brushes.White;
            }

            double targetWidth = hasLyric ? MusicLyricCollapsedWidth : MusicCollapsedWidth;
            if (!isExpanded && !isSleeping && !isToastActive && Math.Abs(notchBody.Width - targetWidth) > 1.5)
            {
                notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
                });
            }
        }

        private void UpdateCollapsedMusicMode()
        {
            if (collapsedMusicLayout == null) return;

            bool hasTrack = !string.IsNullOrWhiteSpace(currentTitle);
            bool audioPeakActive = HardwareController.GetPeakLevel() > 0.003f;
            bool playingNow = hasTrack && (currentStatus == "Playing" || (audioPeakActive && currentStatus != "Stopped" && currentStatus != "Closed"));
            if (playingNow) lastPlayingSeenUtc = DateTime.UtcNow;

            bool wantMusic = playingNow ||
                             (isMusicMode && hasTrack && (DateTime.UtcNow - lastPlayingSeenUtc).TotalSeconds < 6.0);

            if (wantMusic != isMusicMode)
            {
                isMusicMode = wantMusic;
                if (!isSleeping && !isToastActive)
                {
                    collapsedIdleLayout.Visibility = isMusicMode ? Visibility.Collapsed : Visibility.Visible;
                    collapsedMusicLayout.Visibility = isMusicMode ? Visibility.Visible : Visibility.Collapsed;
                }

                if (waveformTimer != null)
                {
                    if (isMusicMode) waveformTimer.Start();
                    else waveformTimer.Stop();
                }

                if (clockTimer != null)
                {
                    clockTimer.Interval = (isExpanded || isMusicMode) ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromMilliseconds(1000);
                }

                if (!isExpanded && !isSleeping && !isToastActive)
                {
                    notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(CurrentCollapsedWidth, TimeSpan.FromMilliseconds(340))
                    {
                        EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 }
                    });
                }
            }

            if (isMusicMode)
            {
                UpdateCollapsedMusicUI();
            }
        }

        private void OnMouseIdleTick(object sender, EventArgs e)
        {
            if (IsCompanionWindow) return;

            NativeMethods.POINT pt;
            if (!NativeMethods.GetCursorPos(out pt)) return;

            bool moved = Math.Abs(pt.X - lastMousePt.X) >= 2 || Math.Abs(pt.Y - lastMousePt.Y) >= 2;
            if (moved)
            {
                lastMousePt = pt;
                lastMouseMoveUtc = DateTime.UtcNow;

                if (isSleeping)
                {
                    WakeFromSleepMode(true);
                }
                if (CompanionInstance != null && CompanionInstance.isSleeping)
                {
                    CompanionInstance.WakeFromSleepMode(true);
                }
                return;
            }

            bool anyExpanded = isExpanded || (CompanionInstance != null && CompanionInstance.isExpanded);
            bool searchVisible = spotlightWin != null && spotlightWin.IsVisible;

            if (!EnableSleepMode || IsPinned || anyExpanded || searchVisible)
            {
                lastMouseMoveUtc = DateTime.UtcNow;
                if (isSleeping) WakeFromSleepMode(true);
                if (CompanionInstance != null && CompanionInstance.isSleeping) CompanionInstance.WakeFromSleepMode(true);
                return;
            }

            double idleSec = (DateTime.UtcNow - lastMouseMoveUtc).TotalSeconds;
            if (idleSec >= SleepTimeoutSeconds)
            {
                if (!isSleeping)
                {
                    EnterSleepMode();
                }
                if (CompanionInstance != null && CompanionInstance.IsVisible && !CompanionInstance.isSleeping)
                {
                    CompanionInstance.EnterSleepMode();
                }
            }
        }

        public void EnterSleepMode()
        {
            if (isSleeping || isExpanded || IsPinned || isToastActive || notchBody == null) return;
            isSleeping = true;

            if (collapsedSleepTimeText != null)
            {
                collapsedSleepTimeText.Text = DateTime.Now.ToString("HH:mm", CultureInfo.InvariantCulture);
            }

            collapsedIdleLayout.Visibility = Visibility.Collapsed;
            collapsedMusicLayout.Visibility = Visibility.Collapsed;

            if (collapsedSleepLayout != null)
            {
                collapsedSleepLayout.Visibility = Visibility.Visible;
                collapsedSleepLayout.BeginAnimation(OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(320)));
            }

            notchBody.CornerRadius = new CornerRadius(14);
            var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
            Duration dur = new Duration(TimeSpan.FromMilliseconds(360));

            notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(SleepCollapsedWidth, dur) { EasingFunction = ease });
            notchBody.BeginAnimation(HeightProperty, new DoubleAnimation(SleepCollapsedHeight, dur) { EasingFunction = ease });
            notchBody.BeginAnimation(OpacityProperty, new DoubleAnimation(0.42, dur) { EasingFunction = ease });
        }

        public void WakeFromSleepMode(bool animate = true)
        {
            if (!isSleeping || notchBody == null) return;
            isSleeping = false;
            lastMouseMoveUtc = DateTime.UtcNow;

            if (collapsedSleepLayout != null)
            {
                collapsedSleepLayout.BeginAnimation(OpacityProperty, null);
                collapsedSleepLayout.Opacity = 0;
                collapsedSleepLayout.Visibility = Visibility.Collapsed;
            }

            collapsedIdleLayout.Visibility = isMusicMode ? Visibility.Collapsed : Visibility.Visible;
            collapsedMusicLayout.Visibility = isMusicMode ? Visibility.Visible : Visibility.Collapsed;

            if (!isExpanded)
            {
                notchBody.CornerRadius = new CornerRadius(18);
                if (animate)
                {
                    var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.22 };
                    Duration dur = new Duration(TimeSpan.FromMilliseconds(260));
                    notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(CurrentCollapsedWidth, dur) { EasingFunction = ease });
                    notchBody.BeginAnimation(HeightProperty, new DoubleAnimation(CollapsedHeight, dur) { EasingFunction = ease });
                    notchBody.BeginAnimation(OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(180)));
                }
                else
                {
                    notchBody.BeginAnimation(WidthProperty, null);
                    notchBody.BeginAnimation(HeightProperty, null);
                    notchBody.BeginAnimation(OpacityProperty, null);
                    notchBody.Width = CurrentCollapsedWidth;
                    notchBody.Height = CollapsedHeight;
                    notchBody.Opacity = 1.0;
                }
            }
            else
            {
                notchBody.BeginAnimation(OpacityProperty, null);
                notchBody.Opacity = 1.0;
            }
        }

        public void ApplySleepConfig(bool enabled, int timeoutSeconds)
        {
            EnableSleepMode = enabled;
            SleepTimeoutSeconds = Math.Clamp(timeoutSeconds, 2, 120);
            lastMouseMoveUtc = DateTime.UtcNow;

            if (!EnableSleepMode && isSleeping)
            {
                WakeFromSleepMode(true);
            }

            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.ApplySleepConfig(EnableSleepMode, SleepTimeoutSeconds);
            }
        }

        /// <summary>
        /// Menampilkan pop-up Live Activity Toast pada Notch (seperti Volume, Brightness, Charger, atau Clipboard).
        /// </summary>
        public void ShowLiveActivityToast(
            string icon,
            string title,
            string valueText,
            int progressPct,
            bool showProgressBar,
            Color accentColor,
            int durationMs = 1850)
        {
            if (notchBody == null || collapsedToastLayout == null) return;

            if (!IsCompanionWindow && CompanionInstance != null && CompanionInstance.IsVisible)
            {
                CompanionInstance.ShowLiveActivityToast(icon, title, valueText, progressPct, showProgressBar, accentColor, durationMs);
            }

            if (isExpanded) return;

            if (isSleeping)
            {
                WakeFromSleepMode(false);
            }
            lastMouseMoveUtc = DateTime.UtcNow;

            toastIconText.Text = icon ?? "🔔";
            toastIconBadge.Background = new SolidColorBrush(Color.FromArgb(48, accentColor.R, accentColor.G, accentColor.B));
            toastTitleText.Text = title ?? "";
            toastValueText.Text = valueText ?? "";
            toastValueText.Foreground = new SolidColorBrush(accentColor);

            if (showProgressBar)
            {
                toastBarTrack.Visibility = Visibility.Visible;
                toastBarFill.Background = new SolidColorBrush(accentColor);
                double targetFillW = Math.Max(4.0, 64.0 * Math.Clamp(progressPct, 0, 100) / 100.0);
                toastBarFill.BeginAnimation(WidthProperty, new DoubleAnimation(targetFillW, TimeSpan.FromMilliseconds(160))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
            }
            else
            {
                toastBarTrack.Visibility = Visibility.Collapsed;
            }

            if (!isToastActive)
            {
                isToastActive = true;
                collapsedIdleLayout.Visibility = Visibility.Collapsed;
                collapsedMusicLayout.Visibility = Visibility.Collapsed;
                if (collapsedSleepLayout != null) collapsedSleepLayout.Visibility = Visibility.Collapsed;

                collapsedToastLayout.Visibility = Visibility.Visible;
                collapsedToastLayout.BeginAnimation(OpacityProperty, new DoubleAnimation(0.0, 1.0, TimeSpan.FromMilliseconds(180)));

                notchBody.CornerRadius = new CornerRadius(18);
                var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.25 };
                Duration dur = new Duration(TimeSpan.FromMilliseconds(260));
                notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(ToastCollapsedWidth, dur) { EasingFunction = ease });
                notchBody.BeginAnimation(HeightProperty, new DoubleAnimation(CollapsedHeight, dur) { EasingFunction = ease });
                notchBody.BeginAnimation(OpacityProperty, new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150)));
            }

            if (toastDismissTimer == null)
            {
                toastDismissTimer = new DispatcherTimer();
                toastDismissTimer.Tick += (s, e) =>
                {
                    toastDismissTimer.Stop();
                    DismissLiveActivityToast(true);
                };
            }
            toastDismissTimer.Stop();
            toastDismissTimer.Interval = TimeSpan.FromMilliseconds(Math.Max(800, durationMs));
            toastDismissTimer.Start();
        }

        public void DismissLiveActivityToast(bool animate = true)
        {
            if (!isToastActive || notchBody == null) return;
            isToastActive = false;
            lastMouseMoveUtc = DateTime.UtcNow;

            if (toastDismissTimer != null)
            {
                toastDismissTimer.Stop();
            }

            if (collapsedToastLayout != null)
            {
                collapsedToastLayout.BeginAnimation(OpacityProperty, null);
                collapsedToastLayout.Opacity = 0;
                collapsedToastLayout.Visibility = Visibility.Collapsed;
            }

            if (!isExpanded && !isSleeping)
            {
                collapsedIdleLayout.Visibility = isMusicMode ? Visibility.Collapsed : Visibility.Visible;
                collapsedMusicLayout.Visibility = isMusicMode ? Visibility.Visible : Visibility.Collapsed;

                if (animate)
                {
                    var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.22 };
                    Duration dur = new Duration(TimeSpan.FromMilliseconds(280));
                    notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(CurrentCollapsedWidth, dur) { EasingFunction = ease });
                    notchBody.BeginAnimation(HeightProperty, new DoubleAnimation(CollapsedHeight, dur) { EasingFunction = ease });
                }
                else
                {
                    notchBody.BeginAnimation(WidthProperty, null);
                    notchBody.Width = CurrentCollapsedWidth;
                }
            }
        }

        private void OnWaveformTick(object sender, EventArgs e)
        {
            if (waveBars == null || isExpanded) return;

            waveformTickCount++;
            if (waveformTickCount % 5 == 0)
            {
                UpdateCollapsedMusicUI();
            }

            float peak = HardwareController.GetPeakLevel();
            bool playing = (currentStatus == "Playing") || (peak > 0.003f);

            for (int i = 0; i < waveBars.Length; i++)
            {
                double target;
                if (!playing)
                {
                    target = 0.1;
                }
                else if (peak > 0.003f)
                {
                    double level = Math.Min(1.0, Math.Sqrt(peak) * 1.25);
                    double jitter = 0.55 + waveRng.NextDouble() * 0.45;
                    target = 0.15 + 0.85 * level * jitter * waveProfile[i];
                }
                else
                {
                    target = 0.18 + waveRng.NextDouble() * 0.5 * waveProfile[i];
                }

                double k = target > waveLevels[i] ? 0.6 : 0.28;
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

            AnimateWaveColor(ColorFromHex(ColorAccent, Color.FromRgb(28, 231, 85)));
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
                    double weight = 0.03 + sat * sat * max;
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
                    return Color.FromRgb(235, 235, 240);
                }

                double boost = 0.96 / mx;
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
    }
}
