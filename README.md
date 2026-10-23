# Glance 🏝️✨

> **An elegant, ultra-lightweight, native desktop companion & productivity HUD for Windows.**  
> *Everything you need, in a single glance.*

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?logo=windows&logoColor=white)](https://github.com/Fyxis/Glance)
[![Language](https://img.shields.io/badge/Language-C%23%20%7C%20WPF%20%7C%20Win32-239120?logo=csharp&logoColor=white)](https://github.com/Fyxis/Glance)
[![Memory Footprint](https://img.shields.io/badge/RAM%20Usage-~25--35%20MB-brightgreen)](#-performance--memory-footprint)
[![Framework](https://img.shields.io/badge/.NET-4.8%20Native-512BD4?logo=dotnet&logoColor=white)](https://github.com/Fyxis/Glance)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

---

**Glance** transforms your desktop experience by bringing a fluid, responsive, top-of-screen **Dynamic Island** and a macOS Spotlight-inspired **App Launcher** to Windows. 

Unlike heavy modern utilities wrapped in Electron or Chromium that devour hundreds of megabytes of RAM, Glance is built **100% native** in C# and WPF with direct Win32/WinRT integrations—delivering buttery-smooth 60 FPS animations while consuming a mere **15–35 MB of RAM**.

---

## 🚀 Key Features

### 🏝️ 1. Dynamic Island HUD
* **Ambient Top Notch**: Sits unobtrusively at the top-center of your monitor.
* **Fluid Expansion**: Expands into an interactive multi-slide control hub on hover or click with custom cubic-bezier spring easing.
* **Translucent Frosted Glass**: Modern acrylic glass styling with customizable opacity slider in Settings (`⚙️`).
* **Pin Mode**: Lock Glance in expanded view or let it auto-collapse gracefully when your mouse leaves.

### 🎵 2. Interactive Media Player & Live Waveform
* **Dynamic Waveform Animation**: Five responsive waveform bars that dance in real-time to your system audio peak level (`IAudioMeterInformation`).
* **Adaptive Color Palette**: Automatically extracts the vibrant dominant accent color from the active song's album art to dynamically tint the waveform bars.
* **Full Media Controls**: Play, pause, skip, rewind, and interactive seekbar with live timestamp tracking powered by Windows Universal Media Session (`GSMTC`).

### ⚡ 3. Real-Time Network Speed Monitor
* Displays live download (`↓`) and upload (`↑`) throughput directly on the collapsed pill and expanded system card.
* Adaptive units: automatically formats in `KB/s` and `MB/s` with smooth rolling averages.

### 🔍 4. Glance Search (`Ctrl + Shift + P`)
* **macOS Spotlight-style Launcher**: Centered keyboard-first search bar that appears instantly with `Ctrl + Shift + P`.
* **Frosted Glass Blur**: Blurs your desktop background into a silky-smooth Gaussian glass backdrop.
* **Instant App Search**: Fuzzy searches installed software from the Start Menu, Desktop, and Windows Registry App Paths.
* **Keyboard Navigation**: Navigate matches with `↑` / `↓` and press `Enter` to launch, or `ESC` to dismiss.

### 🎛️ 5. Control Center
* **Wi-Fi Monitor**: Displays connection status, active network SSID, and real-time signal strength via Native `wlanapi.dll`.
* **Master Volume Slider**: Smooth hardware volume slider with real-time level sync and mute toggle via Windows CoreAudio Endpoint.
* **Display Brightness**: Native WMI monitor brightness adjustment slider with smooth hardware stepping.

### 📊 6. System & Storage Monitor
* **Multi-Drive Storage Cards**: Individual split cards for each fixed drive (e.g. **Disk C:** and **Disk D:**) showing total capacity, used storage in GB, and circular visual percentage bars.
* **Hardware Telemetry**: Real-time CPU busy load percentage, physical RAM usage (used vs total GB), and battery charge level with charging status.

### 📋 7. Quick Notes & Clipboard Manager
* **Quick Scratchpad**: Write quick to-dos and thoughts directly on Glance without opening heavy text editors.
* **Clipboard History**: Automatically logs copied text snippets with timestamps, copy-back buttons, deletion, and pin-to-top favoriting.

---

## ⚡ Performance & Memory Footprint

Glance was engineered from the ground up for zero-overhead, always-on desktop usage.

| Application / Framework | Technology | Average RAM in Task Manager |
| :--- | :--- | :---: |
| **Glance** | **Native C# + WPF (Win32 / WinRT)** | **~25 – 35 MB** *(Peak < 48 MB)* ⚡ |
| Electron / Chromium Utilities | Node.js + Chromium Runtime | ~350 – 550+ MB |
| Web-Based Overlay Widgets | CEF / WebView2 | ~200 – 400 MB |

### How Glance stays under 35 MB:
1. **Automated Working Set Trimming**: Custom Win32 memory optimizer (`psapi.dll` + `kernel32.dll`) flushes inactive JIT buffers, driver startup pools, and font caches 2 seconds after launch, upon window collapse, upon search dismissal, and during 30s background idle cycles.
2. **Zero COM Leaks**: Strict lifetime caching and explicit `Marshal.ReleaseComObject()` management for CoreAudio and WMI endpoints.
3. **Optimized Screen Blurring**: 4× downscaled hardware-filtered screen capture for Glance Search, saving over 94% of offscreen render target memory compared to raw 4K frame buffers.
4. **Adaptive Bitmap Decoding**: Downscaled album art decoding (`DecodePixelWidth = 128`) prevents multi-megabyte bitmap heap bloat.

---

## ⌨️ Keyboard Shortcuts & Gestures

| Shortcut / Action | Function |
| :--- | :--- |
| **`Ctrl + Shift + P`** | Toggle **Glance Search** (Spotlight App Launcher) |
| **`ESC`** | Dismiss Search or collapse expanded view |
| **`↑` / `↓` + `Enter`** | Navigate and open application in Glance Search |
| **Hover on Top Notch** | Expand Glance into Control Center & HUD |
| **Click `⚙️` Icon** | Open Glance Settings (Transparency, Run at Startup, Pin) |
| **Right-click Tray Icon** | Access system tray quick menu and settings |

---

## 📦 Installation & Download

### Option 1: Standalone Installer (Recommended)
1. Download [**`GlanceSetup.exe`**](GlanceSetup.exe).
2. Run the installer.
3. Choose your installation folder (defaults to `%LOCALAPPDATA%\Programs\Glance`).
4. Keep **"Run at Startup Windows"** checked if you want Glance to start automatically when your PC boots.
5. Click **"Pasang Aplikasi (Install)"**.

### Option 2: Portable Executable
1. Download [**`Glance.exe`**](Glance.exe).
2. Place it anywhere on your machine and launch it directly.

---

## 🛠️ Building from Source

No heavy Visual Studio installation or third-party package managers are needed! Glance can be built in seconds using the Windows built-in C# compiler (`csc.exe`):

1. **Clone the repository:**
   ```powershell
   git clone https://github.com/Fyxis/Glance.git
   cd Glance
   ```

2. **Run the build script:**
   ```powershell
   powershell -ExecutionPolicy Bypass -File .\build.ps1
   ```

The script will compile:
* `Glance.exe` (Main application binary)
* `GlanceSetup.exe` (Self-contained standalone installer with embedded resource)

---

## 📂 Project Structure

```
Glance/
├── src/
│   ├── GlanceApp.cs        # Complete Glance Application (Window, HUD, Search, Hardware, Audio)
│   ├── SetupInstaller.cs   # Standalone Setup Installer with ShellLink & Registry Startup
│   └── app.ico             # Application Icon
├── build.ps1               # Automated build script using native Windows csc.exe
├── Glance.exe              # Precompiled portable executable (~105 KB)
├── GlanceSetup.exe         # Precompiled standalone installer (~128 KB)
├── .gitignore              # Git ignore rules
└── README.md               # Documentation
```

---

## 📄 License

This project is open-source and available under the [MIT License](LICENSE).

---

<p align="center">
  <b>Glance</b> — Crafted with precision for Windows.
</p>
