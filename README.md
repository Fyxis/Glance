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
* **Ambient Top Notch**: Sits unobtrusively at the top-center of your monitor with a balanced 3-column design:
  - **Kiri**: Baterai (circular progress arc ring dengan indikator charging ⚡)
  - **Tengah**: Jam (bold real-time clock)
  - **Kanan**: Kecepatan Internet Wi-Fi (Download `↓` & Upload `↑` real-time)
* **Fluid Expansion**: Expands into an interactive 6-slide control hub on hover or click with custom cubic-bezier spring easing.
* **Translucent Frosted Glass**: Modern acrylic glass styling with customizable opacity slider in Settings (`⚙️`).
* **Pin Mode**: Lock Glance in expanded view or let it auto-collapse gracefully when your mouse leaves.

### 🎵 2. Interactive Media Player, Dual-Column HUD & Synced Lyrics
* **Split 2-Column Slide Layout**: Slide pertama kini dibagi menjadi dua kolom elegan berdampingan:
  - **Kolom Kiri (Media Player)**: Cover Album (46×46), judul lagu, artis, timeline seekbar interaktif dengan live timestamp, serta tombol kontrol lengkap (Previous, Play/Pause circular button, Next, dan tombol toggle lirik). Kontrol musik tetap aktif dan bisa diakses kapan saja.
  - **Kolom Kanan (Widget Kalender & Lirik Lagu Interaktif)**:
    - **Default (Widget Kalender Estetik)**: Menampilkan nama hari (misal `JUMAT`), tanggal berukuran besar, bulan & tahun (`Oktober 2026`), serta 7-day mini week strip (Senin–Minggu) dengan hari ini disorot pill hijau zamrud. Klik pada widget kalender untuk langsung membuka Jam / Kalender Windows (`ms-clock:`).
    - **Mode Lirik Lagu (`♪`)**: Klik tombol lirik pada kontrol media untuk mengubah kolom kanan menjadi tampilan lirik lagu yang tersinkronisasi (*synced live scrolling lyrics*) secara otomatis via API LRCLIB. Klik lirik baris mana saja untuk melompat (*seek*) ke bagian lagu tersebut.
* **Dynamic Waveform Animation**: Lima batang gelombang audio (*waveform bars*) yang bergerak responsif mengikuti peak level suara sistem (`IAudioMeterInformation`) saat notch dalam mode collapsed.
* **Adaptive Color Palette**: Mengekstrak warna aksen dominan dari cover album lagu yang sedang diputar untuk mewarnai bar waveform secara dinamis.

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

### 🖥️ 8. Multi-Monitor & Dual-Display Support
* **Layar 1 Saja (Primary Display)**: Run Glance on your main monitor.
* **Layar 2 Saja (Secondary Display)**: Move Glance to your secondary monitor.
* **Kedua Layar (Both Screens)**: Run synchronized dynamic islands on both monitors simultaneously, sharing media playback, live waveform, and hardware telemetry with zero extra CPU/RAM overhead (~20 MB total).
* **Cursor-Aware Glance Search**: `Ctrl + Shift + P` automatically detects which monitor your cursor is currently active on and launches centered on that display.
* **Instant Switching**: Switch monitors on the fly anytime from Settings (`⚙️`) or the right-click System Tray menu.

### 📥 9. Drop Shelf / File Staging Zone
* **Drag-and-Drop ke Notch**: Tarik file apa saja (gambar, dokumen, zip, video) dari desktop atau File Explorer langsung ke notch atas. Notch akan menyala hijau dan menampung file secara instan.
* **Drag OUT Langsung**: Klik dan seret (drag) file apa saja dari Shelf langsung ke WhatsApp Web, Telegram, Discord, email, browser upload form, atau folder lain.
* **Quick File Actions**: Buka lokasi file di Windows Explorer (`📂`), salin path file, hapus per item (`✕`), dan tombol "Bersihkan Semua".
* **Multi-Monitor Sync**: File di Shelf tersinkronisasi otomatis di monitor 1 dan monitor 2.

### 🎨 10. Kustomisasi Penuh: Warna per Area & Manajemen Slide
* **Dialog Pengaturan Multi-Tab Modern (`⚙️`)**:
  - `⚙️ Umum`: Startup otomatis Windows, Pin notch agar tetap terbuka, pemilihan monitor (Layar 1 / Layar 2 / Kedua Layar), slider opasitas frosted glass, dan panduan shortcut Glance Search.
  - `🎨 Kustom Warna`: Personalisasi warna independen untuk setiap elemen/area tanpa mempengaruhi area lain:
    - **Tema Utama (Main Accent)**: Tombol aksi, glow, dan titik pagination.
    - **Baterai**: Cincin circular arc pada notch dan bar status baterai di system card.
    - **Processor (CPU)**: Bar telemetri beban kerja CPU.
    - **Memory (RAM)**: Bar kapasitas pemakaian RAM.
    - **Penyimpanan (Disk)**: Bar kapasitas partisi drive (Drive C, D, dst).
    - **Volume Suara**: Slider dan pill master audio.
    - **Kecerahan Layar**: Slider hardware brightness.
    - **Wi-Fi & Speed**: Pill status koneksi dan indikator kecepatan internet.
    - Dilengkapi **8 preset swatch instan** + **input kode HEX custom** + **live preview swatch** + **tombol reset per area**.
  - `📑 Kelola Slide`: Centang slide mana saja yang ingin diaktifkan atau dinonaktifkan:
    - 🎵 *Media Player & Kalender*
    - 📊 *Sistem & Disk Telemetri*
    - 🎛️ *Pusat Kontrol (Volume, Brightness, Wi-Fi)*
    - 📋 *Riwayat Clipboard*
    - 📝 *Catatan Cepat (Quick Notes)*
    - 📥 *Drop Shelf (File Staging)*
    - *Jika slide dimatikan (misal Catatan Cepat), slide tersebut otomatis dieliminasi dari rotasi carousel, titik navigasi (dots) dibuat ulang secara dinamis sesuai jumlah slide aktif, dan tombol `<` / `>` langsung melompati slide yang dinonaktifkan.*
* **Penerapan Langsung (Live Instant Sync)**: Perubahan warna dan susunan slide langsung aktif seketika pada kedua monitor saat tombol "Simpan Pengaturan" ditekan tanpa perlu me-restart aplikasi.

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
