# Glance 🏝️✨

> **An elegant, ultra-lightweight, native desktop companion & productivity HUD for Windows.**  
> *Everything you need, in a single glance.*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?logo=windows&logoColor=white)](https://github.com/Fyxis/Glance)
[![Language](https://img.shields.io/badge/Language-C%23%20%7C%20WPF%20%7C%20Win32-239120?logo=csharp&logoColor=white)](https://github.com/Fyxis/Glance)
[![Memory Footprint](https://img.shields.io/badge/RAM%20Usage-~25--45%20MB-brightgreen)](#-performance--memory-footprint)
[![Framework](https://img.shields.io/badge/.NET-8.0%20Native-512BD4?logo=dotnet&logoColor=white)](https://github.com/Fyxis/Glance)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

**Glance** transforms your desktop experience by bringing a fluid, responsive, top-of-screen **Dynamic Island** and a macOS Spotlight-inspired **App Launcher** to Windows 10 and Windows 11. 

Unlike heavy modern utilities wrapped in Electron or Chromium that devour hundreds of megabytes of RAM, Glance is built **100% native** in C# and WPF on **.NET 8** with direct Win32/WinRT integrations—delivering buttery-smooth 60 FPS animations while consuming a mere **25–45 MB of RAM**.

---

## 🚀 Key Features

### 🏝️ 1. Dynamic Island HUD
* **Ambient Top Notch**: Sits unobtrusively at the top-center of your monitor with a balanced 3-column design:
  - **Left**: Battery (circular progress arc ring with charging indicator ⚡)
  - **Center**: Clock (bold real-time clock)
  - **Right**: Wi-Fi throughput speed (live Download `↓` & Upload `↑`)
* **Fluid Expansion**: Expands into an interactive 6-slide control hub on hover or click with custom cubic-bezier spring easing.
* **Translucent Frosted Glass**: Modern acrylic glass styling with customizable opacity slider in Settings (`⚙️`).
* **Pin Mode**: Lock Glance in expanded view or let it auto-collapse gracefully when your mouse leaves.

### 🎵 2. Interactive Media Player & Synced Lyrics
* **Split 2-Column HUD Layout**:
  - **Left Column**: Album art, track title, artist, live seekbar with timestamps, and transport controls (Previous, Play/Pause, Next, and Lyrics toggle).
  - **Right Column (Calendar & Lyrics)**:
    - **Default Calendar**: Full day name, prominent date, month/year, and a 7-day week strip with today highlighted. Clicking directly launches Windows Clock & Calendar (`ms-clock:`).
    - **Synced Lyrics Mode (`♪`)**: Synchronized scrolling lyrics fetched automatically via LRCLIB with duration-aware track matching. Click any lyric line to instantly seek playback.
    - **Center-Locked Highlighting**: The currently sung lyric line stays centered in view at all times. If you scroll to browse upcoming lyrics, Glance automatically snaps back to center when the next line begins.
* **Dynamic Waveform Animation**: Five responsive audio waveform bars that move dynamically based on live system audio peak levels (`IAudioMeterInformation`).
* **Adaptive Color Palette**: Automatically extracts the dominant accent color from active album art to dynamically tint controls and waveforms.

### 🔍 3. Glance Search (`Ctrl + Shift + P`)
* **macOS Spotlight-style Launcher**: Centered keyboard-first search bar that appears instantly with `Ctrl + Shift + P`.
* **Frosted Glass Blur**: Blurs your desktop background into a silky-smooth Gaussian glass backdrop.
* **Instant Fuzzy Search**: Rapidly finds applications across the Start Menu, Desktop, and Windows Registry App Paths.
* **Keyboard Navigation**: Navigate matches with `↑` / `↓` and press `Enter` to launch, or `ESC` to dismiss.

### 🎛️ 4. Control Center
* **Wi-Fi Telemetry**: Connection status, active SSID, and real-time signal strength via native `wlanapi.dll`.
* **Master Volume Slider**: Smooth hardware volume slider with real-time level sync and mute toggle via Windows CoreAudio Endpoint.
* **Display Brightness**: Native WMI monitor brightness adjustment slider with smooth hardware stepping.

### 📊 5. System & Storage Monitor
* **Multi-Drive Storage Cards**: Individual split cards for each fixed drive (e.g. **Disk C:** and **Disk D:**) showing total capacity, used storage in GB, and circular visual percentage bars.
* **Hardware Telemetry**: Real-time CPU busy load percentage, physical RAM usage (used vs total GB), and battery charge level with charging status.

### 📋 6. Quick Notes & Clipboard Manager
* **Quick Scratchpad**: Write quick to-dos and thoughts directly on Glance without opening heavy text editors.
* **Clipboard History**: Automatically logs copied text snippets with timestamps, copy-back buttons, deletion, and pin-to-top favoriting.

### 🖥️ 7. Multi-Monitor & Dual-Display Support
* **Primary Display Only**: Run Glance exclusively on your primary monitor.
* **Secondary Display Only**: Position Glance on your secondary monitor.
* **Both Displays**: Run synchronized dynamic islands on both screens simultaneously, sharing media playback, live waveform, and hardware telemetry with zero extra CPU/RAM overhead (~25–35 MB total).
* **Cursor-Aware Glance Search**: `Ctrl + Shift + P` automatically detects which monitor your cursor is currently active on and launches centered on that display.
* **Instant Switching**: Switch monitors on the fly anytime from Settings (`⚙️`) or the right-click System Tray menu.

### 📥 8. Drop Shelf / File Staging Zone
* **Drag-and-Drop to Notch**: Drag any file (images, documents, archives, videos) from your desktop or File Explorer directly onto the top notch. The notch illuminates green and instantly stages the file.
* **Direct Drag OUT**: Click and drag staged files directly from Glance into WhatsApp Web, Telegram, Discord, emails, web upload forms, or other folders.
* **Quick File Actions**: Open containing folder in Windows Explorer (`📂`), copy file paths, delete individual items (`✕`), and a one-click "Clear All" button.
* **Multi-Monitor Sync**: Files in the Drop Shelf automatically stay synchronized across both monitors.

### 🎨 9. Complete Customization & Shortcut Recorder
* **Modern Multi-Tab Settings Dialog (`⚙️`)**:
  - `⚙️ General`:
    - Run at Windows startup
    - Pin expanded HUD
    - Display selection (Primary / Secondary / Both)
    - Frosted glass opacity slider
    - **Global Keyboard Shortcut Recorder**: Record custom key combinations for **Glance Search** with instant live key capture and one-click reset button!
  - `🎨 Color Palette`: Independent color personalization for each element:
    - Main Accent, Battery, Processor (CPU), Memory (RAM), Storage (Drives), Master Volume, Brightness, and Wi-Fi & Speed meters.
    - Includes **8 instant preset swatches** + **custom HEX color input** + **live color preview**.
  - `📑 Manage Slides`: Select which slides to include or exclude from the notch carousel.

---

## ⚡ Performance & Memory Footprint

Glance was engineered from the ground up for zero-overhead, always-on desktop usage.

| Application / Framework | Technology | Average RAM in Task Manager |
| :--- | :--- | :---: |
| **Glance** | **Native .NET 8 + WPF (Win32 / WinRT)** | **~25 – 45 MB** *(Peak < 50 MB)* ⚡ |
| Electron / Chromium Utilities | Node.js + Chromium Runtime | ~350 – 550+ MB |
| Web-Based Overlay Widgets | CEF / WebView2 | ~200 – 400 MB |

### How Glance stays ultra-lightweight:
1. **Automated Working Set Trimming**: Custom Win32 memory optimizer (`psapi.dll` + `kernel32.dll`) flushes inactive JIT buffers, driver startup pools, and font caches upon window collapse, search dismissal, and periodic idle cycles.
2. **Zero COM Leaks**: Strict lifetime caching and explicit `Marshal.ReleaseComObject()` management for CoreAudio and WMI endpoints.
3. **Optimized Screen Blurring**: 4× downscaled hardware-filtered screen capture for Glance Search, saving over 94% of offscreen render target memory compared to raw 4K frame buffers.
4. **Adaptive Bitmap Decoding**: Downscaled album art decoding (`DecodePixelWidth = 128`) prevents multi-megabyte bitmap heap bloat.

---

## ⌨️ Keyboard Shortcuts & Gestures

| Shortcut / Action | Function |
| :--- | :--- |
| **`Ctrl + Shift + P`** | Toggle **Glance Search** (Spotlight App Launcher) |
| **`ESC`** | Dismiss Search or collapse expanded notch HUD |
| **`↑` / `↓` + `Enter`** | Navigate and launch application in Glance Search |
| **Hover on Top Notch** | Expand Glance into Control Center & HUD |
| **Drag file to Notch** | Stage file in Drop Shelf |
| **Click `⚙️` Icon** | Open Glance Settings (Transparency, Run at Startup, Shortcuts, Colors) |
| **Right-click Tray Icon** | Access system tray quick menu and display selector |

*(`Ctrl + Shift + P` can be customized to any key combination in Settings).*

---

## 📦 Installation & Download

### Option 1: Standalone Installer (Recommended)
1. Download [**`GlanceSetup.exe`**](GlanceSetup.exe).
2. Run the installer.
3. Choose your installation folder (defaults to `%LOCALAPPDATA%\Programs\Glance`).
4. Keep **"Run at Startup Windows"** checked if you want Glance to start automatically when your PC boots.
5. Click **"Install Now"**.

### Option 2: Portable Executable
1. Download [**`Glance.exe`**](Glance.exe).
2. Place it anywhere on your machine and launch it directly.

---

## 🛠️ Building from Source

Glance can be compiled directly using the **.NET 8 SDK**:

1. **Clone the repository:**
   ```powershell
   git clone https://github.com/Fyxis/Glance.git
   cd Glance
   ```

2. **Run the automated build script:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\build.ps1
   ```

The script will build and publish:
* `Glance.exe` (Single-file native .NET 8 binary)
* `GlanceSetup.exe` (Standalone self-contained installer)

---

## 📂 Project Structure (Clean Architecture)

Glance is organized using **Clean Architecture** principles to separate domain models, application business logic, OS/hardware infrastructure, and WPF presentation views:

```text
Glance/
├── src/
│   ├── Domain/                         # 1. Core Domain Layer (Zero external dependencies)
│   │   └── Entities/
│   │       ├── AppConfig.cs            # Application configuration & color/hotkey preferences
│   │       ├── MediaModels.cs          # LyricLine, LrcSearchItem, MediaSnapshot
│   │       ├── ProductivityModels.cs   # NoteItem, ClipItem, ShelfItem
│   │       └── SystemModels.cs         # DriveStatInfo, AppSearchEntry
│   │
│   ├── Application/                    # 2. Application Layer (Business logic & services)
│   │   └── Services/
│   │       └── LyricsScoringService.cs # Track title/artist normalization, candidate scoring & LRC parser
│   │
│   ├── Infrastructure/                 # 3. Infrastructure Layer (Win32, Hardware, APIs & Persistence)
│   │   ├── ExternalApis/
│   │   │   └── LrcLibApiClient.cs      # LRCLIB REST API client for synchronized lyrics
│   │   ├── Hardware/
│   │   │   ├── AudioComInterfaces.cs   # CoreAudio COM interfaces (IAudioEndpointVolume, IAudioMeterInformation)
│   │   │   ├── HardwareController.cs   # Volume, Mute, WMI Brightness, CPU, RAM, Disk & Wi-Fi telemetry
│   │   │   └── NetworkSpeedMonitor.cs  # Live network interface download/upload throughput monitor
│   │   ├── Persistence/
│   │   │   ├── JsonHelper.cs           # JSON serialization helper
│   │   │   └── StartupManager.cs       # Windows Registry CurrentVersion\Run startup manager
│   │   └── Win32/
│   │       ├── MemoryOptimizer.cs      # Native working-set memory trimmer (psapi.dll / kernel32.dll)
│   │       └── NativeMethods.cs        # Win32 P/Invoke declarations (user32, dwmapi, gdi32, wlanapi)
│   │
│   ├── Presentation/                   # 4. Presentation Layer (WPF Windows, HUD & Carousel Slides)
│   │   ├── Notch/
│   │   │   ├── MainWindow.cs               # Core Dynamic Island window, state & lifecycle
│   │   │   ├── MainWindow.Collapsed.cs     # Collapsed ambient pill UI (Battery, Clock, Media/Net)
│   │   │   ├── MainWindow.Expanded.cs      # Expanded HUD shell, header tabs & slide carousel
│   │   │   ├── MainWindow.Telemetry.cs     # Media GSMTC polling, waveform & hardware timers
│   │   │   ├── MainWindow.Persistence.cs   # Config/notes/clipboard/shelf load & save orchestration
│   │   │   └── Slides/
│   │   │       ├── MainWindow.SlideMedia.cs     # Slide 0: Media Player, Calendar & Synced Lyrics
│   │   │       ├── MainWindow.SlideSystem.cs    # Slide 1: CPU, RAM, Battery & Multi-Drive Storage
│   │   │       ├── MainWindow.SlideControl.cs   # Slide 2: Volume, Brightness & Wi-Fi Control Center
│   │   │       ├── MainWindow.SlideClipboard.cs # Slide 3: Clipboard History Manager
│   │   │       ├── MainWindow.SlideNotes.cs     # Slide 4: Quick Scratchpad Notes
│   │   │       └── MainWindow.SlideShelf.cs     # Slide 5: Drag-and-Drop File Staging Shelf
│   │   ├── Search/
│   │   │   └── SpotlightWindow.cs      # Glance Search (macOS Spotlight-style app launcher)
│   │   └── Settings/
│   │       └── SettingsWindow.cs       # Multi-tab Settings & Color/Hotkey Customizer dialog
│   │
│   ├── Installer/                      # Standalone Setup Installer project files
│   │   └── SetupInstaller.cs           # Setup GUI, ShellLink shortcut creator & Uninstaller
│   │
│   ├── Program.cs                      # Application entry point & single-instance guard
│   └── app.ico                         # Application icon
│
├── build.ps1                           # Automated .NET 8 build & publish script
├── Glance.csproj                       # Main Glance .NET 8 project file
├── GlanceSetup.csproj                  # Standalone Installer project file
├── Glance.exe                          # Precompiled portable executable
├── GlanceSetup.exe                     # Precompiled standalone installer
└── README.md                           # Documentation
```

---

## 📄 License

This project is open-source and available under the [MIT License](LICENSE).

---

<p align="center">
  <b>Glance</b> — Crafted with precision for Windows.
</p>
