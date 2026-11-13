using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Forms = System.Windows.Forms;

namespace Glance
{
    /// <summary>
    /// Jendela Pengaturan (Settings) untuk kustomisasi tampilan, palet warna, shortcut, dan visibilitas slide.
    /// </summary>
    public class SettingsWindow : Window
    {
        private MainWindow parentIsland;

        // Tab 1: Umum
        private CheckBox chkStartup;
        private CheckBox chkPin;
        private RadioButton rbScreen1;
        private RadioButton rbScreen2;
        private RadioButton rbScreenBoth;
        private Slider sliderAlpha;
        private TextBlock lblAlphaVal;

        // Tab 2: Kustom Warna
        private string tempColorAccent;
        private string tempColorBattery;
        private string tempColorCpu;
        private string tempColorRam;
        private string tempColorDisk;
        private string tempColorSound;
        private string tempColorBrightness;
        private string tempColorWifi;
        private StackPanel colorsContentStack;

        // Tab 3: Kelola Slide
        private CheckBox chkSlideMedia;
        private CheckBox chkSlideSystem;
        private CheckBox chkSlideControl;
        private CheckBox chkSlideClipboard;
        private CheckBox chkSlideNotes;
        private CheckBox chkSlideShelf;

        // Hotkey & Search Customization
        private bool tempIsSearchInstalled;
        private bool tempIsSearchEnabled;
        private Border cardSearch;
        private StackPanel spSearchBody;
        private CheckBox chkEnableSearch;
        private uint tempHotkeyModifiers;
        private uint tempHotkeyKey;
        private string tempHotkeyDisplay;
        private Border shortcutKeyBadge;
        private TextBlock shortcutKeyText;
        private Button resetHotkeyBtn;
        private bool isRecordingHotkey = false;

        // Tab Navigation
        private int currentSelectedTab = 0;
        private Border tabBtnGeneral;
        private Border tabBtnColors;
        private Border tabBtnSlides;
        private ScrollViewer scrollGeneral;
        private ScrollViewer scrollColors;
        private ScrollViewer scrollSlides;
        private Button saveBtn;

        public SettingsWindow(MainWindow owner)
        {
            parentIsland = owner;
            Title = "Glance — Settings";
            Width = 490;
            Height = 630;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            FontFamily = new FontFamily("SF Pro Display, Segoe UI Variable Display, Segoe UI, -apple-system, sans-serif");
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            Topmost = true;
            Closed += (s, e) => MemoryOptimizer.TrimMemory(force: true);

            tempIsSearchInstalled = parentIsland != null ? parentIsland.IsSearchInstalled : true;
            tempIsSearchEnabled = parentIsland != null ? parentIsland.IsSearchEnabled : true;
            tempHotkeyModifiers = parentIsland != null ? parentIsland.HotkeyModifiers : (NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT);
            tempHotkeyKey = parentIsland != null ? parentIsland.HotkeyKey : NativeMethods.VK_P;
            tempHotkeyDisplay = parentIsland != null && !string.IsNullOrEmpty(parentIsland.HotkeyDisplay) ? parentIsland.HotkeyDisplay : "Ctrl + Shift + P";

            tempColorAccent = parentIsland != null ? (parentIsland.ColorAccent ?? "#1CE755") : "#1CE755";
            tempColorBattery = parentIsland != null ? (parentIsland.ColorBattery ?? "#1CE755") : "#1CE755";
            tempColorCpu = parentIsland != null ? (parentIsland.ColorCpu ?? "#38BDF8") : "#38BDF8";
            tempColorRam = parentIsland != null ? (parentIsland.ColorRam ?? "#C084FC") : "#C084FC";
            tempColorDisk = parentIsland != null ? (parentIsland.ColorDisk ?? "#34D399") : "#34D399";
            tempColorSound = parentIsland != null ? (parentIsland.ColorSound ?? "#38BDF8") : "#38BDF8";
            tempColorBrightness = parentIsland != null ? (parentIsland.ColorBrightness ?? "#FBBF24") : "#FBBF24";
            tempColorWifi = parentIsland != null ? (parentIsland.ColorWifi ?? "#10B981") : "#10B981";

            Border outer = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(242, 16, 16, 22)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 56)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(20, 18, 20, 18),
                Effect = new DropShadowEffect { BlurRadius = 28, ShadowDepth = 6, Opacity = 0.65, Color = Colors.Black }
            };

            Grid root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) }); // Header
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) }); // Tab bar
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) }); // Save button

            // 1. Header (Title bar is draggable)
            DockPanel header = new DockPanel { VerticalAlignment = VerticalAlignment.Center, Background = Brushes.Transparent };
            header.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed && e.OriginalSource != null)
                {
                    try { DragMove(); } catch { }
                }
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

            StackPanel titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
            titleStack.Children.Add(new TextBlock
            {
                Text = "Glance Settings",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "Customize Appearance, Accent Colors & Slide Features",
                Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 155)),
                FontSize = 10.5,
                Margin = new Thickness(0, 1, 0, 0)
            });
            header.Children.Add(titleStack);
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            // 2. Tab Bar
            Grid tabBar = new Grid { Margin = new Thickness(0, 6, 0, 4) };
            tabBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tabBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            tabBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tabBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            tabBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            tabBtnGeneral = CreateTabButton("⚙️ General", () => SelectTab(0), 0);
            Grid.SetColumn(tabBtnGeneral, 0);

            tabBtnColors = CreateTabButton("🎨 Color Palette", () => SelectTab(1), 1);
            Grid.SetColumn(tabBtnColors, 2);

            tabBtnSlides = CreateTabButton("📑 Manage Slides", () => SelectTab(2), 2);
            Grid.SetColumn(tabBtnSlides, 4);

            tabBar.Children.Add(tabBtnGeneral);
            tabBar.Children.Add(tabBtnColors);
            tabBar.Children.Add(tabBtnSlides);
            Grid.SetRow(tabBar, 1);
            root.Children.Add(tabBar);

            // 3. Tab Content Hosts
            Grid contentHost = new Grid { Margin = new Thickness(0, 8, 0, 0) };

            scrollGeneral = BuildGeneralTab();
            scrollColors = BuildColorsTab();
            scrollSlides = BuildSlidesTab();

            contentHost.Children.Add(scrollGeneral);
            contentHost.Children.Add(scrollColors);
            contentHost.Children.Add(scrollSlides);
            Grid.SetRow(contentHost, 2);
            root.Children.Add(contentHost);

            // 4. Save Button
            saveBtn = new Button
            {
                Content = "Save Settings",
                Height = 38,
                Background = new SolidColorBrush(MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85))),
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            saveBtn.Click += (s, e) => OnSaveClicked();
            Grid.SetRow(saveBtn, 3);
            root.Children.Add(saveBtn);

            SelectTab(0);

            outer.Child = root;
            Content = outer;
        }

        private Border CreateTabButton(string text, Action onClick, int tabIndex)
        {
            Border btn = new Border
            {
                Height = 32,
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255))
            };
            TextBlock tb = new TextBlock
            {
                Text = text,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false
            };
            btn.Child = tb;

            btn.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                onClick();
            };

            btn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                onClick();
            };

            btn.MouseEnter += (s, e) =>
            {
                if (currentSelectedTab != tabIndex)
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                }
            };

            btn.MouseLeave += (s, e) =>
            {
                if (currentSelectedTab != tabIndex)
                {
                    btn.Background = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
                }
            };

            return btn;
        }

        private void SelectTab(int index)
        {
            currentSelectedTab = index;
            scrollGeneral.Visibility = (index == 0) ? Visibility.Visible : Visibility.Collapsed;
            scrollColors.Visibility = (index == 1) ? Visibility.Visible : Visibility.Collapsed;
            scrollSlides.Visibility = (index == 2) ? Visibility.Visible : Visibility.Collapsed;

            StyleTabButton(tabBtnGeneral, index == 0);
            StyleTabButton(tabBtnColors, index == 1);
            StyleTabButton(tabBtnSlides, index == 2);
        }

        private void StyleTabButton(Border btn, bool isSelected)
        {
            Color accent = MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85));
            btn.Background = isSelected
                ? new SolidColorBrush(accent)
                : new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
            btn.BorderBrush = isSelected
                ? new SolidColorBrush(accent)
                : new SolidColorBrush(Color.FromArgb(35, 255, 255, 255));
            btn.BorderThickness = new Thickness(1);

            TextBlock tb = btn.Child as TextBlock;
            if (tb != null)
            {
                tb.Foreground = isSelected ? Brushes.Black : new SolidColorBrush(Color.FromRgb(220, 220, 230));
                tb.FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold;
            }
        }

        private void UpdateAccentVisuals()
        {
            Color accent = MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85));
            if (saveBtn != null) saveBtn.Background = new SolidColorBrush(accent);
            StyleTabButton(tabBtnGeneral, scrollGeneral.Visibility == Visibility.Visible);
            StyleTabButton(tabBtnColors, scrollColors.Visibility == Visibility.Visible);
            StyleTabButton(tabBtnSlides, scrollSlides.Visibility == Visibility.Visible);
        }

        // =======================================================
        // TAB 1: UMUM (GENERAL & DISPLAY)
        // =======================================================
        private ScrollViewer BuildGeneralTab()
        {
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 4, 0)
            };
            StackPanel sp = new StackPanel();

            // Option 1: Run at Startup
            Border cardStartup = CreateOptionCard();
            chkStartup = new CheckBox
            {
                Content = "Run at Windows Startup (Launch automatically on boot)",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                IsChecked = StartupManager.IsStartupEnabled(),
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            cardStartup.Child = chkStartup;
            sp.Children.Add(cardStartup);

            // Option 2: Pin Expanded
            Border cardPin = CreateOptionCard();
            chkPin = new CheckBox
            {
                Content = "Keep Glance Open (Pin expanded HUD)",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                IsChecked = parentIsland != null && parentIsland.IsPinned,
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            cardPin.Child = chkPin;
            sp.Children.Add(cardPin);

            // Option 3: Multi-Monitor Display
            Border cardScreen = CreateOptionCard();
            StackPanel spScreen = new StackPanel();

            TextBlock lblScreenTitle = new TextBlock
            {
                Text = "Display Glance on Screen / Monitor:",
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
                Content = "Primary Display Only",
                GroupName = "DisplayScreenGroup",
                Foreground = Brushes.White,
                FontSize = 12,
                IsChecked = (curMode == "screen1"),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand
            };
            rbScreen2 = new RadioButton
            {
                Content = screenCount > 1 ? "Secondary Display Only" : "Secondary Display Only (Not detected)",
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
                Content = screenCount > 1 ? "Both Displays (Screen 1 & 2)" : "Both Displays (Requires 2+ monitors)",
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
            sp.Children.Add(cardScreen);

            // Option 4: Background Transparency Slider
            Border cardAlpha = CreateOptionCard();
            StackPanel spAlpha = new StackPanel();
            DockPanel dpAlpha = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            int curAlpha = parentIsland != null ? parentIsland.BackgroundAlpha : 175;
            int curPct = (int)Math.Round(curAlpha * 100.0 / 255.0);
            lblAlphaVal = new TextBlock
            {
                Text = curPct + "%",
                Foreground = new SolidColorBrush(MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85))),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(lblAlphaVal, Dock.Right);
            dpAlpha.Children.Add(lblAlphaVal);
            dpAlpha.Children.Add(new TextBlock
            {
                Text = "Frosted Glass Opacity / Background Blur",
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
            sp.Children.Add(cardAlpha);

            // Option 5: Glance Search Component Settings (ON/OFF Toggle, Installer, Shortcut Recorder)
            cardSearch = CreateOptionCard();
            spSearchBody = new StackPanel();
            cardSearch.Child = spSearchBody;
            RenderSearchOptionCard();
            sp.Children.Add(cardSearch);

            // Option 6: Auto-Update via GitHub
            sp.Children.Add(BuildUpdateOptionCard());

            sv.Content = sp;
            return sv;
        }

        private Border BuildUpdateOptionCard()
        {
            Border card = CreateOptionCard();
            StackPanel sp = new StackPanel();

            var localVer = GitHubUpdateService.GetLocalVersion();
            Color acc = MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85));

            DockPanel dpHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };
            Border badge = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(8, 2, 8, 2),
                Background = new SolidColorBrush(Color.FromArgb(45, acc.R, acc.G, acc.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, acc.R, acc.G, acc.B)),
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            badge.Child = new TextBlock
            {
                Text = "v" + (localVer?.Version ?? "1.1.0"),
                Foreground = new SolidColorBrush(acc),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold
            };
            DockPanel.SetDock(badge, Dock.Right);
            dpHeader.Children.Add(badge);

            dpHeader.Children.Add(new TextBlock
            {
                Text = "Glance Auto-Update (GitHub):",
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(dpHeader);

            TextBlock lblUpdateStatus = new TextBlock
            {
                Text = "Otomatis memeriksa versi terbaru dari repositori GitHub (Fyxis/Glance) saat aplikasi berjalan.",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 178)),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            sp.Children.Add(lblUpdateStatus);

            Button btnCheckUpdate = new Button
            {
                Content = "🔄 Check for Updates Now",
                Height = 30,
                Padding = new Thickness(12, 0, 12, 0),
                Background = new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)),
                Foreground = Brushes.White,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            btnCheckUpdate.Click += async (s, e) =>
            {
                btnCheckUpdate.IsEnabled = false;
                btnCheckUpdate.Content = "⏳ Memeriksa GitHub...";
                lblUpdateStatus.Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 205));
                lblUpdateStatus.Text = "Menghubungi GitHub untuk mengecek versi terbaru...";

                var res = await GitHubUpdateService.CheckForUpdateAsync();
                btnCheckUpdate.IsEnabled = true;
                btnCheckUpdate.Content = "🔄 Check for Updates Now";

                if (res.IsUpdateAvailable && res.RemoteVersion != null)
                {
                    lblUpdateStatus.Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85));
                    lblUpdateStatus.Text = $"✓ Versi baru ditemukan (v{res.RemoteVersion.Version})! Membuka jendela pembaruan...";
                    UpdateNotificationWindow.ShowOrActivate(res.LocalVersion, res.RemoteVersion);
                }
                else if (!string.IsNullOrEmpty(res.ErrorMessage))
                {
                    lblUpdateStatus.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                    lblUpdateStatus.Text = "Gagal memeriksa update: " + res.ErrorMessage;
                }
                else
                {
                    lblUpdateStatus.Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85));
                    lblUpdateStatus.Text = $"✓ Anda sudah menggunakan versi terbaru (v{res.LocalVersion?.Version ?? "1.1.0"}).";
                }
            };

            sp.Children.Add(btnCheckUpdate);
            card.Child = sp;
            return card;
        }

        private void RenderSearchOptionCard()
        {
            if (spSearchBody == null) return;
            spSearchBody.Children.Clear();

            DockPanel dpHeader = new DockPanel { Margin = new Thickness(0, 0, 0, 6) };

            Border badge = new Border
            {
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(7, 2, 7, 2),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(badge, Dock.Right);

            TextBlock badgeText = new TextBlock
            {
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            badge.Child = badgeText;

            if (!tempIsSearchInstalled)
            {
                badge.Background = new SolidColorBrush(Color.FromArgb(45, 245, 158, 11));
                badge.BorderBrush = new SolidColorBrush(Color.FromArgb(120, 245, 158, 11));
                badge.BorderThickness = new Thickness(1);
                badgeText.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                badgeText.Text = "NOT INSTALLED";
            }
            else if (tempIsSearchEnabled)
            {
                Color acc = MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85));
                badge.Background = new SolidColorBrush(Color.FromArgb(45, acc.R, acc.G, acc.B));
                badge.BorderBrush = new SolidColorBrush(Color.FromArgb(120, acc.R, acc.G, acc.B));
                badge.BorderThickness = new Thickness(1);
                badgeText.Foreground = new SolidColorBrush(acc);
                badgeText.Text = "ACTIVE & READY";
            }
            else
            {
                badge.Background = new SolidColorBrush(Color.FromArgb(45, 120, 120, 130));
                badge.BorderBrush = new SolidColorBrush(Color.FromArgb(100, 120, 120, 130));
                badge.BorderThickness = new Thickness(1);
                badgeText.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175));
                badgeText.Text = "DISABLED";
            }

            dpHeader.Children.Add(badge);

            TextBlock lblSearchTitle = new TextBlock
            {
                Text = "Glance Search (Spotlight Launcher):",
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            dpHeader.Children.Add(lblSearchTitle);
            spSearchBody.Children.Add(dpHeader);

            if (!tempIsSearchInstalled)
            {
                TextBlock lblNotInstalled = new TextBlock
                {
                    Text = "Glance Search component was excluded during initial setup. Install it anytime to search and launch applications, files, and queries via a global shortcut or the Notch 🔍 icon.",
                    Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 178)),
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 10),
                    TextWrapping = TextWrapping.Wrap
                };
                spSearchBody.Children.Add(lblNotInstalled);

                Button btnInstallNow = new Button
                {
                    Content = "📥 Install Glance Search Component",
                    Height = 34,
                    Padding = new Thickness(14, 0, 14, 0),
                    Background = new SolidColorBrush(MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85))),
                    Foreground = Brushes.Black,
                    FontSize = 11.5,
                    FontWeight = FontWeights.Bold,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                btnInstallNow.Click += (s, e) =>
                {
                    btnInstallNow.IsEnabled = false;
                    btnInstallNow.Content = "⏳ Installing & indexing applications...";
                    DispatcherTimer t = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
                    t.Tick += (st, ev) =>
                    {
                        t.Stop();
                        tempIsSearchInstalled = true;
                        tempIsSearchEnabled = true;
                        if (parentIsland != null)
                        {
                            parentIsland.ApplySearchConfig(true, true);
                            parentIsland.SaveAppConfig();
                        }
                        RenderSearchOptionCard();
                    };
                    t.Start();
                };

                spSearchBody.Children.Add(btnInstallNow);
                return;
            }

            TextBlock lblSearchSub = new TextBlock
            {
                Text = "Spotlight-style floating app launcher with instant keyword search and keyboard navigation.",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 178)),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8),
                TextWrapping = TextWrapping.Wrap
            };
            spSearchBody.Children.Add(lblSearchSub);

            chkEnableSearch = new CheckBox
            {
                Content = "Enable Glance Search (Global Shortcut & Notch 🔍 Button)",
                IsChecked = tempIsSearchEnabled,
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };

            Border shortcutBox = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(100, 20, 20, 26)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(45, 45, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 4)
            };

            StackPanel spShortcutInner = new StackPanel();

            TextBlock lblShortcutBoxTitle = new TextBlock
            {
                Text = "Global Keyboard Shortcut:",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 210)),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            spShortcutInner.Children.Add(lblShortcutBoxTitle);

            DockPanel dpShortcutRow = new DockPanel();

            resetHotkeyBtn = new Button
            {
                Content = "Reset to Default",
                Height = 30,
                Padding = new Thickness(12, 0, 12, 0),
                Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 230)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            resetHotkeyBtn.MouseEnter += (s, e) => resetHotkeyBtn.Background = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
            resetHotkeyBtn.MouseLeave += (s, e) => resetHotkeyBtn.Background = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
            DockPanel.SetDock(resetHotkeyBtn, Dock.Right);
            dpShortcutRow.Children.Add(resetHotkeyBtn);

            shortcutKeyBadge = new Border
            {
                Height = 30,
                Background = new SolidColorBrush(Color.FromArgb(220, 22, 22, 30)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(65, 65, 75)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 0, 10, 0),
                Cursor = Cursors.Hand,
                Focusable = true,
                Margin = new Thickness(0, 0, 8, 0)
            };

            shortcutKeyText = new TextBlock
            {
                Text = tempHotkeyDisplay,
                Foreground = new SolidColorBrush(MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85))),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
            shortcutKeyBadge.Child = shortcutKeyText;
            dpShortcutRow.Children.Add(shortcutKeyBadge);

            Action startRecording = () =>
            {
                if (!tempIsSearchEnabled) return;
                isRecordingHotkey = true;
                shortcutKeyText.Text = "⌨️ Press keys now...";
                shortcutKeyText.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
                Color acc = MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85));
                shortcutKeyBadge.BorderBrush = new SolidColorBrush(acc);
                shortcutKeyBadge.Background = new SolidColorBrush(Color.FromArgb(240, 35, 35, 45));
                shortcutKeyBadge.Focus();
            };

            Action stopRecording = () =>
            {
                isRecordingHotkey = false;
                shortcutKeyText.Text = tempHotkeyDisplay;
                Color acc = MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85));
                shortcutKeyText.Foreground = new SolidColorBrush(acc);
                shortcutKeyBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(65, 65, 75));
                shortcutKeyBadge.Background = new SolidColorBrush(Color.FromArgb(220, 22, 22, 30));
            };

            shortcutKeyBadge.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (!isRecordingHotkey) startRecording();
                else stopRecording();
            };

            shortcutKeyBadge.LostFocus += (s, e) =>
            {
                if (isRecordingHotkey) stopRecording();
            };

            shortcutKeyBadge.PreviewKeyDown += (s, e) =>
            {
                if (!isRecordingHotkey) return;
                e.Handled = true;

                Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;
                if (key == Key.Escape) { stopRecording(); return; }
                if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
                    key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin) return;

                ModifierKeys mods = Keyboard.Modifiers;
                uint win32Mods = 0;
                List<string> parts = new List<string>();
                if ((mods & ModifierKeys.Control) != 0) { win32Mods |= NativeMethods.MOD_CONTROL; parts.Add("Ctrl"); }
                if ((mods & ModifierKeys.Alt) != 0) { win32Mods |= NativeMethods.MOD_ALT; parts.Add("Alt"); }
                if ((mods & ModifierKeys.Shift) != 0) { win32Mods |= NativeMethods.MOD_SHIFT; parts.Add("Shift"); }
                if ((mods & ModifierKeys.Windows) != 0) { win32Mods |= NativeMethods.MOD_WIN; parts.Add("Win"); }

                if (win32Mods == 0)
                {
                    shortcutKeyText.Text = "Must include Ctrl, Alt, or Shift!";
                    return;
                }

                string keyName = FormatKeyName(key);
                parts.Add(keyName);
                tempHotkeyModifiers = win32Mods;
                tempHotkeyKey = (uint)KeyInterop.VirtualKeyFromKey(key);
                tempHotkeyDisplay = string.Join(" + ", parts);
                stopRecording();
            };

            resetHotkeyBtn.Click += (s, e) =>
            {
                tempHotkeyModifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;
                tempHotkeyKey = NativeMethods.VK_P;
                tempHotkeyDisplay = "Ctrl + Shift + P";
                stopRecording();
            };

            spShortcutInner.Children.Add(dpShortcutRow);
            shortcutBox.Child = spShortcutInner;

            Action updateShortcutBoxState = () =>
            {
                bool on = chkEnableSearch.IsChecked == true;
                shortcutBox.Opacity = on ? 1.0 : 0.38;
                shortcutBox.IsEnabled = on;
                if (!on && isRecordingHotkey) stopRecording();
            };

            chkEnableSearch.Click += (s, e) =>
            {
                tempIsSearchEnabled = chkEnableSearch.IsChecked == true;
                updateShortcutBoxState();

                if (tempIsSearchEnabled)
                {
                    Color acc = MainWindow.ColorFromHex(tempColorAccent, Color.FromRgb(28, 231, 85));
                    badge.Background = new SolidColorBrush(Color.FromArgb(45, acc.R, acc.G, acc.B));
                    badge.BorderBrush = new SolidColorBrush(Color.FromArgb(120, acc.R, acc.G, acc.B));
                    badgeText.Foreground = new SolidColorBrush(acc);
                    badgeText.Text = "ACTIVE & READY";
                }
                else
                {
                    badge.Background = new SolidColorBrush(Color.FromArgb(45, 120, 120, 130));
                    badge.BorderBrush = new SolidColorBrush(Color.FromArgb(100, 120, 120, 130));
                    badgeText.Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175));
                    badgeText.Text = "DISABLED";
                }
            };

            updateShortcutBoxState();

            spSearchBody.Children.Add(chkEnableSearch);
            spSearchBody.Children.Add(shortcutBox);
        }

        private static string FormatKeyName(Key key)
        {
            if (key >= Key.D0 && key <= Key.D9)
            {
                return ((int)key - (int)Key.D0).ToString();
            }
            if (key >= Key.NumPad0 && key <= Key.NumPad9)
            {
                return "Num " + ((int)key - (int)Key.NumPad0).ToString();
            }
            if (key == Key.Space) return "Space";
            if (key == Key.Return) return "Enter";
            if (key == Key.Tab) return "Tab";
            if (key == Key.Back) return "Backspace";
            if (key == Key.OemTilde || (int)key == 146) return "`";
            if (key == Key.OemQuotes) return "'";
            if (key == Key.OemQuestion) return "/";
            if (key == Key.OemPeriod) return ".";
            if (key == Key.OemComma) return ",";
            if (key == Key.OemPlus) return "+";
            if (key == Key.OemMinus) return "-";
            return key.ToString().ToUpperInvariant();
        }

        // =======================================================
        // TAB 2: KUSTOM WARNA PER AREA
        // =======================================================
        private ScrollViewer BuildColorsTab()
        {
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 4, 0)
            };
            colorsContentStack = new StackPanel();
            RenderColorCards();
            sv.Content = colorsContentStack;
            return sv;
        }

        private void RenderColorCards()
        {
            colorsContentStack.Children.Clear();

            TextBlock info = new TextBlock
            {
                Text = "Select a preset color swatch or enter a custom HEX code for each HUD element:",
                Foreground = new SolidColorBrush(Color.FromRgb(175, 175, 190)),
                FontSize = 11.5,
                Margin = new Thickness(2, 0, 2, 8)
            };
            colorsContentStack.Children.Add(info);

            colorsContentStack.Children.Add(CreateColorSettingCard(
                "Main Accent Color",
                "Accent color for calendar, active lyrics, controls, and sliders.",
                tempColorAccent,
                c => { tempColorAccent = c; UpdateAccentVisuals(); }
            ));

            colorsContentStack.Children.Add(CreateColorSettingCard(
                "Battery HUD & Ring",
                "Progress arc ring on collapsed notch, header, and battery level bars.",
                tempColorBattery,
                c => tempColorBattery = c
            ));

            colorsContentStack.Children.Add(CreateColorSettingCard(
                "Processor (CPU)",
                "Performance telemetry bar and load percentage text.",
                tempColorCpu,
                c => tempColorCpu = c
            ));

            colorsContentStack.Children.Add(CreateColorSettingCard(
                "Memory (RAM)",
                "Physical RAM consumption bar and usage telemetry.",
                tempColorRam,
                c => tempColorRam = c
            ));

            colorsContentStack.Children.Add(CreateColorSettingCard(
                "Storage (Drives)",
                "Partition capacity bars for fixed storage drives (C:, D:).",
                tempColorDisk,
                c => tempColorDisk = c
            ));

            colorsContentStack.Children.Add(CreateColorSettingCard(
                "Sound & Master Volume",
                "Master audio slider and volume indicator in Control Center.",
                tempColorSound,
                c => tempColorSound = c
            ));

            colorsContentStack.Children.Add(CreateColorSettingCard(
                "Display Brightness",
                "Monitor hardware brightness slider in Control Center.",
                tempColorBrightness,
                c => tempColorBrightness = c
            ));

            colorsContentStack.Children.Add(CreateColorSettingCard(
                "Wi-Fi & Speed Meter",
                "Connection pill, signal status, and live throughput speed meter.",
                tempColorWifi,
                c => tempColorWifi = c
            ));

            Button btnResetColors = new Button
            {
                Content = "↺ Reset All Colors to Default",
                Height = 32,
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 230)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 4, 0, 10)
            };
            btnResetColors.Click += (s, e) =>
            {
                tempColorAccent = "#1CE755";
                tempColorBattery = "#1CE755";
                tempColorCpu = "#38BDF8";
                tempColorRam = "#C084FC";
                tempColorDisk = "#34D399";
                tempColorSound = "#38BDF8";
                tempColorBrightness = "#FBBF24";
                tempColorWifi = "#10B981";
                UpdateAccentVisuals();
                RenderColorCards();
            };
            colorsContentStack.Children.Add(btnResetColors);
        }

        private Border CreateColorSettingCard(string title, string subtitle, string initialHex, Action<string> onColorPicked)
        {
            Border card = CreateOptionCard();
            StackPanel sp = new StackPanel();

            DockPanel top = new DockPanel { Margin = new Thickness(0, 0, 0, 7) };

            Border swatch = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(11),
                Background = new SolidColorBrush(MainWindow.ColorFromHex(initialHex, Colors.White)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                BorderThickness = new Thickness(1.5),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Current color"
            };
            DockPanel.SetDock(swatch, Dock.Right);
            top.Children.Add(swatch);

            StackPanel labelStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            TextBlock lblTitle = new TextBlock
            {
                Text = title,
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold
            };
            TextBlock lblSub = new TextBlock
            {
                Text = subtitle,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175)),
                FontSize = 10.5,
                Margin = new Thickness(0, 1, 0, 0)
            };
            labelStack.Children.Add(lblTitle);
            labelStack.Children.Add(lblSub);
            top.Children.Add(labelStack);

            sp.Children.Add(top);

            WrapPanel swatchesPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            string[] palette = {
                "#1CE755", // Emerald Green
                "#00E5FF", // Cyan
                "#38BDF8", // Sky Blue
                "#A855F7", // Neon Violet
                "#EC4899", // Hot Pink
                "#F59E0B", // Sunset Amber
                "#EF4444", // Red
                "#FFFFFF"  // White
            };

            TextBox hexBox = null;

            foreach (string hex in palette)
            {
                string h = hex;
                Border pill = new Border
                {
                    Width = 20,
                    Height = 20,
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(MainWindow.ColorFromHex(h, Colors.White)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 0, 7, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = h
                };
                pill.PreviewMouseLeftButtonDown += (s, e) =>
                {
                    e.Handled = true;
                    swatch.Background = new SolidColorBrush(MainWindow.ColorFromHex(h, Colors.White));
                    if (hexBox != null) hexBox.Text = h;
                    onColorPicked(h);
                };
                pill.MouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                };
                pill.MouseEnter += (s, e) => { pill.BorderBrush = Brushes.White; };
                pill.MouseLeave += (s, e) => { pill.BorderBrush = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)); };
                swatchesPanel.Children.Add(pill);
            }

            hexBox = new TextBox
            {
                Text = initialHex,
                Width = 75,
                Height = 22,
                FontSize = 11,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4, 0, 2, 0),
                Margin = new Thickness(4, 0, 0, 0),
                ToolTip = "Enter HEX color code (e.g. #1CE755)"
            };
            hexBox.TextChanged += (s, e) =>
            {
                string text = hexBox.Text.Trim();
                if ((text.Length == 7 && text.StartsWith("#")) || text.Length == 6)
                {
                    Color c = MainWindow.ColorFromHex(text, Colors.Transparent);
                    if (c.A > 0)
                    {
                        swatch.Background = new SolidColorBrush(c);
                        onColorPicked(text);
                    }
                }
            };
            swatchesPanel.Children.Add(hexBox);

            sp.Children.Add(swatchesPanel);
            card.Child = sp;
            return card;
        }

        // =======================================================
        // TAB 3: KELOLA SLIDE (SLIDE VISIBILITY TOGGLE)
        // =======================================================
        private ScrollViewer BuildSlidesTab()
        {
            ScrollViewer sv = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 4, 0)
            };
            StackPanel sp = new StackPanel();

            TextBlock info = new TextBlock
            {
                Text = "Select which slides appear inside the Glance notch carousel:",
                Foreground = new SolidColorBrush(Color.FromRgb(175, 175, 190)),
                FontSize = 11.5,
                Margin = new Thickness(2, 0, 2, 8)
            };
            sp.Children.Add(info);

            sp.Children.Add(CreateSlideCheckCard(
                "🎵 Media Player & Calendar / Lyrics",
                "GSMTC media playback, album art, interactive seekbar, synced lyrics, and 7-day calendar.",
                parentIsland == null || parentIsland.ShowSlideMedia,
                out chkSlideMedia
            ));

            sp.Children.Add(CreateSlideCheckCard(
                "⚡ System & Storage Telemetry",
                "Real-time battery status, CPU workload, physical RAM, and fixed drive partition meters.",
                parentIsland == null || parentIsland.ShowSlideSystem,
                out chkSlideSystem
            ));

            sp.Children.Add(CreateSlideCheckCard(
                "🎛️ Control Center (Wi-Fi, Audio, Brightness)",
                "Wi-Fi telemetry & network speed, master hardware volume slider, and display brightness.",
                parentIsland == null || parentIsland.ShowSlideControl,
                out chkSlideControl
            ));

            sp.Children.Add(CreateSlideCheckCard(
                "📋 Clipboard History",
                "Automatic clipboard history manager with copy-back shortcuts and favorite pinning.",
                parentIsland == null || parentIsland.ShowSlideClipboard,
                out chkSlideClipboard
            ));

            sp.Children.Add(CreateSlideCheckCard(
                "📝 Quick Notes (Scratchpad)",
                "Instant scratchpad for to-dos, thoughts, and snippets without opening heavy editors.",
                parentIsland == null || parentIsland.ShowSlideNotes,
                out chkSlideNotes
            ));

            sp.Children.Add(CreateSlideCheckCard(
                "📥 Drop Shelf (File Staging)",
                "Drag-and-drop staging zone to easily stash files and drag them directly into chat/apps.",
                parentIsland == null || parentIsland.ShowSlideShelf,
                out chkSlideShelf
            ));

            TextBlock tip = new TextBlock
            {
                Text = "💡 Tip: At least 1 slide must remain checked so the notch carousel navigation can operate.",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 165)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(2, 4, 2, 10)
            };
            sp.Children.Add(tip);

            sv.Content = sp;
            return sv;
        }

        private Border CreateSlideCheckCard(string title, string desc, bool isChecked, out CheckBox chkOut)
        {
            Border card = CreateOptionCard();
            card.Cursor = Cursors.Hand;
            StackPanel sp = new StackPanel();

            CheckBox chk = new CheckBox
            {
                Content = title,
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                IsChecked = isChecked,
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            chkOut = chk;

            TextBlock tbDesc = new TextBlock
            {
                Text = desc,
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175)),
                FontSize = 10.5,
                Margin = new Thickness(24, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap,
                Cursor = Cursors.Hand
            };
            tbDesc.PreviewMouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                chk.IsChecked = !chk.IsChecked;
            };

            sp.Children.Add(chk);
            sp.Children.Add(tbDesc);
            card.Child = sp;
            return card;
        }

        private void OnSaveClicked()
        {
            try
            {
                StartupManager.SetStartup(chkStartup.IsChecked == true);
                string selectedMode = "screen1";
                if (rbScreen2.IsChecked == true) selectedMode = "screen2";
                else if (rbScreenBoth.IsChecked == true) selectedMode = "both";

                if (parentIsland != null)
                {
                    // Colors
                    parentIsland.ColorAccent = tempColorAccent;
                    parentIsland.ColorBattery = tempColorBattery;
                    parentIsland.ColorCpu = tempColorCpu;
                    parentIsland.ColorRam = tempColorRam;
                    parentIsland.ColorDisk = tempColorDisk;
                    parentIsland.ColorSound = tempColorSound;
                    parentIsland.ColorBrightness = tempColorBrightness;
                    parentIsland.ColorWifi = tempColorWifi;

                    // Slides (Ensure at least 1 is active)
                    bool m = chkSlideMedia.IsChecked == true;
                    bool s = chkSlideSystem.IsChecked == true;
                    bool c = chkSlideControl.IsChecked == true;
                    bool cl = chkSlideClipboard.IsChecked == true;
                    bool n = chkSlideNotes.IsChecked == true;
                    bool sh = chkSlideShelf.IsChecked == true;

                    if (!m && !s && !c && !cl && !n && !sh)
                    {
                        m = true;
                    }

                    parentIsland.ShowSlideMedia = m;
                    parentIsland.ShowSlideSystem = s;
                    parentIsland.ShowSlideControl = c;
                    parentIsland.ShowSlideClipboard = cl;
                    parentIsland.ShowSlideNotes = n;
                    parentIsland.ShowSlideShelf = sh;

                    parentIsland.SetPinned(chkPin.IsChecked == true);
                    parentIsland.ApplySearchConfig(tempIsSearchInstalled, tempIsSearchEnabled);
                    parentIsland.UpdateGlobalHotkey(tempHotkeyModifiers, tempHotkeyKey, tempHotkeyDisplay);
                    parentIsland.ApplyCustomColors();
                    parentIsland.ApplySlideVisibilityConfig();
                }

                MainWindow.ApplyDisplayMode(selectedMode);

                if (parentIsland != null)
                {
                    parentIsland.SaveAppConfig();
                }
            }
            catch { }

            Close();
        }

        private Border CreateOptionCard()
        {
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(140, 28, 28, 34)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 56)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 8)
            };
        }
    }
}
