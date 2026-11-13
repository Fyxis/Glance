using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Path = System.Windows.Shapes.Path;

namespace Glance
{
    /// <summary>
    /// Overlay pencarian instan ala MacBook Spotlight (Ctrl + Shift + P) untuk membuka aplikasi dengan cepat.
    /// </summary>
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
                                    ? "Installed App"
                                    : ("App • " + folderName);

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
                                                Subtitle = "System App",
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
            FontFamily = new FontFamily("SF Pro Display, Segoe UI Variable Display, Segoe UI, -apple-system, sans-serif");
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            Topmost = true;
            Closed += (s, e) => MemoryOptimizer.TrimMemory(force: true);
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
                Text = "Search apps, calculate (250*14), commands, or web (g / yt / ai)...",
                Foreground = new SolidColorBrush(Color.FromRgb(125, 125, 138)),
                FontSize = 16,
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
            cachedApps = null;
            MemoryOptimizer.TrimMemory(force: true);
        }

        private void OnSearchInputChanged(object sender, TextChangedEventArgs e)
        {
            string rawInput = (searchInput.Text ?? "").Trim();
            string query = rawInput.ToLowerInvariant();
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

            var combined = new List<AppSearchEntry>();

            // 1. Instant Math Calculator Result (Top Priority if valid math expression)
            if (SearchQueryService.TryEvaluateCalculator(rawInput, out AppSearchEntry calcEntry))
            {
                combined.Add(calcEntry);
            }

            // 2. Check Web Search Prefixes (e.g. "g ", "yt ", "gh ", "ai ")
            var webEntries = SearchQueryService.GetWebSearchEntries(rawInput, out bool isExplicitWebPrefix);
            if (isExplicitWebPrefix)
            {
                combined.AddRange(webEntries);
            }

            // 3. Matching Installed / System Applications
            var scoredApps = new List<Tuple<int, AppSearchEntry>>();
            if (cachedApps != null)
            {
                foreach (var app in cachedApps)
                {
                    if (app.LowerName.StartsWith(query))
                    {
                        scoredApps.Add(Tuple.Create(1, app));
                    }
                    else if (app.Words != null && app.Words.Any(w => w.StartsWith(query)))
                    {
                        scoredApps.Add(Tuple.Create(2, app));
                    }
                    else if (app.LowerName.Contains(query))
                    {
                        scoredApps.Add(Tuple.Create(3, app));
                    }
                }
            }

            var topApps = scoredApps
                .OrderBy(t => t.Item1)
                .ThenBy(t => t.Item2.Name.Length)
                .ThenBy(t => t.Item2.Name)
                .Select(t => t.Item2)
                .Take(5)
                .ToList();

            combined.AddRange(topApps);

            // 4. Matching Quick System Commands (Lock, Sleep, Empty Recycle Bin, Mute, Settings, etc.)
            var cmdMatches = SearchQueryService.GetMatchingSystemCommands(query);
            combined.AddRange(cmdMatches);

            // 5. Web Search Fallback (Google & YouTube) if not already added via prefix
            if (!isExplicitWebPrefix)
            {
                combined.AddRange(webEntries);
            }

            currentMatches = combined.Take(8).ToList();
            RenderSearchResults();
        }

        private void RenderSearchResults()
        {
            resultsStack.Children.Clear();
            resultRowBorders.Clear();
            selectedIndex = 0;

            resultsDivider.Visibility = Visibility.Visible;
            resultsStack.Visibility = Visibility.Visible;

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
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });

                Color badgeCol = MainWindow.ColorFromHex(app.BadgeColorHex, Color.FromRgb(28, 231, 85));

                if (app.EntryKind == "app")
                {
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
                }
                else
                {
                    Border emojiBadge = new Border
                    {
                        Width = 27,
                        Height = 27,
                        CornerRadius = new CornerRadius(7),
                        Background = new SolidColorBrush(Color.FromArgb(48, badgeCol.R, badgeCol.G, badgeCol.B)),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(95, badgeCol.R, badgeCol.G, badgeCol.B)),
                        BorderThickness = new Thickness(1),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    emojiBadge.Child = new TextBlock
                    {
                        Text = !string.IsNullOrEmpty(app.EmojiIcon) ? app.EmojiIcon : "✨",
                        FontSize = 13.5,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(emojiBadge, 0);
                    g.Children.Add(emojiBadge);
                }

                StackPanel textCol = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                textCol.Children.Add(new TextBlock
                {
                    Text = app.Name,
                    Foreground = app.EntryKind == "calc" ? new SolidColorBrush(badgeCol) : Brushes.White,
                    FontSize = app.EntryKind == "calc" ? 15 : 13.5,
                    FontWeight = app.EntryKind == "calc" ? FontWeights.Bold : FontWeights.SemiBold,
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
                    Text = !string.IsNullOrEmpty(app.ActionHint) ? app.ActionHint : "Open ↵",
                    Foreground = new SolidColorBrush(badgeCol),
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
                    ExecuteEntry(app);
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
                    ExecuteEntry(currentMatches[selectedIndex]);
                }
            }
        }

        private void ExecuteEntry(AppSearchEntry entry)
        {
            if (entry == null) return;
            HideSpotlight();

            try
            {
                if (entry.EntryKind == "calc")
                {
                    if (!string.IsNullOrEmpty(entry.CopyText))
                    {
                        Clipboard.SetText(entry.CopyText);
                        MainWindow.PrimaryInstance?.ShowLiveActivityToast(
                            "🧮",
                            "Copied Result",
                            entry.CopyText,
                            100,
                            false,
                            Color.FromRgb(56, 189, 248),
                            1800
                        );
                    }
                    return;
                }

                if (entry.EntryKind == "command")
                {
                    ExecuteSystemCommand(entry.CommandId);
                    return;
                }

                if (!string.IsNullOrEmpty(entry.LaunchPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = entry.LaunchPath,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }

        private void ExecuteSystemCommand(string commandId)
        {
            var main = MainWindow.PrimaryInstance;
            switch (commandId)
            {
                case "cmd_settings":
                    main?.OpenSettingsDialog();
                    break;
                case "cmd_update":
                    main?.CheckForUpdatesInteractive(true);
                    break;
                case "cmd_mute":
                    HardwareController.ToggleAudioMute();
                    break;
                case "cmd_pin":
                    if (main != null)
                    {
                        bool nextPin = !main.IsPinned;
                        main.SetPinned(nextPin);
                        main.ShowLiveActivityToast("📌", "Notch Pin", nextPin ? "Pinned" : "Unpinned", 100, false, Color.FromRgb(245, 158, 11), 1600);
                    }
                    break;
                case "cmd_aod":
                    if (main != null)
                    {
                        bool nextAod = !main.EnableSleepMode;
                        main.ApplySleepConfig(nextAod, main.SleepTimeoutSeconds);
                        main.SaveAppConfig();
                        main.ShowLiveActivityToast("🌙", "Sleep Mode (AOD)", nextAod ? "Enabled" : "Disabled", 100, false, Color.FromRgb(168, 85, 247), 1800);
                    }
                    break;
                case "cmd_lock":
                    NativeMethods.LockWorkStation();
                    break;
                case "cmd_sleep_pc":
                    NativeMethods.SetSuspendState(false, false, false);
                    break;
                case "cmd_recycle":
                    Task.Run(() =>
                    {
                        try
                        {
                            NativeMethods.SHEmptyRecycleBin(
                                IntPtr.Zero,
                                null,
                                NativeMethods.SHERB_NOCONFIRMATION | NativeMethods.SHERB_NOPROGRESSUI | NativeMethods.SHERB_NOSOUND
                            );
                        }
                        catch { }
                    });
                    main?.ShowLiveActivityToast("🗑️", "Recycle Bin", "Emptied", 100, false, Color.FromRgb(239, 68, 68), 2000);
                    break;
                case "cmd_taskmgr":
                    Process.Start(new ProcessStartInfo { FileName = "taskmgr.exe", UseShellExecute = true });
                    break;
                case "cmd_winsettings":
                    Process.Start(new ProcessStartInfo { FileName = "ms-settings:", UseShellExecute = true });
                    break;
                case "cmd_explorer_restart":
                    Task.Run(() =>
                    {
                        try
                        {
                            foreach (var p in Process.GetProcessesByName("explorer"))
                            {
                                try { p.Kill(); p.WaitForExit(1500); } catch { }
                            }
                            Process.Start(new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true });
                        }
                        catch { }
                    });
                    main?.ShowLiveActivityToast("🔃", "Explorer", "Restarted", 100, false, Color.FromRgb(249, 115, 22), 2000);
                    break;
            }
        }
    }
}
