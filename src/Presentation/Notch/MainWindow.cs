using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;

namespace Glance
{
    /// <summary>
    /// Jendela utama Dynamic Island Notch (Lifecycle, State, Multi-Monitor, Hotkeys & System Tray).
    /// Implementasi UI dipecah ke beberapa file partial berdasarkan bagian dan slide.
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int HOTKEY_ID_SPOTLIGHT = 9001;
        private const double CollapsedWidth = 216;
        private const double CollapsedHeight = 36;
        private const double ExpandedWidth = 426;
        private const double ExpandedHeight = 216;
        private const int SlideCount = 6;

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
        private volatile bool isExpanded = false;
        private bool isInputFocused = false;
        private int currentSlide = 0;
        private long lastSlideScrollTick = 0;

        // Customizable Area Colors
        public string ColorAccent { get; set; }
        public string ColorBattery { get; set; }
        public string ColorCpu { get; set; }
        public string ColorRam { get; set; }
        public string ColorDisk { get; set; }
        public string ColorSound { get; set; }
        public string ColorBrightness { get; set; }
        public string ColorWifi { get; set; }

        // Customizable Slide Visibility
        public bool ShowSlideMedia { get; set; }
        public bool ShowSlideSystem { get; set; }
        public bool ShowSlideControl { get; set; }
        public bool ShowSlideClipboard { get; set; }
        public bool ShowSlideNotes { get; set; }
        public bool ShowSlideShelf { get; set; }

        // Customizable Hotkey & Search Component
        public uint HotkeyModifiers { get; set; }
        public uint HotkeyKey { get; set; }
        public string HotkeyDisplay { get; set; }
        public bool IsSearchInstalled { get; set; } = true;
        public bool IsSearchEnabled { get; set; } = true;

        // Customizable Sleep / Always-On Display (AOD) Mode when Mouse is Idle
        private const double SleepCollapsedWidth = 100;
        private const double SleepCollapsedHeight = 28;
        public bool EnableSleepMode { get; set; } = true;
        public int SleepTimeoutSeconds { get; set; } = 5;
        private bool isSleeping = false;
        private Grid collapsedSleepLayout;
        private TextBlock collapsedSleepTimeText;
        private DispatcherTimer mouseIdleTimer;
        private NativeMethods.POINT lastMousePt;
        private DateTime lastMouseMoveUtc = DateTime.UtcNow;

        // Live Activities / Contextual Toast Overlay on Collapsed Notch
        private const double ToastCollapsedWidth = 258;
        private bool isToastActive = false;
        private Grid collapsedToastLayout;
        private Border toastIconBadge;
        private TextBlock toastIconText;
        private TextBlock toastTitleText;
        private Border toastBarTrack;
        private Border toastBarFill;
        private TextBlock toastValueText;
        private DispatcherTimer toastDismissTimer;
        private bool telemetryInitialized = false;
        private int lastToastVolPct = -1;
        private bool lastToastMuted = false;
        private int lastToastBrightPct = -1;
        private bool? lastChargingState = null;
        private bool lowBatteryWarned = false;

        private SolidColorBrush notchBgBrush;
        private static SpotlightWindow spotlightWin;
        private static SettingsWindow activeSettingsWin;

        // Core UI Elements
        private Border searchQuickBtn;
        private Forms.ToolStripMenuItem traySearchItem;
        private Border notchBody;
        private Grid collapsedView;
        private Grid expandedView;
        private TextBlock collapsedTimeText;
        private TextBlock expandedDateText;
        private TextBlock expandedTimeText;
        private const double CollapsedBatterySize = 20.0;
        private const double CollapsedBatteryStroke = 2.8;
        private const double ExpandedBatterySize = 18.0;
        private const double ExpandedBatteryStroke = 2.8;
        private Path collapsedBatteryArc;
        private Path expandedBatteryArc;
        private Ellipse collapsedBatteryBgRing;
        private Ellipse expandedBatteryBgRing;
        private Border pinBtnBorder;
        private Path pinBtnIcon;

        // Slides & Dynamic Dots
        private Grid[] slidePanels = new Grid[SlideCount];
        private StackPanel dotsPanel;
        private List<Border> activeDotPills = new List<Border>();
        private List<int> activeSlideIndices = new List<int>();
        private Border prevSlideBtn;
        private Border nextSlideBtn;
        private string[] slideTitles =
        {
            "Media Player & Calendar / Lyrics",
            "Battery, CPU, RAM & Storage (C: / D:)",
            "Wi-Fi, Sound & Brightness",
            "Clipboard History",
            "Quick Notes",
            "File Shelf (Staging Zone)"
        };

        // Slide 0: Media Player + Calendar / Lyrics UI
        private Grid mediaStandardPanel;
        private Border calendarPanel;
        private TextBlock calDayNameText;
        private TextBlock calDateNumText;
        private TextBlock calMonthYearText;
        private StackPanel calWeekStrip;
        private int lastCalendarDay = -1;
        private static readonly string[] EnglishDayNames = { "SUNDAY", "MONDAY", "TUESDAY", "WEDNESDAY", "THURSDAY", "FRIDAY", "SATURDAY" };
        private static readonly string[] EnglishMonthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
        private static readonly string[] DayInitials = { "S", "M", "T", "W", "T", "F", "S" };
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
        private TextBlock collapsedNetDownText;
        private TextBlock collapsedNetUpText;

        // Collapsed music mode (album art + lyrics + live waveform, like iPhone Dynamic Island)
        private const double MusicCollapsedWidth = 230;
        private const double MusicLyricCollapsedWidth = 310;
        private Grid collapsedIdleLayout;
        private Grid collapsedMusicLayout;
        private Border collapsedArtBox;
        private ImageBrush collapsedArtBrush;
        private Path collapsedArtPlaceholder;
        private Grid collapsedMusicMidGrid;
        private TextBlock collapsedMusicLyricText;
        private TextBlock collapsedMusicTimeText;
        private Rectangle[] waveBars;
        private double[] waveLevels;
        private readonly double[] waveProfile = { 0.62, 0.92, 1.0, 0.8, 0.58 };
        private DispatcherTimer waveformTimer;
        private bool isMusicMode = false;
        private DateTime lastPlayingSeenUtc = DateTime.MinValue;
        private readonly Random waveRng = new Random();
        private readonly SolidColorBrush waveBrush = new SolidColorBrush(Color.FromRgb(28, 231, 85));
        private int waveformTickCount = 0;

        private double CurrentCollapsedWidth
        {
            get
            {
                if (!isMusicMode) return CollapsedWidth;
                bool hasLyric = !string.IsNullOrWhiteSpace(GetCurrentLyricText());
                return hasLyric ? MusicLyricCollapsedWidth : MusicCollapsedWidth;
            }
        }

        // Slide 3 & 4: Clipboard & Notes UI
        private StackPanel clipListPanel;
        private TextBox noteInputBox;
        private StackPanel notesListPanel;

        // Slide 5: Drop Shelf UI (File Staging Area)
        private StackPanel shelfListPanel;
        private TextBlock shelfCountText;
        private string shelfFilePath;
        private List<ShelfItem> shelfList = new List<ShelfItem>();
        private static readonly Dictionary<string, ImageSource> fileIconCache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);

        // Timers & State
        private DispatcherTimer clockTimer;
        private DispatcherTimer collapseDelayTimer;
        private Forms.NotifyIcon trayIcon;
        private int lastBatPct = -1;
        private string lastFastTimeStr = "";
        private string lastFastDateStr = "";

        // Media & Lyrics State
        private string currentTitle = "";
        private string currentArtist = "";
        private string currentStatus = "None";
        private double currentPos = 0;
        private double currentDur = 0;
        private double lastRawSmtcPos = -1;
        private DateTime lastPosSync = DateTime.UtcNow;
        private bool showLyrics = false;
        private string loadedLyricsKey = "";
        private double loadedLyricsTrackDur = 0;
        private List<LyricLine> syncedLyrics = new List<LyricLine>();
        private int activeLyricIndex = -1;
        private DispatcherTimer lyricScrollAnimTimer;
        private double lyricAnimStartOffset;
        private double lyricAnimTargetOffset;
        private DateTime lyricAnimStartTime;
        private const double LyricAnimDurationMs = 280.0;

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
        private JsonHelper json = new JsonHelper();

        public MainWindow(Forms.Screen targetScreen = null, bool isCompanion = false)
        {
            IsCompanionWindow = isCompanion;
            if (!isCompanion)
            {
                PrimaryInstance = this;
            }

            BackgroundAlpha = 175;
            ColorAccent = "#1CE755";
            ColorBattery = "#1CE755";
            ColorCpu = "#38BDF8";
            ColorRam = "#C084FC";
            ColorDisk = "#34D399";
            ColorSound = "#38BDF8";
            ColorBrightness = "#FBBF24";
            ColorWifi = "#10B981";
            ShowSlideMedia = true;
            ShowSlideSystem = true;
            ShowSlideControl = true;
            ShowSlideClipboard = true;
            ShowSlideNotes = true;
            ShowSlideShelf = true;

            HotkeyModifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;
            HotkeyKey = NativeMethods.VK_P;
            HotkeyDisplay = "Ctrl + Shift + P";
            IsSearchInstalled = true;
            IsSearchEnabled = true;

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
            shelfFilePath = System.IO.Path.Combine(dataDir, "shelf.json");
            configFilePath = System.IO.Path.Combine(dataDir, "config.json");

            if (!IsCompanionWindow)
            {
                LoadSavedData();
            }
            else if (PrimaryInstance != null)
            {
                BackgroundAlpha = PrimaryInstance.BackgroundAlpha;
                IsPinned = PrimaryInstance.IsPinned;

                ColorAccent = PrimaryInstance.ColorAccent;
                ColorBattery = PrimaryInstance.ColorBattery;
                ColorCpu = PrimaryInstance.ColorCpu;
                ColorRam = PrimaryInstance.ColorRam;
                ColorDisk = PrimaryInstance.ColorDisk;
                ColorSound = PrimaryInstance.ColorSound;
                ColorBrightness = PrimaryInstance.ColorBrightness;
                ColorWifi = PrimaryInstance.ColorWifi;

                ShowSlideMedia = PrimaryInstance.ShowSlideMedia;
                ShowSlideSystem = PrimaryInstance.ShowSlideSystem;
                ShowSlideControl = PrimaryInstance.ShowSlideControl;
                ShowSlideClipboard = PrimaryInstance.ShowSlideClipboard;
                ShowSlideNotes = PrimaryInstance.ShowSlideNotes;
                ShowSlideShelf = PrimaryInstance.ShowSlideShelf;

                HotkeyModifiers = PrimaryInstance.HotkeyModifiers;
                HotkeyKey = PrimaryInstance.HotkeyKey;
                HotkeyDisplay = PrimaryInstance.HotkeyDisplay;
                IsSearchInstalled = PrimaryInstance.IsSearchInstalled;
                IsSearchEnabled = PrimaryInstance.IsSearchEnabled;
                EnableSleepMode = PrimaryInstance.EnableSleepMode;
                SleepTimeoutSeconds = PrimaryInstance.SleepTimeoutSeconds;

                notesList = new List<NoteItem>(PrimaryInstance.notesList);
                clipList = new List<ClipItem>(PrimaryInstance.clipList);
                shelfList = new List<ShelfItem>(PrimaryInstance.shelfList);
            }

            Title = isCompanion ? "Glance (Companion)" : "Glance";
            Width = 490;
            Height = 255;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            FontFamily = new FontFamily("SF Pro Display, Segoe UI Variable Display, Segoe UI, -apple-system, sans-serif");
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(this, TextRenderingMode.ClearType);
            Topmost = true;
            Closed += (s, e) =>
            {
                MemoryOptimizer.TrimMemory(force: true);
            };
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
            PreviewMouseWheel += OnWindowPreviewMouseWheel;

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
                        NativeMethods.RemoveClipboardFormatListener(hwnd);
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

        public static Color ColorFromHex(string hex, Color defaultColor)
        {
            if (string.IsNullOrWhiteSpace(hex)) return defaultColor;
            try
            {
                hex = hex.Trim();
                if (hex.StartsWith("#")) hex = hex.Substring(1);
                if (hex.Length == 6)
                {
                    byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                    return Color.FromRgb(r, g, b);
                }
                else if (hex.Length == 8)
                {
                    byte a = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    byte r = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                    return Color.FromArgb(a, r, g, b);
                }
            }
            catch { }
            return defaultColor;
        }

        public void SetBackgroundAlpha(byte alpha)
        {
            BackgroundAlpha = alpha;
            if (notchBgBrush != null)
            {
                notchBgBrush.Color = Color.FromArgb(BackgroundAlpha, 5, 5, 8);
            }
            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.SetBackgroundAlpha(alpha);
            }
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
                        CompanionInstance.shelfList = new List<ShelfItem>(PrimaryInstance.shelfList);
                        CompanionInstance.RenderShelfList();
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
                    HwndSource src = HwndSource.FromHwnd(hwnd);
                    if (src != null)
                    {
                        src.AddHook(WndProc);
                    }
                    UpdateGlobalHotkey(HotkeyModifiers, HotkeyKey, HotkeyDisplay);
                    ApplySearchConfig(IsSearchInstalled, IsSearchEnabled);
                    NativeMethods.AddClipboardFormatListener(hwnd);

                    clockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000) };
                    clockTimer.Tick += OnFastTick;
                    clockTimer.Start();
                    OnFastTick(null, null);

                    NativeMethods.GetCursorPos(out lastMousePt);
                    lastMouseMoveUtc = DateTime.UtcNow;
                    mouseIdleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
                    mouseIdleTimer.Tick += OnMouseIdleTick;
                    mouseIdleTimer.Start();

                    var startupTrimTimer1 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1500) };
                    startupTrimTimer1.Tick += (s, ev) =>
                    {
                        startupTrimTimer1.Stop();
                        MemoryOptimizer.TrimMemory(force: true);
                    };
                    startupTrimTimer1.Start();

                    var startupTrimTimer2 = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(4000) };
                    startupTrimTimer2.Tick += (s, ev) =>
                    {
                        startupTrimTimer2.Stop();
                        MemoryOptimizer.TrimMemory(force: true);
                    };
                    startupTrimTimer2.Start();

                    var maintenanceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5.0) };
                    maintenanceTimer.Tick += (s, ev) =>
                    {
                        bool anyExpanded = isExpanded || (CompanionInstance != null && CompanionInstance.isExpanded);
                        if (!anyExpanded && (spotlightWin == null || !spotlightWin.IsVisible))
                        {
                            MemoryOptimizer.TrimMemory(force: true);
                        }
                    };
                    maintenanceTimer.Start();

                    Task.Run(() => BackgroundMediaAndSystemLoop());

                    ApplyCustomColors();
                    ApplySlideVisibilityConfig();
                    ApplyDisplayMode(DisplayMode);

                    // Auto-Update Check on Startup (after 5s) & Periodic Check (every 15 mins)
                    var startupUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5.0) };
                    startupUpdateTimer.Tick += (s, ev) =>
                    {
                        startupUpdateTimer.Stop();
                        CheckForUpdatesInteractive(false);
                    };
                    startupUpdateTimer.Start();

                    var periodicUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(3.0) };
                    periodicUpdateTimer.Tick += (s, ev) =>
                    {
                        CheckForUpdatesInteractive(false);
                    };
                    periodicUpdateTimer.Start();
                }
            }
            catch (Exception ex)
            {
                try { File.WriteAllText("d:\\Coding-an\\windows-notch\\error.log", "OnWindowLoaded error: " + ex.ToString()); } catch { }
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == HOTKEY_ID_SPOTLIGHT)
                {
                    handled = true;
                    ToggleSpotlightSearch();
                }
            }
            else if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                handled = true;
                OnClipboardTick(null, null);
            }
            else if (msg == NativeMethods.WM_MOUSEHWHEEL && isExpanded)
            {
                short delta = (short)((wParam.ToInt64() >> 16) & 0xFFFF);
                long now = Environment.TickCount64;
                if (now - lastSlideScrollTick >= 160)
                {
                    if (delta > 0)
                    {
                        SwitchSlide(currentSlide + 1);
                        lastSlideScrollTick = now;
                        handled = true;
                    }
                    else if (delta < 0)
                    {
                        SwitchSlide(currentSlide - 1);
                        lastSlideScrollTick = now;
                        handled = true;
                    }
                }
            }
            return IntPtr.Zero;
        }

        public void ApplySearchConfig(bool isInstalled, bool isEnabled)
        {
            IsSearchInstalled = isInstalled;
            IsSearchEnabled = isEnabled;

            bool active = IsSearchInstalled && IsSearchEnabled;

            if (searchQuickBtn != null)
            {
                searchQuickBtn.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            }

            if (traySearchItem != null)
            {
                traySearchItem.Visible = active;
            }

            if (!IsCompanionWindow)
            {
                try
                {
                    IntPtr hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        NativeMethods.UnregisterHotKey(hwnd, HOTKEY_ID_SPOTLIGHT);
                        if (active)
                        {
                            NativeMethods.RegisterHotKey(
                                hwnd,
                                HOTKEY_ID_SPOTLIGHT,
                                HotkeyModifiers | NativeMethods.MOD_NOREPEAT,
                                HotkeyKey
                            );
                        }
                    }
                }
                catch { }

                if (!active && spotlightWin != null && spotlightWin.IsVisible)
                {
                    spotlightWin.HideSpotlight();
                }

                UpdateHotkeyTooltips();
            }

            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.IsSearchInstalled = isInstalled;
                CompanionInstance.IsSearchEnabled = isEnabled;
                CompanionInstance.ApplySearchConfig(isInstalled, isEnabled);
            }
        }

        public void UpdateGlobalHotkey(uint modifiers, uint key, string display)
        {
            if (modifiers == 0 || key == 0)
            {
                modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;
                key = NativeMethods.VK_P;
                display = "Ctrl + Shift + P";
            }

            HotkeyModifiers = modifiers;
            HotkeyKey = key;
            HotkeyDisplay = string.IsNullOrWhiteSpace(display) ? "Ctrl + Shift + P" : display;

            bool active = IsSearchInstalled && IsSearchEnabled;

            if (!IsCompanionWindow)
            {
                try
                {
                    IntPtr hwnd = new WindowInteropHelper(this).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        NativeMethods.UnregisterHotKey(hwnd, HOTKEY_ID_SPOTLIGHT);
                        if (active)
                        {
                            bool registered = NativeMethods.RegisterHotKey(
                                hwnd,
                                HOTKEY_ID_SPOTLIGHT,
                                HotkeyModifiers | NativeMethods.MOD_NOREPEAT,
                                HotkeyKey
                            );

                            if (!registered)
                            {
                                HotkeyModifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;
                                HotkeyKey = NativeMethods.VK_P;
                                HotkeyDisplay = "Ctrl + Shift + P";
                                NativeMethods.RegisterHotKey(
                                    hwnd,
                                    HOTKEY_ID_SPOTLIGHT,
                                    HotkeyModifiers | NativeMethods.MOD_NOREPEAT,
                                    HotkeyKey
                                );
                            }
                        }
                    }
                }
                catch { }

                UpdateHotkeyTooltips();
            }

            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.HotkeyModifiers = HotkeyModifiers;
                CompanionInstance.HotkeyKey = HotkeyKey;
                CompanionInstance.HotkeyDisplay = HotkeyDisplay;
                CompanionInstance.UpdateHotkeyTooltips();
            }
        }

        public void UpdateHotkeyTooltips()
        {
            bool active = IsSearchInstalled && IsSearchEnabled;
            if (searchQuickBtn != null)
            {
                searchQuickBtn.ToolTip = "Glance Search (" + HotkeyDisplay + ")";
                searchQuickBtn.Visibility = active ? Visibility.Visible : Visibility.Collapsed;
            }
            if (traySearchItem != null)
            {
                traySearchItem.Text = "Glance Search (" + HotkeyDisplay + ")";
                traySearchItem.Visible = active;
            }
        }

        public void ToggleSpotlightSearch()
        {
            if (!IsSearchInstalled || !IsSearchEnabled) return;

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
                ? new SolidColorBrush(Color.FromArgb(50, 28, 231, 85))
                : Brushes.Transparent;
            if (pinBtnIcon != null)
            {
                pinBtnIcon.Fill = IsPinned
                    ? new SolidColorBrush(Color.FromRgb(28, 231, 85))
                    : new SolidColorBrush(Color.FromRgb(165, 165, 175));
            }

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
            notchBgBrush = new SolidColorBrush(Color.FromArgb(BackgroundAlpha, 5, 5, 8));

            Grid rootGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top
            };

            rootGrid.MouseEnter += (s, e) =>
            {
                collapseDelayTimer.Stop();
                AnimateToExpanded();
            };
            rootGrid.MouseLeave += (s, e) =>
            {
                if (!IsPinned && !isInputFocused)
                {
                    collapseDelayTimer.Start();
                }
            };

            notchBody = new Border
            {
                Width = CollapsedWidth,
                Height = CollapsedHeight,
                Background = notchBgBrush,
                BorderBrush = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 8, 0, 0),
                SnapsToDevicePixels = true,
                AllowDrop = true,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 24,
                    ShadowDepth = 4,
                    Direction = 270,
                    Opacity = 0.52,
                    Color = Colors.Black
                }
            };

            notchBody.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                    notchBody.BorderBrush = new SolidColorBrush(Color.FromRgb(28, 231, 85));
                    notchBody.BorderThickness = new Thickness(1);
                }
            };
            notchBody.DragOver += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effects = DragDropEffects.Copy;
                    e.Handled = true;
                }
            };
            notchBody.DragLeave += (s, e) =>
            {
                notchBody.BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
                notchBody.BorderThickness = new Thickness(1, 0, 1, 1);
            };
            notchBody.Drop += (s, e) =>
            {
                notchBody.BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255));
                notchBody.BorderThickness = new Thickness(1, 0, 1, 1);
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Length > 0)
                    {
                        AddFilesToShelf(files);
                        int shelfPos = activeSlideIndices.IndexOf(5);
                        if (shelfPos >= 0)
                        {
                            SwitchSlide(shelfPos);
                        }
                        AnimateToExpanded();
                    }
                }
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

            Grid innerContainer = new Grid { ClipToBounds = true };
            collapsedView = BuildCollapsedView();
            expandedView = BuildExpandedView();
            expandedView.Opacity = 0;
            expandedView.IsHitTestVisible = false;
            expandedView.Visibility = Visibility.Collapsed;

            innerContainer.Children.Add(collapsedView);
            innerContainer.Children.Add(expandedView);
            notchBody.Child = innerContainer;

            rootGrid.Children.Add(notchBody);
            Content = rootGrid;
        }

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
            traySearchItem = new Forms.ToolStripMenuItem("Glance Search (" + HotkeyDisplay + ")", null, (s, e) => Dispatcher.Invoke(ToggleSpotlightSearch));
            traySearchItem.Visible = (IsSearchInstalled && IsSearchEnabled);
            menu.Items.Add(traySearchItem);

            menu.Items.Add("Glance Settings...", null, (s, e) => Dispatcher.Invoke(OpenSettingsDialog));
            menu.Items.Add("Check for Updates...", null, (s, e) => Dispatcher.Invoke(() => CheckForUpdatesInteractive(true)));

            int screenCount = Forms.Screen.AllScreens.Length;
            Forms.ToolStripMenuItem screenSubMenu = new Forms.ToolStripMenuItem("Display Selection");

            trayItemScr1 = new Forms.ToolStripMenuItem("Primary Display Only", null, (s, e) => ApplyDisplayMode("screen1"));
            trayItemScr2 = new Forms.ToolStripMenuItem(screenCount > 1 ? "Secondary Display Only" : "Secondary Display Only (Not detected)", null, (s, e) => { if (screenCount > 1) ApplyDisplayMode("screen2"); });
            trayItemBoth = new Forms.ToolStripMenuItem(screenCount > 1 ? "Both Displays" : "Both Displays (Requires 2+ monitors)", null, (s, e) => { if (screenCount > 1) ApplyDisplayMode("both"); });

            trayItemScr2.Enabled = screenCount > 1;
            trayItemBoth.Enabled = screenCount > 1;

            screenSubMenu.DropDownItems.Add(trayItemScr1);
            screenSubMenu.DropDownItems.Add(trayItemScr2);
            screenSubMenu.DropDownItems.Add(trayItemBoth);
            menu.Items.Add(screenSubMenu);

            menu.Items.Add("Pin / Unpin Expanded HUD", null, (s, e) => Dispatcher.Invoke(() => SetPinned(!IsPinned)));
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("Exit Glance", null, (s, e) => Dispatcher.Invoke(() => Application.Current.Shutdown()));

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
            try
            {
                if (activeSettingsWin != null && activeSettingsWin.IsLoaded)
                {
                    activeSettingsWin.Activate();
                    return;
                }
                activeSettingsWin = new SettingsWindow(PrimaryInstance ?? this);
                activeSettingsWin.Closed += (s, e) => activeSettingsWin = null;
                activeSettingsWin.Show();
                activeSettingsWin.Activate();
            }
            catch { }
        }

        public async void CheckForUpdatesInteractive(bool showUpToDateAlert)
        {
            try
            {
                var result = await GitHubUpdateService.CheckForUpdateAsync();
                if (result.IsUpdateAvailable && result.RemoteVersion != null)
                {
                    if (showUpToDateAlert || !GitHubUpdateService.IsBuildDismissed(result.RemoteVersion.BuildNumber))
                    {
                        UpdateNotificationWindow.ShowOrActivate(result.LocalVersion, result.RemoteVersion);
                    }
                }
                else if (showUpToDateAlert)
                {
                    if (!string.IsNullOrEmpty(result.ErrorMessage))
                    {
                        MessageBox.Show(
                            "Gagal memeriksa pembaruan dari GitHub:\n" + result.ErrorMessage,
                            "Glance Auto-Update",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            $"Anda sudah menggunakan versi Glance terbaru (v{result.LocalVersion?.Version ?? "1.1.0"}).",
                            "Glance Auto-Update",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                }
            }
            catch { }
        }
    }
}
