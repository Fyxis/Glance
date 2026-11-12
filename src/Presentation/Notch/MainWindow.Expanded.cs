using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Path = System.Windows.Shapes.Path;

namespace Glance
{
    /// <summary>
    /// Bagian partial MainWindow untuk tampilan Notch saat terbuka (Expanded):
    /// Top Header, Navigasi Carousel 6-Slide, Animasi Expand/Collapse, Tema Warna, dan Fast Tick UI.
    /// </summary>
    public partial class MainWindow
    {
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

            // Top-Left Search Button (Quick trigger for Spotlight Search)
            searchQuickBtn = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ToolTip = "Glance Search (" + HotkeyDisplay + ")",
                Visibility = (IsSearchInstalled && IsSearchEnabled) ? Visibility.Visible : Visibility.Collapsed
            };
            Path searchQuickIcon = new Path
            {
                Data = Geometry.Parse("M 5.8 1.5 A 4.3 4.3 0 1 1 1.5 5.8 A 4.3 4.3 0 0 1 5.8 1.5 Z M 9 9 L 12.5 12.5"),
                Stroke = new SolidColorBrush(Color.FromRgb(175, 175, 185)),
                StrokeThickness = 1.6,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Width = 11.5,
                Height = 11.5,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            searchQuickBtn.Child = searchQuickIcon;
            searchQuickBtn.MouseEnter += (s, e) =>
            {
                searchQuickBtn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                searchQuickIcon.Stroke = Brushes.White;
            };
            searchQuickBtn.MouseLeave += (s, e) =>
            {
                searchQuickBtn.Background = Brushes.Transparent;
                searchQuickIcon.Stroke = new SolidColorBrush(Color.FromRgb(175, 175, 185));
            };
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

            Grid smallBatRing = CreateBatteryRingWidget(ExpandedBatterySize, ExpandedBatteryStroke, out expandedBatteryArc, out expandedBatteryBgRing);
            smallBatRing.Margin = new Thickness(0, 0, 8, 0);

            expandedDateText = new TextBlock
            {
                Text = "Fri 23 Oct 2026",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 178)),
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 7, 0)
            };

            expandedTimeText = new TextBlock
            {
                Text = "11:54",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
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
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = "Glance Settings",
                Margin = new Thickness(0, 0, 3, 0)
            };
            Path gearIcon = new Path
            {
                Data = Geometry.Parse("M 6 1 L 8 1 L 8.3 2.5 A 5 5 0 0 1 9.5 3.2 L 10.9 2.5 L 12.3 3.9 L 11.6 5.3 A 5 5 0 0 1 12.3 6.5 L 13.8 6.8 L 13.8 8.8 L 12.3 9.1 A 5 5 0 0 1 11.6 10.3 L 12.3 11.7 L 10.9 13.1 L 9.5 12.4 A 5 5 0 0 1 8.3 13.1 L 8 14.6 L 6 14.6 L 5.7 13.1 A 5 5 0 0 1 4.5 12.4 L 3.1 13.1 L 1.7 11.7 L 2.4 10.3 A 5 5 0 0 1 1.7 9.1 L 0.2 8.8 L 0.2 6.8 L 1.7 6.5 A 5 5 0 0 1 2.4 5.3 L 1.7 3.9 L 3.1 2.5 L 4.5 3.2 A 5 5 0 0 1 5.7 2.5 Z M 7 5 A 2.5 2.5 0 1 0 7 10 A 2.5 2.5 0 0 0 7 5 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(175, 175, 185)),
                Width = 12.5,
                Height = 12.5,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            settingsBtn.Child = gearIcon;
            settingsBtn.MouseEnter += (s, e) =>
            {
                settingsBtn.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                gearIcon.Fill = Brushes.White;
            };
            settingsBtn.MouseLeave += (s, e) =>
            {
                settingsBtn.Background = Brushes.Transparent;
                gearIcon.Fill = new SolidColorBrush(Color.FromRgb(175, 175, 185));
            };
            settingsBtn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                OpenSettingsDialog();
            };

            pinBtnBorder = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                ToolTip = "Pin expanded HUD"
            };
            pinBtnIcon = new Path
            {
                Data = Geometry.Parse("M 3.5 1.5 L 10.5 1.5 L 9.5 5.5 L 11.5 7.5 L 7.5 7.5 L 6.5 13 L 5.5 7.5 L 1.5 7.5 L 3.5 5.5 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(175, 175, 185)),
                Width = 11,
                Height = 11,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            pinBtnBorder.Child = pinBtnIcon;
            pinBtnBorder.MouseEnter += (s, e) =>
            {
                pinBtnBorder.Background = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                if (!IsPinned) pinBtnIcon.Fill = Brushes.White;
            };
            pinBtnBorder.MouseLeave += (s, e) =>
            {
                pinBtnBorder.Background = IsPinned ? new SolidColorBrush(Color.FromArgb(50, 28, 231, 85)) : Brushes.Transparent;
                if (!IsPinned) pinBtnIcon.Fill = new SolidColorBrush(Color.FromRgb(175, 175, 185));
            };
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
            prevSlideBtn = CreateNavCircleButton(true);
            prevSlideBtn.HorizontalAlignment = HorizontalAlignment.Left;
            prevSlideBtn.VerticalAlignment = VerticalAlignment.Center;
            prevSlideBtn.Margin = new Thickness(12, 0, 0, 0);
            prevSlideBtn.MouseLeftButtonUp += (s, e) => { e.Handled = true; SwitchSlide(currentSlide - 1); };

            nextSlideBtn = CreateNavCircleButton(false);
            nextSlideBtn.HorizontalAlignment = HorizontalAlignment.Right;
            nextSlideBtn.VerticalAlignment = VerticalAlignment.Center;
            nextSlideBtn.Margin = new Thickness(0, 0, 12, 0);
            nextSlideBtn.MouseLeftButtonUp += (s, e) => { e.Handled = true; SwitchSlide(currentSlide + 1); };

            g.Children.Add(prevSlideBtn);
            g.Children.Add(nextSlideBtn);

            // 3. Slides Container (6 Slides)
            Grid slidesHost = new Grid
            {
                Margin = new Thickness(42, 34, 42, 24),
                ClipToBounds = true
            };

            slidePanels[0] = BuildMediaSlide();
            slidePanels[1] = BuildSystemAndDrivesSlide();
            slidePanels[2] = BuildControlCenterSlide();
            slidePanels[3] = BuildClipboardSlide();
            slidePanels[4] = BuildNotesSlide();
            slidePanels[5] = BuildShelfSlide();

            for (int i = 0; i < SlideCount; i++)
            {
                slidePanels[i].RenderTransform = new TranslateTransform(0, 0);
                slidePanels[i].Visibility = (i == 0) ? Visibility.Visible : Visibility.Collapsed;
                slidesHost.Children.Add(slidePanels[i]);
            }
            g.Children.Add(slidesHost);

            // 4. Bottom Pagination Dots
            dotsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 9)
            };
            dotsPanel.PreviewMouseWheel += (s, e) =>
            {
                e.Handled = true;
                long now = Environment.TickCount64;
                if (now - lastSlideScrollTick >= 160)
                {
                    if (e.Delta < 0) { SwitchSlide(currentSlide + 1); lastSlideScrollTick = now; }
                    else if (e.Delta > 0) { SwitchSlide(currentSlide - 1); lastSlideScrollTick = now; }
                }
            };
            g.Children.Add(dotsPanel);

            ApplySlideVisibilityConfig();
            ApplyCustomColors();

            return g;
        }

        private Border CreateNavCircleButton(bool isLeft)
        {
            Border b = new Border
            {
                Width = 24,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                ToolTip = isLeft ? "Previous Slide (Shift + Scroll Up)" : "Next Slide (Shift + Scroll Down)"
            };
            Path arrow = new Path
            {
                Data = Geometry.Parse(isLeft ? "M 13.5,6 L 7.5,12 L 13.5,18" : "M 8.5,6 L 14.5,12 L 8.5,18"),
                Stroke = new SolidColorBrush(Color.FromRgb(215, 215, 225)),
                StrokeThickness = 2.0,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Width = 9.5,
                Height = 9.5,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            b.Child = arrow;
            b.MouseEnter += (s, e) =>
            {
                b.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
                arrow.Stroke = Brushes.White;
            };
            b.MouseLeave += (s, e) =>
            {
                b.Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255));
                arrow.Stroke = new SolidColorBrush(Color.FromRgb(215, 215, 225));
            };
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

        public void AnimateToExpanded()
        {
            if (isExpanded) return;
            isExpanded = true;

            if (clockTimer != null) clockTimer.Interval = TimeSpan.FromMilliseconds(250);
            activeLyricIndex = -1;
            UpdateMediaTimelineUI();
            if (showLyrics)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ScrollActiveLyricToCenter(animate: false);
                }), DispatcherPriority.Loaded);
            }

            notchBody.CornerRadius = new CornerRadius(26);

            var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.20 };
            Duration dur = new Duration(TimeSpan.FromMilliseconds(290));

            notchBody.BeginAnimation(WidthProperty, new DoubleAnimation(ExpandedWidth, dur) { EasingFunction = ease });
            notchBody.BeginAnimation(HeightProperty, new DoubleAnimation(ExpandedHeight, dur) { EasingFunction = ease });

            collapsedView.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(120)));
            collapsedView.IsHitTestVisible = false;

            expandedView.Visibility = Visibility.Visible;
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

            if (clockTimer != null)
            {
                clockTimer.Interval = isMusicMode ? TimeSpan.FromMilliseconds(250) : TimeSpan.FromMilliseconds(1000);
            }

            notchBody.CornerRadius = new CornerRadius(18);

            var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.16 };
            Duration dur = new Duration(TimeSpan.FromMilliseconds(270));

            var widthAnim = new DoubleAnimation(CurrentCollapsedWidth, dur) { EasingFunction = ease };
            widthAnim.Completed += (s, e) => MemoryOptimizer.TrimMemory(force: true);

            notchBody.BeginAnimation(WidthProperty, widthAnim);
            notchBody.BeginAnimation(HeightProperty, new DoubleAnimation(CollapsedHeight, dur) { EasingFunction = ease });

            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(110));
            fadeOut.Completed += (s, e) =>
            {
                if (!isExpanded) expandedView.Visibility = Visibility.Collapsed;
            };
            expandedView.BeginAnimation(OpacityProperty, fadeOut);
            expandedView.IsHitTestVisible = false;

            collapsedView.IsHitTestVisible = true;
            collapsedView.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(200))
            {
                BeginTime = TimeSpan.FromMilliseconds(60)
            });
        }

        public void ApplySlideVisibilityConfig()
        {
            activeSlideIndices.Clear();
            if (ShowSlideMedia) activeSlideIndices.Add(0);
            if (ShowSlideSystem) activeSlideIndices.Add(1);
            if (ShowSlideControl) activeSlideIndices.Add(2);
            if (ShowSlideClipboard) activeSlideIndices.Add(3);
            if (ShowSlideNotes) activeSlideIndices.Add(4);
            if (ShowSlideShelf) activeSlideIndices.Add(5);

            if (activeSlideIndices.Count == 0)
            {
                activeSlideIndices.Add(0);
                ShowSlideMedia = true;
            }

            if (dotsPanel != null)
            {
                dotsPanel.Children.Clear();
                activeDotPills.Clear();

                Color accent = ColorFromHex(ColorAccent, Color.FromRgb(28, 231, 85));

                for (int i = 0; i < activeSlideIndices.Count; i++)
                {
                    int activePos = i;
                    int slideIdx = activeSlideIndices[i];
                    Border dot = new Border
                    {
                        Width = (activePos == currentSlide) ? 18 : 5,
                        Height = 5,
                        CornerRadius = new CornerRadius(2.5),
                        Background = (activePos == currentSlide)
                            ? new SolidColorBrush(accent)
                            : new SolidColorBrush(Color.FromArgb(140, 130, 130, 140)),
                        Margin = new Thickness(3, 0, 3, 0),
                        Cursor = Cursors.Hand,
                        ToolTip = slideTitles[slideIdx]
                    };
                    dot.MouseLeftButtonUp += (s, e) => { e.Handled = true; SwitchSlide(activePos); };
                    activeDotPills.Add(dot);
                    dotsPanel.Children.Add(dot);
                }
            }

            if (prevSlideBtn != null && nextSlideBtn != null)
            {
                bool multi = activeSlideIndices.Count > 1;
                prevSlideBtn.Visibility = multi ? Visibility.Visible : Visibility.Collapsed;
                nextSlideBtn.Visibility = multi ? Visibility.Visible : Visibility.Collapsed;
            }

            if (currentSlide >= activeSlideIndices.Count)
            {
                currentSlide = 0;
            }

            SwitchSlide(currentSlide, null, false);

            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.ShowSlideMedia = ShowSlideMedia;
                CompanionInstance.ShowSlideSystem = ShowSlideSystem;
                CompanionInstance.ShowSlideControl = ShowSlideControl;
                CompanionInstance.ShowSlideClipboard = ShowSlideClipboard;
                CompanionInstance.ShowSlideNotes = ShowSlideNotes;
                CompanionInstance.ShowSlideShelf = ShowSlideShelf;
                CompanionInstance.ApplySlideVisibilityConfig();
            }
        }

        public void ApplyCustomColors()
        {
            Color colAccent = ColorFromHex(ColorAccent, Color.FromRgb(28, 231, 85));
            Color colBattery = ColorFromHex(ColorBattery, Color.FromRgb(28, 231, 85));
            Color colCpu = ColorFromHex(ColorCpu, Color.FromRgb(56, 189, 248));
            Color colRam = ColorFromHex(ColorRam, Color.FromRgb(192, 132, 252));
            Color colDisk = ColorFromHex(ColorDisk, Color.FromRgb(52, 211, 153));
            Color colSound = ColorFromHex(ColorSound, Color.FromRgb(56, 189, 248));
            Color colBright = ColorFromHex(ColorBrightness, Color.FromRgb(251, 191, 36));
            Color colWifi = ColorFromHex(ColorWifi, Color.FromRgb(16, 185, 129));

            if (collapsedBatteryArc != null) collapsedBatteryArc.Stroke = new SolidColorBrush(colBattery);
            if (expandedBatteryArc != null) expandedBatteryArc.Stroke = new SolidColorBrush(colBattery);
            if (collapsedBatteryBgRing != null) collapsedBatteryBgRing.Stroke = new SolidColorBrush(Color.FromArgb(60, colBattery.R, colBattery.G, colBattery.B));
            if (expandedBatteryBgRing != null) expandedBatteryBgRing.Stroke = new SolidColorBrush(Color.FromArgb(60, colBattery.R, colBattery.G, colBattery.B));

            if (calDayNameText != null) calDayNameText.Foreground = new SolidColorBrush(colAccent);
            if (lyricsHeaderText != null) lyricsHeaderText.Foreground = new SolidColorBrush(colAccent);
            if (lyricsBtnIcon != null && showLyrics) lyricsBtnIcon.Fill = new SolidColorBrush(colAccent);

            if (sysBatBar != null) sysBatBar.Background = new SolidColorBrush(colBattery);
            if (sysBatVal != null) sysBatVal.Foreground = new SolidColorBrush(colBattery);

            if (sysCpuBar != null) sysCpuBar.Background = new SolidColorBrush(colCpu);
            if (sysCpuVal != null) sysCpuVal.Foreground = new SolidColorBrush(colCpu);

            if (sysRamBar != null) sysRamBar.Background = new SolidColorBrush(colRam);
            if (sysRamVal != null) sysRamVal.Foreground = new SolidColorBrush(colRam);

            if (driveFillBars != null)
            {
                foreach (var df in driveFillBars) df.Background = new SolidColorBrush(colDisk);
            }
            if (driveValTexts != null)
            {
                foreach (var dv in driveValTexts) dv.Foreground = new SolidColorBrush(colDisk);
            }

            if (soundFillBorder != null) soundFillBorder.Background = new SolidColorBrush(colSound);
            if (soundValText != null) soundValText.Foreground = new SolidColorBrush(colSound);

            if (brightFillBorder != null) brightFillBorder.Background = new SolidColorBrush(colBright);
            if (brightValText != null) brightValText.Foreground = new SolidColorBrush(colBright);

            if (wifiSubText != null && wifiSignalPill != null)
            {
                wifiSubText.Foreground = new SolidColorBrush(colWifi);
                wifiSignalPill.Background = new SolidColorBrush(Color.FromArgb(45, colWifi.R, colWifi.G, colWifi.B));
            }

            SwitchSlide(currentSlide, null, false);

            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.ColorAccent = ColorAccent;
                CompanionInstance.ColorBattery = ColorBattery;
                CompanionInstance.ColorCpu = ColorCpu;
                CompanionInstance.ColorRam = ColorRam;
                CompanionInstance.ColorDisk = ColorDisk;
                CompanionInstance.ColorSound = ColorSound;
                CompanionInstance.ColorBrightness = ColorBrightness;
                CompanionInstance.ColorWifi = ColorWifi;
                CompanionInstance.ApplyCustomColors();
            }
        }

        public void SwitchSlide(int index, bool? forceForward = null, bool animate = true)
        {
            if (activeSlideIndices == null || activeSlideIndices.Count == 0) return;

            int newSlidePos = (index + activeSlideIndices.Count) % activeSlideIndices.Count;
            int oldSlidePos = currentSlide;
            int oldIdx = (oldSlidePos >= 0 && oldSlidePos < activeSlideIndices.Count) ? activeSlideIndices[oldSlidePos] : -1;
            currentSlide = newSlidePos;
            int newIdx = activeSlideIndices[currentSlide];

            bool isForward;
            if (forceForward.HasValue)
            {
                isForward = forceForward.Value;
            }
            else if (index > oldSlidePos || (oldSlidePos == activeSlideIndices.Count - 1 && newSlidePos == 0 && index != oldSlidePos))
            {
                isForward = true;
            }
            else if (index < oldSlidePos || (oldSlidePos == 0 && newSlidePos == activeSlideIndices.Count - 1 && index != oldSlidePos))
            {
                isForward = false;
            }
            else
            {
                isForward = (newSlidePos >= oldSlidePos);
            }

            if (!animate || oldIdx == newIdx || oldIdx == -1)
            {
                for (int i = 0; i < SlideCount; i++)
                {
                    if (slidePanels[i] != null)
                    {
                        slidePanels[i].BeginAnimation(OpacityProperty, null);
                        var tr = slidePanels[i].RenderTransform as TranslateTransform;
                        if (tr != null)
                        {
                            tr.BeginAnimation(TranslateTransform.XProperty, null);
                            tr.X = 0;
                        }

                        if (i == newIdx)
                        {
                            slidePanels[i].Visibility = Visibility.Visible;
                            slidePanels[i].Opacity = 1.0;
                        }
                        else
                        {
                            slidePanels[i].Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }
            else
            {
                var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                var dur = TimeSpan.FromMilliseconds(220);

                var incoming = (newIdx >= 0 && newIdx < SlideCount) ? slidePanels[newIdx] : null;
                var outgoing = (oldIdx >= 0 && oldIdx < SlideCount) ? slidePanels[oldIdx] : null;

                for (int i = 0; i < SlideCount; i++)
                {
                    if (i != newIdx && i != oldIdx && slidePanels[i] != null)
                    {
                        slidePanels[i].BeginAnimation(OpacityProperty, null);
                        var tr = slidePanels[i].RenderTransform as TranslateTransform;
                        if (tr != null) tr.BeginAnimation(TranslateTransform.XProperty, null);
                        slidePanels[i].Visibility = Visibility.Collapsed;
                    }
                }

                if (incoming != null)
                {
                    var transIn = incoming.RenderTransform as TranslateTransform;
                    if (transIn == null)
                    {
                        transIn = new TranslateTransform();
                        incoming.RenderTransform = transIn;
                    }

                    transIn.BeginAnimation(TranslateTransform.XProperty, null);
                    incoming.BeginAnimation(OpacityProperty, null);

                    double startX = isForward ? 36.0 : -36.0;
                    transIn.X = startX;
                    incoming.Opacity = 0.0;
                    incoming.Visibility = Visibility.Visible;

                    transIn.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0.0, dur) { EasingFunction = ease });
                    incoming.BeginAnimation(OpacityProperty, new DoubleAnimation(1.0, dur) { EasingFunction = ease });
                }

                if (outgoing != null && outgoing != incoming)
                {
                    var transOut = outgoing.RenderTransform as TranslateTransform;
                    if (transOut == null)
                    {
                        transOut = new TranslateTransform();
                        outgoing.RenderTransform = transOut;
                    }

                    transOut.BeginAnimation(TranslateTransform.XProperty, null);
                    outgoing.BeginAnimation(OpacityProperty, null);

                    double endX = isForward ? -36.0 : 36.0;
                    var animX = new DoubleAnimation(endX, dur) { EasingFunction = ease };
                    var animOp = new DoubleAnimation(0.0, dur) { EasingFunction = ease };

                    int capturedOldIdx = oldIdx;
                    animOp.Completed += (s, e) =>
                    {
                        if (currentSlide < activeSlideIndices.Count && activeSlideIndices[currentSlide] != capturedOldIdx)
                        {
                            outgoing.Visibility = Visibility.Collapsed;
                            transOut.X = 0;
                        }
                    };

                    transOut.BeginAnimation(TranslateTransform.XProperty, animX);
                    outgoing.BeginAnimation(OpacityProperty, animOp);
                }
            }

            Color accent = ColorFromHex(ColorAccent, Color.FromRgb(28, 231, 85));

            for (int i = 0; i < activeDotPills.Count; i++)
            {
                bool active = (i == currentSlide);
                double targetW = active ? 18.0 : 5.0;
                var targetBrush = active
                    ? new SolidColorBrush(accent)
                    : new SolidColorBrush(Color.FromArgb(140, 130, 130, 140));

                if (animate)
                {
                    activeDotPills[i].BeginAnimation(WidthProperty, new DoubleAnimation(targetW, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    });
                }
                else
                {
                    activeDotPills[i].BeginAnimation(WidthProperty, null);
                    activeDotPills[i].Width = targetW;
                }
                activeDotPills[i].Background = targetBrush;
            }

            if (newIdx == 0 && showLyrics)
            {
                ScrollActiveLyricToCenter(animate: false);
            }
        }

        private void OnWindowPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (isExpanded && NativeMethods.IsShiftDown())
            {
                e.Handled = true;
                long now = Environment.TickCount64;
                if (now - lastSlideScrollTick >= 160)
                {
                    if (e.Delta < 0)
                    {
                        SwitchSlide(currentSlide + 1);
                        lastSlideScrollTick = now;
                    }
                    else if (e.Delta > 0)
                    {
                        SwitchSlide(currentSlide - 1);
                        lastSlideScrollTick = now;
                    }
                }
            }
        }

        private void OnFastTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            string timeStr = now.ToString("HH:mm", CultureInfo.InvariantCulture);
            if (timeStr != lastFastTimeStr)
            {
                lastFastTimeStr = timeStr;
                collapsedTimeText.Text = timeStr;
                expandedTimeText.Text = timeStr;
                if (collapsedMusicTimeText != null) collapsedMusicTimeText.Text = timeStr;
            }

            string dateStr = now.ToString("ddd dd MMM yyyy", CultureInfo.InvariantCulture);
            if (dateStr != lastFastDateStr)
            {
                lastFastDateStr = dateStr;
                expandedDateText.Text = dateStr;
                UpdateCalendarUI(now);
            }

            if (isExpanded)
            {
                UpdateMediaTimelineUI();
            }
            else if (isMusicMode)
            {
                UpdateCollapsedMusicUI();
            }

            if (!IsCompanionWindow && CompanionInstance != null && CompanionInstance.IsVisible)
            {
                CompanionInstance.OnFastTick(sender, e);
            }
        }
    }
}
