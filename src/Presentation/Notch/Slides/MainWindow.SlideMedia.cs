using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;

namespace Glance
{
    /// <summary>
    /// Slide 0: Media Player Controls (Kiri) + Kalender 7-Hari & Synced Lyrics .LRC (Kanan).
    /// </summary>
    public partial class MainWindow
    {
        private Grid BuildMediaSlide()
        {
            Grid slide = new Grid();
            slide.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(176) });
            slide.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            slide.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ---------------------------------------------------
            // LEFT COLUMN: MEDIA PLAYER CONTROLS
            // ---------------------------------------------------
            mediaStandardPanel = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            mediaStandardPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
            mediaStandardPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            mediaStandardPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(34) });

            // 1. Top Track Info (Album Art + Title/Artist)
            Grid topRow = new Grid { VerticalAlignment = VerticalAlignment.Center };
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(46) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            albumArtBox = new Border
            {
                Width = 46,
                Height = 46,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            albumImageBrush = new ImageBrush { Stretch = Stretch.UniformToFill };

            albumPlaceholderIcon = new Path
            {
                Data = Geometry.Parse("M 12,3 L 12,13.55 C 11.41,13.21 10.73,13 10,13 C 7.79,13 6,14.79 6,17 C 6,19.21 7.79,21 10,21 C 12.21,21 14,19.21 14,17 L 14,7 L 18,7 L 18,3 L 12,3 Z"),
                Fill = new SolidColorBrush(Color.FromArgb(160, 180, 180, 190)),
                Width = 22,
                Height = 22,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            albumArtBox.Child = albumPlaceholderIcon;
            Grid.SetColumn(albumArtBox, 0);

            StackPanel titleCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            mediaTitleText = new TextBlock
            {
                Text = "No media playing",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            mediaArtistText = new TextBlock
            {
                Text = "",
                Foreground = new SolidColorBrush(Color.FromRgb(175, 175, 185)),
                FontSize = 10.5,
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

            // 2. Timeline Progress Bar
            Grid progRow = new Grid { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 3, 0, 0) };
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            progRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

            timeCurrentText = new TextBlock
            {
                Text = "00:00",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
                FontSize = 10,
                FontWeight = FontWeights.Medium,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timeCurrentText, 0);

            progressTrackBorder = new Border
            {
                Height = 4,
                CornerRadius = new CornerRadius(2),
                Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ClipToBounds = true
            };
            progressFillBorder = new Border
            {
                Height = 4,
                Width = 0,
                CornerRadius = new CornerRadius(2),
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
                FontSize = 10,
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(timeRemainingText, 4);

            progRow.Children.Add(timeCurrentText);
            progRow.Children.Add(progressTrackBorder);
            progRow.Children.Add(timeRemainingText);
            Grid.SetRow(progRow, 1);

            // 3. Transport Controls Row (Lyrics Toggle + Prev + Play/Pause + Next)
            StackPanel controlsRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            lyricsBtnBorder = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(6),
                Background = Brushes.Transparent,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                ToolTip = "Toggle synchronized lyrics"
            };
            lyricsBtnIcon = new Path
            {
                Data = Geometry.Parse("M 4,4 L 20,4 C 21.1,4 22,4.9 22,6 L 22,16 C 22,17.1 21.1,18 20,18 L 8,18 L 4,22 L 4,6 C 4,4.9 4.9,4 4,4 Z M 8,9 L 11,9 L 11,12 L 9.5,14 L 8,14 L 9.2,12 L 8,12 Z M 13,9 L 16,9 L 16,12 L 14.5,14 L 13,14 L 14.2,12 L 13,12 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                Width = 15,
                Height = 15,
                Stretch = Stretch.Uniform
            };
            lyricsBtnBorder.Child = lyricsBtnIcon;
            lyricsBtnBorder.MouseEnter += (s, e) => { if (!showLyrics) lyricsBtnIcon.Fill = Brushes.White; };
            lyricsBtnBorder.MouseLeave += (s, e) => { if (!showLyrics) lyricsBtnIcon.Fill = new SolidColorBrush(Color.FromRgb(150, 150, 160)); };
            lyricsBtnBorder.MouseLeftButtonUp += (s, e) => { e.Handled = true; ToggleLyricsView(); };
            controlsRow.Children.Add(lyricsBtnBorder);

            Border btnPrev = CreateMediaIconBtn("M 11,18 L 11,6 L 2.5,12 Z M 11.5,12 L 20,18 L 20,6 Z", 14, "Previous Track");
            btnPrev.Margin = new Thickness(0, 0, 6, 0);
            btnPrev.MouseLeftButtonUp += (s, e) => { e.Handled = true; pendingCommand = "prev"; };

            Border btnPlay = CreateMediaIconBtn("M 8,5 L 8,19 L 19,12 Z", 15, "Play / Pause", out playPauseIconPath);
            btnPlay.Width = 34;
            btnPlay.Height = 34;
            btnPlay.CornerRadius = new CornerRadius(17);
            btnPlay.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            btnPlay.BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
            btnPlay.BorderThickness = new Thickness(1);
            btnPlay.Margin = new Thickness(0, 0, 6, 0);
            btnPlay.MouseEnter += (s, e) => btnPlay.Background = new SolidColorBrush(Color.FromArgb(75, 255, 255, 255));
            btnPlay.MouseLeave += (s, e) => btnPlay.Background = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            btnPlay.MouseLeftButtonUp += (s, e) => { e.Handled = true; pendingCommand = "play-pause"; };

            Border btnNext = CreateMediaIconBtn("M 4,18 L 12.5,12 L 4,6 Z M 13,6 L 13,18 L 21.5,12 Z", 14, "Next Track");
            btnNext.MouseLeftButtonUp += (s, e) => { e.Handled = true; pendingCommand = "next"; };

            controlsRow.Children.Add(btnPrev);
            controlsRow.Children.Add(btnPlay);
            controlsRow.Children.Add(btnNext);
            Grid.SetRow(controlsRow, 2);

            mediaStandardPanel.Children.Add(topRow);
            mediaStandardPanel.Children.Add(progRow);
            mediaStandardPanel.Children.Add(controlsRow);
            Grid.SetColumn(mediaStandardPanel, 0);

            // ---------------------------------------------------
            // CENTER COLUMN: VERTICAL FROSTED SEPARATOR
            // ---------------------------------------------------
            Border sep = new Border
            {
                Width = 1,
                Height = 110,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255))
            };
            Grid.SetColumn(sep, 1);

            // ---------------------------------------------------
            // RIGHT COLUMN: CARD HOST FOR CALENDAR OR LYRICS
            // ---------------------------------------------------
            Border rightCard = new Border
            {
                Height = 138,
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(22, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(7, 6, 7, 6),
                ClipToBounds = true
            };

            Grid rightContainer = new Grid();

            // 1. Calendar Panel (Default View)
            calendarPanel = new Border
            {
                Background = Brushes.Transparent,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ToolTip = "Glance Calendar (Click to open Windows Clock, click ♪ for lyrics)"
            };
            calendarPanel.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                try { Process.Start(new ProcessStartInfo("ms-clock:") { UseShellExecute = true }); } catch { }
            };

            StackPanel calStack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            calDayNameText = new TextBlock
            {
                Text = "DAY",
                Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 0)
            };

            calDateNumText = new TextBlock
            {
                Text = "00",
                Foreground = Brushes.White,
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, -3, 0, -3)
            };

            calMonthYearText = new TextBlock
            {
                Text = "Month Year",
                Foreground = new SolidColorBrush(Color.FromRgb(185, 185, 195)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 7)
            };

            calWeekStrip = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            calStack.Children.Add(calDayNameText);
            calStack.Children.Add(calDateNumText);
            calStack.Children.Add(calMonthYearText);
            calStack.Children.Add(calWeekStrip);
            calendarPanel.Child = calStack;

            // 2. Lyrics Panel (Toggled View)
            lyricsPanel = new Grid
            {
                Visibility = Visibility.Collapsed,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            lyricsPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(20) });
            lyricsPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid lyricsHeaderRow = new Grid();
            lyricsHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            lyricsHeaderRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });

            lyricsHeaderText = new TextBlock
            {
                Text = "Lyrics",
                Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lyricsHeaderText, 0);

            Border closeLyricsBtn = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Cursor = Cursors.Hand,
                ToolTip = "Back to Calendar",
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            TextBlock closeLyricsIcon = new TextBlock
            {
                Text = "✕",
                FontSize = 9,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            closeLyricsBtn.Child = closeLyricsIcon;
            closeLyricsBtn.MouseEnter += (s, e) => closeLyricsBtn.Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255));
            closeLyricsBtn.MouseLeave += (s, e) => closeLyricsBtn.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
            closeLyricsBtn.MouseLeftButtonUp += (s, e) => { e.Handled = true; ToggleLyricsView(); };
            Grid.SetColumn(closeLyricsBtn, 1);

            lyricsHeaderRow.Children.Add(lyricsHeaderText);
            lyricsHeaderRow.Children.Add(closeLyricsBtn);
            Grid.SetRow(lyricsHeaderRow, 0);

            lyricsScroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 2, 0, 2),
                Focusable = false
            };
            lyricsScroll.PreviewMouseWheel += (s, e) => StopLyricScrollAnimation();
            lyricsStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 42, 0, 50)
            };
            lyricsScroll.Content = lyricsStack;
            Grid.SetRow(lyricsScroll, 1);

            lyricsPanel.Children.Add(lyricsHeaderRow);
            lyricsPanel.Children.Add(lyricsScroll);

            rightContainer.Children.Add(calendarPanel);
            rightContainer.Children.Add(lyricsPanel);
            rightCard.Child = rightContainer;
            Grid.SetColumn(rightCard, 2);

            UpdateCalendarUI(DateTime.Now);

            slide.Children.Add(mediaStandardPanel);
            slide.Children.Add(sep);
            slide.Children.Add(rightCard);

            return slide;
        }

        private void UpdateCalendarUI(DateTime now)
        {
            if (calDayNameText == null) return;
            if (lastCalendarDay == now.Day && !string.IsNullOrEmpty(calDayNameText.Text)) return;
            lastCalendarDay = now.Day;

            Color accent = ColorFromHex(ColorAccent, Color.FromRgb(28, 231, 85));

            int dayOfWeekIdx = (int)now.DayOfWeek;
            calDayNameText.Text = EnglishDayNames[dayOfWeekIdx];
            calDayNameText.Foreground = new SolidColorBrush(accent);
            calDateNumText.Text = now.Day.ToString();
            calMonthYearText.Text = EnglishMonthNames[now.Month - 1] + " " + now.Year;

            if (calWeekStrip != null)
            {
                calWeekStrip.Children.Clear();
                int deltaFromMonday = ((int)now.DayOfWeek + 6) % 7;
                DateTime monday = now.Date.AddDays(-deltaFromMonday);

                for (int i = 0; i < 7; i++)
                {
                    DateTime day = monday.AddDays(i);
                    bool isToday = (day.Date == now.Date);
                    string initial = DayInitials[(int)day.DayOfWeek];

                    Border pill = new Border
                    {
                        Width = 15,
                        Height = 27,
                        CornerRadius = new CornerRadius(4.5),
                        Background = isToday
                            ? new SolidColorBrush(accent)
                            : Brushes.Transparent,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(1.5, 0, 1.5, 0)
                    };

                    StackPanel pillStack = new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    TextBlock tInit = new TextBlock
                    {
                        Text = initial,
                        FontSize = 7.5,
                        FontWeight = isToday ? FontWeights.Bold : FontWeights.Medium,
                        Foreground = isToday
                            ? Brushes.Black
                            : new SolidColorBrush(Color.FromRgb(135, 135, 148)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    TextBlock tNum = new TextBlock
                    {
                        Text = day.Day.ToString(),
                        FontSize = 8.5,
                        FontWeight = isToday ? FontWeights.Bold : FontWeights.SemiBold,
                        Foreground = isToday
                            ? Brushes.Black
                            : new SolidColorBrush(Color.FromRgb(215, 215, 225)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 1, 0, 0)
                    };

                    pillStack.Children.Add(tInit);
                    pillStack.Children.Add(tNum);
                    pill.Child = pillStack;
                    calWeekStrip.Children.Add(pill);
                }
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

        private void ToggleLyricsView()
        {
            showLyrics = !showLyrics;
            Color colAccent = ColorFromHex(ColorAccent, Color.FromRgb(28, 231, 85));
            lyricsBtnBorder.Background = showLyrics
                ? new SolidColorBrush(Color.FromArgb(45, colAccent.R, colAccent.G, colAccent.B))
                : Brushes.Transparent;
            lyricsBtnIcon.Fill = showLyrics
                ? new SolidColorBrush(colAccent)
                : Brushes.White;

            if (calendarPanel != null) calendarPanel.Visibility = showLyrics ? Visibility.Collapsed : Visibility.Visible;
            if (lyricsPanel != null) lyricsPanel.Visibility = showLyrics ? Visibility.Visible : Visibility.Collapsed;

            if (showLyrics)
            {
                FetchLyricsIfNeeded();
                activeLyricIndex = -1;
                UpdateMediaTimelineUI();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ScrollActiveLyricToCenter(animate: false);
                }), DispatcherPriority.Loaded);
            }
        }

        private void FetchLyricsIfNeeded()
        {
            string title = currentTitle;
            string artist = currentArtist;
            if (string.IsNullOrWhiteSpace(title))
            {
                loadedLyricsKey = "";
                loadedLyricsTrackDur = 0;
                syncedLyrics.Clear();
                lyricsHeaderText.Text = "Lyrics";
                lyricsStack.Margin = new Thickness(0);
                lyricsStack.Children.Clear();
                lyricsStack.Children.Add(new TextBlock
                {
                    Text = "Play a song in Spotify or your browser to view synchronized lyrics",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                    FontSize = 10.5,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 18, 0, 0)
                });
                return;
            }

            string key = title + "|" + artist;
            if (key == loadedLyricsKey)
            {
                activeLyricIndex = -1;
                UpdateMediaTimelineUI();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ScrollActiveLyricToCenter(animate: false);
                }), DispatcherPriority.Loaded);
                return;
            }
            loadedLyricsKey = key;
            loadedLyricsTrackDur = currentDur;
            syncedLyrics.Clear();
            activeLyricIndex = -1;

            lyricsHeaderText.Text = string.IsNullOrEmpty(artist) ? title : (title + " — " + artist);
            lyricsStack.Margin = new Thickness(0);
            lyricsStack.Children.Clear();
            lyricsStack.Children.Add(new TextBlock
            {
                Text = "Searching for lyrics...",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
                FontSize = 10.5,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 26, 0, 0)
            });

            Task.Run(() =>
            {
                for (int w = 0; w < 7 && currentDur <= 0; w++)
                {
                    Thread.Sleep(50);
                }
                double targetDur = currentDur;
                loadedLyricsTrackDur = targetDur;

                string cleanTitle = LyricsScoringService.CleanTrackTitle(title);
                string cleanArtist = LyricsScoringService.CleanArtistName(artist);

                List<LrcSearchItem> candidates = new List<LrcSearchItem>();
                if (!string.IsNullOrEmpty(cleanTitle) && !string.IsNullOrEmpty(cleanArtist))
                {
                    string structuredUrl = "https://lrclib.net/api/search?track_name=" + Uri.EscapeDataString(cleanTitle) + "&artist_name=" + Uri.EscapeDataString(cleanArtist);
                    candidates.AddRange(LrcLibApiClient.FetchCandidates(structuredUrl));
                }

                bool hasGoodDurationMatch = candidates.Any(c =>
                    !string.IsNullOrWhiteSpace(c.syncedLyrics) &&
                    (targetDur <= 10.0 || (c.duration > 10.0 && Math.Abs(c.duration - targetDur) <= 2.5)));

                if (!hasGoodDurationMatch)
                {
                    string qUrl = "https://lrclib.net/api/search?q=" + Uri.EscapeDataString((cleanTitle + " " + cleanArtist).Trim());
                    candidates.AddRange(LrcLibApiClient.FetchCandidates(qUrl));
                }

                List<LyricLine> parsedSynced = new List<LyricLine>();
                List<string> parsedPlain = new List<string>();

                if (candidates.Count > 0)
                {
                    var best = candidates
                        .OrderByDescending(x => LyricsScoringService.ScoreCandidate(x, title, cleanTitle, cleanArtist, targetDur))
                        .FirstOrDefault();

                    if (best != null)
                    {
                        if (!string.IsNullOrWhiteSpace(best.syncedLyrics))
                        {
                            parsedSynced = LyricsScoringService.ParseSyncedLrc(best.syncedLyrics);
                        }
                        else if (!string.IsNullOrWhiteSpace(best.plainLyrics))
                        {
                            parsedPlain.AddRange(best.plainLyrics.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
                        }
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    if (loadedLyricsKey != key) return;
                    lyricsStack.Children.Clear();
                    if (parsedSynced.Count > 0)
                    {
                        syncedLyrics = parsedSynced;
                        lyricsStack.Margin = new Thickness(0, 42, 0, 50);
                        if (!IsCompanionWindow && CompanionInstance != null)
                        {
                            CompanionInstance.syncedLyrics = parsedSynced;
                            if (CompanionInstance.isMusicMode)
                            {
                                CompanionInstance.UpdateCollapsedMusicUI();
                            }
                        }
                        foreach (var l in syncedLyrics)
                        {
                            double jumpSec = l.TimeSec;
                            TextBlock tb = new TextBlock
                            {
                                Text = l.Text,
                                Foreground = new SolidColorBrush(Color.FromRgb(145, 145, 155)),
                                FontSize = 10,
                                FontWeight = FontWeights.Normal,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(2, 3, 2, 3),
                                Cursor = Cursors.Hand
                            };
                            tb.MouseLeftButtonUp += (s, ev) =>
                            {
                                ev.Handled = true;
                                pendingSeekSec = jumpSec;
                                currentPos = jumpSec;
                                lastPosSync = DateTime.UtcNow;
                                activeLyricIndex = -1;
                                UpdateMediaTimelineUI();
                            };
                            lyricsStack.Children.Add(tb);
                        }
                        activeLyricIndex = -1;
                        UpdateMediaTimelineUI();
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            ScrollActiveLyricToCenter(animate: false);
                        }), DispatcherPriority.Loaded);
                    }
                    else if (parsedPlain.Count > 0)
                    {
                        lyricsStack.Margin = new Thickness(0, 8, 0, 12);
                        foreach (string line in parsedPlain)
                        {
                            lyricsStack.Children.Add(new TextBlock
                            {
                                Text = line,
                                Foreground = Brushes.White,
                                FontSize = 10,
                                TextAlignment = TextAlignment.Center,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(2, 2, 2, 2)
                            });
                        }
                    }
                    else
                    {
                        lyricsStack.Margin = new Thickness(0);
                        lyricsStack.Children.Add(new TextBlock
                        {
                            Text = "No lyrics found for this track",
                            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                            FontSize = 10.5,
                            TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 26, 0, 0)
                        });
                    }

                    if (isMusicMode)
                    {
                        UpdateCollapsedMusicUI();
                    }
                });
            });
        }

        private void StopLyricScrollAnimation()
        {
            if (lyricScrollAnimTimer != null && lyricScrollAnimTimer.IsEnabled)
            {
                lyricScrollAnimTimer.Stop();
            }
        }

        private void ScrollActiveLyricToCenter(bool animate = true)
        {
            if (lyricsScroll == null || lyricsStack == null) return;
            if (activeLyricIndex < 0 || activeLyricIndex >= lyricsStack.Children.Count) return;

            FrameworkElement targetElement = lyricsStack.Children[activeLyricIndex] as FrameworkElement;
            if (targetElement == null) return;

            try
            {
                lyricsStack.UpdateLayout();
                lyricsScroll.UpdateLayout();
            }
            catch { }

            double itemTop = lyricsStack.Margin.Top;
            try
            {
                GeneralTransform gt = targetElement.TransformToAncestor(lyricsStack);
                Point pt = gt.Transform(new Point(0, 0));
                itemTop = lyricsStack.Margin.Top + pt.Y;
            }
            catch
            {
                itemTop = lyricsStack.Margin.Top;
                for (int k = 0; k < activeLyricIndex; k++)
                {
                    var ch = lyricsStack.Children[k] as FrameworkElement;
                    if (ch != null) itemTop += ch.ActualHeight + ch.Margin.Top + ch.Margin.Bottom;
                }
            }

            double itemHeight = targetElement.ActualHeight > 0 ? targetElement.ActualHeight : 20.0;
            double itemCenter = itemTop + (itemHeight / 2.0);

            double viewportHeight = lyricsScroll.ViewportHeight;
            if (viewportHeight <= 0) viewportHeight = lyricsScroll.ActualHeight;
            if (viewportHeight <= 0) viewportHeight = 102.0;

            double targetOffset = itemCenter - (viewportHeight / 2.0);
            if (targetOffset < 0) targetOffset = 0;
            if (lyricsScroll.ScrollableHeight > 0 && targetOffset > lyricsScroll.ScrollableHeight)
            {
                targetOffset = lyricsScroll.ScrollableHeight;
            }

            if (!animate || Math.Abs(lyricsScroll.VerticalOffset - targetOffset) < 1.0)
            {
                StopLyricScrollAnimation();
                lyricsScroll.ScrollToVerticalOffset(targetOffset);
                return;
            }

            StopLyricScrollAnimation();
            lyricAnimStartOffset = lyricsScroll.VerticalOffset;
            lyricAnimTargetOffset = targetOffset;
            lyricAnimStartTime = DateTime.UtcNow;

            if (lyricScrollAnimTimer == null)
            {
                lyricScrollAnimTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(16)
                };
                lyricScrollAnimTimer.Tick += (s, e) =>
                {
                    double elapsed = (DateTime.UtcNow - lyricAnimStartTime).TotalMilliseconds;
                    double t = Math.Max(0.0, Math.Min(1.0, elapsed / LyricAnimDurationMs));
                    double ease = 1.0 - Math.Pow(1.0 - t, 3);
                    double current = lyricAnimStartOffset + (lyricAnimTargetOffset - lyricAnimStartOffset) * ease;
                    lyricsScroll.ScrollToVerticalOffset(current);

                    if (t >= 1.0)
                    {
                        lyricScrollAnimTimer.Stop();
                        lyricsScroll.ScrollToVerticalOffset(lyricAnimTargetOffset);
                    }
                };
            }
            lyricScrollAnimTimer.Start();
        }

        private void UpdateMediaTimelineUI()
        {
            if (!isExpanded) return;

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
                double lyricPos = pos + 0.25;
                int idx = -1;
                for (int i = 0; i < syncedLyrics.Count; i++)
                {
                    if (lyricPos >= syncedLyrics[i].TimeSec) idx = i;
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
                        Color colAccent = ColorFromHex(ColorAccent, Color.FromRgb(28, 231, 85));
                        tb.Foreground = active ? new SolidColorBrush(colAccent) : new SolidColorBrush(Color.FromRgb(145, 145, 155));
                        tb.FontWeight = active ? FontWeights.Bold : FontWeights.Normal;
                        tb.FontSize = active ? 11.5 : 10.0;
                        tb.Opacity = active ? 1.0 : (Math.Abs(i - activeLyricIndex) == 1 ? 0.65 : 0.40);
                    }
                    ScrollActiveLyricToCenter(animate: true);
                }
            }
        }
    }
}
