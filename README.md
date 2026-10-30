# Glance 🏝️✨

> **An elegant, ultra-lightweight, native desktop companion & productivity HUD for Windows.**  
> *Everything you need, in a single glance.*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?logo=windows&logoColor=white)](https://github.com/Fyxis/Glance)
[![Language](https://img.shields.io/badge/Language-C%23%20%7C%20WPF%20%7C%20Win32-239120?logo=csharp&logoColor=white)](https://github.com/Fyxis/Glance)
[![Memory Footprint](https://img.shields.io/badge/RAM%20Usage-~25--45%20MB-brightgreen)](#-performance--memory-footprint)
[![Framework](https://img.shields.io/badge/.NET-8.0%20Native-512BD4?logo=dotnet&logoColor=white)](https://github.com/Fyxis/Glance)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

**Glance** transforms your desktop experience by bringing a fluid, responsive, top-of-screen **Dynamic Island**, an authentic floating **macOS Dynamic Dock**, and a macOS Spotlight-inspired **App Launcher** to Windows 10 and Windows 11. 

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

### 🌊 2. macOS Dynamic Dock (Floating Taskbar)
* **Authentic Frosted Glass Pill**: Floats elegantly above the bottom of your screen with genuine macOS acrylic styling, drop shadow, and rounded pill curves.
* **Fluid Wave Magnification**: Smooth fish-eye magnification wave physics as your cursor sweeps across icons. The container pill dynamically adapts in height so icons remain perfectly framed within frosted glass.
* **Smart Running App Indicators**: Real-time glowing indicator dots underneath running applications, powered by accurate top-level window filtering (`EnumWindows`) that ignores background services and system desktop surfaces.
* **Intelligent Window Activation**: 
  - Clicking an open app icon (e.g., File Explorer, Spotify, Edge, Chrome) automatically restores and brings the existing window to the foreground (`SW_RESTORE` + `SetForegroundWindow` with thread input activation lock bypass).
  - **No duplicate windows opened by mistake!**
  - **Multi-Window Cycling**: If you have multiple windows open for the same app (such as two File Explorer folders), consecutive clicks seamlessly cycle through them.
  - **Force New Window**: Right-click any icon to select **"🗔 Open New Window"** or hold **Shift + Left Click** anytime you wish to launch a new instance.
* **Auto-Hide on Fullscreen Video & Gaming**: Automatically detects when an active window is in true Fullscreen mode (YouTube fullscreen in Chrome/Edge, Netflix, VLC, or fullscreen games) and smoothly slides the dock out of view. Restores instantly when exiting fullscreen.
* **Global Show/Hide Shortcut (`Alt + \``)**: Instantly toggle the taskbar on or off with `Alt + ` ` (customizable in the Settings dialog).

### 🎨 3. Full Dock & Icon Customizer
* **Visual Icon Picker**: Choose from over 20 tailored vector icons (Music Player, Spotify, Browser, Terminal, Store, Settings, Explorer, Code, Calculator, Notes, Chat, Mail, Camera, etc.).
* **Custom App & File Icons**: Set custom app paths and pick custom `.ico`, `.png`, or application `.exe` files for your dock items.
* **One-Click Presets**: Preconfigured launcher presets for Spotify, Edge, File Explorer, Terminal, Settings, Recycle Bin, and more.
* **Live Interactive Preview**: See changes update live in real-time before applying.
* **Reorder & Organize**: Add separators, move items left/right, rename labels, and configure custom process match filters.

### 🎵 4. Interactive Media Player & Synced Lyrics
* **Split 2-Column HUD Layout**:
  - **Left Column**: Album art, track title, artist, live seekbar with timestamps, and transport controls (Previous, Play/Pause, Next, and Lyrics toggle).
  - **Right Column (Calendar & Lyrics)**:
    - **Default Calendar**: Full day name, prominent date, month/year, and a 7-day week strip with today highlighted. Clicking directly launches Windows Clock & Calendar (`ms-clock:`).
    - **Synced Lyrics Mode (`♪`)**: Synchronized scrolling lyrics fetched automatically via LRCLIB. Click any lyric line to instantly seek playback.
    - **Center-Locked Highlighting**: The currently sung lyric line stays centered in view at all times. If you scroll to browse upcoming lyrics, Glance automatically snaps back to center when the next line begins.
* **Dynamic Waveform Animation**: Five responsive audio waveform bars that move dynamically based on live system audio peak levels (`IAudioMeterInformation`).
* **Adaptive Color Palette**: Automatically extracts the dominant accent color from active album art to dynamically tint controls and waveforms.

### 🔍 5. Glance Search (`Ctrl + Shift + P`)
* **macOS Spotlight-style Launcher**: Centered keyboard-first search bar that appears instantly with `Ctrl + Shift + P`.
* **Frosted Glass Blur**: Blurs your desktop background into a silky-smooth Gaussian glass backdrop.
* **Instant Fuzzy Search**: Rapidly finds applications across the Start Menu, Desktop, and Windows Registry App Paths.
* **Keyboard Navigation**: Navigate matches with `↑` / `↓` and press `Enter` to launch, or `ESC` to dismiss.

### 🎛️ 6. Control Center
* **Wi-Fi Telemetry**: Connection status, active SSID, and real-time signal strength via native `wlanapi.dll`.
* **Master Volume Slider**: Smooth hardware volume slider with real-time level sync and mute toggle via Windows CoreAudio Endpoint.
* **Display Brightness**: Native WMI monitor brightness adjustment slider with smooth hardware stepping.

### 📊 7. System & Storage Monitor
* **Multi-Drive Storage Cards**: Individual split cards for each fixed drive (e.g. **Disk C:** and **Disk D:**) showing total capacity, used storage in GB, and circular visual percentage bars.
* **Hardware Telemetry**: Real-time CPU busy load percentage, physical RAM usage (used vs total GB), and battery charge level with charging status.

### 📋 8. Quick Notes & Clipboard Manager
* **Quick Scratchpad**: Write quick to-dos and thoughts directly on Glance without opening heavy text editors.
* **Clipboard History**: Automatically logs copied text snippets with timestamps, copy-back buttons, deletion, and pin-to-top favoriting.

### 🖥️ 9. Multi-Monitor & Dual-Display Support
* **Primary Display Only**: Run Glance exclusively on your primary monitor.
* **Secondary Display Only**: Position Glance on your secondary monitor.
* **Both Displays**: Run synchronized dynamic islands on both screens simultaneously, sharing media playback, live waveform, and hardware telemetry with zero extra CPU/RAM overhead (~25–35 MB total).
* **Cursor-Aware Glance Search**: `Ctrl + Shift + P` automatically detects which monitor your cursor is currently active on and launches centered on that display.
* **Instant Switching**: Switch monitors on the fly anytime from Settings (`⚙️`) or the right-click System Tray menu.

### 📥 10. Drop Shelf / File Staging Zone
* **Drag-and-Drop to Notch**: Drag any file (images, documents, archives, videos) from your desktop or File Explorer directly onto the top notch. The notch illuminates green and instantly stages the file.
* **Direct Drag OUT**: Click and drag staged files directly from Glance into WhatsApp Web, Telegram, Discord, emails, web upload forms, or other folders.
* **Quick File Actions**: Open containing folder in Windows Explorer (`📂`), copy file paths, delete individual items (`✕`), and a one-click "Clear All" button.
* **Multi-Monitor Sync**: Files in the Drop Shelf automatically stay synchronized across both monitors.

### 🎨 11. Complete Customization & Shortcut Recorders
* **Modern Multi-Tab Settings Dialog (`⚙️`)**:
  - `⚙️ General`:
    - Run at Windows startup
    - Pin expanded HUD
    - Display selection (Primary / Secondary / Both)
    - Frosted glass opacity slider
    - Enable / Disable macOS Dynamic Dock
    - Auto-hide Dock on Fullscreen toggle
    - **Global Keyboard Shortcut Recorders**: Record custom key combinations for both **Glance Search** and **Taskbar Show/Hide** with instant live key capture and one-click reset buttons!
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
| **`Alt + \``** | Toggle **macOS Dynamic Dock** (Taskbar Show / Hide) |
| **`Ctrl + Shift + P`** | Toggle **Glance Search** (Spotlight App Launcher) |
| **`Shift + Click` (Dock Item)** | Force open a **New Window** / instance |
| **Right-Click (Dock Item)** | Access dock item menu (*Open New Window*, *Edit*, *Customize*, *Hide*, *Remove*) |
| **`ESC`** | Dismiss Search or collapse expanded notch HUD |
| **`↑` / `↓` + `Enter`** | Navigate and launch application in Glance Search |
| **Hover on Top Notch** | Expand Glance into Control Center & HUD |
| **Drag file to Notch** | Stage file in Drop Shelf |
| **Click `⚙️` Icon** | Open Glance Settings (Transparency, Run at Startup, Shortcuts, Colors) |
| **Right-click Tray Icon** | Access system tray quick menu and display selector |

*(Both `Alt + \`` and `Ctrl + Shift + P` can be customized to any key combination in Settings).*

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

## 📂 Project Structure

```
Glance/
├── src/
│   ├── GlanceApp.cs        # Complete Glance Application (Notch, Dock, Search, Customizer, Lyrics, Win32)
│   ├── SetupInstaller.cs   # Standalone Setup Installer with ShellLink & Registry Startup
│   └── app.ico             # Application Icon
├── build.ps1               # Automated .NET 8 build script
├── Glance.csproj           # Modern .NET 8 project file
├── GlanceSetup.csproj      # Installer project file
├── Glance.exe              # Precompiled portable executable (~38 MB self-contained)
├── GlanceSetup.exe         # Precompiled standalone installer (~38 MB self-contained)
├── .gitignore              # Git ignore rules
└── README.md               # Complete documentation
```

---

## 📄 License

This project is open-source and available under the [MIT License](LICENSE).

---

<p align="center">
  <b>Glance</b> — Crafted with precision for Windows.
</p>
