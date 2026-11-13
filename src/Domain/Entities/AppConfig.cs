using System;

namespace Glance
{
    /// <summary>
    /// Menyimpan seluruh konfigurasi pengguna yang disimpan ke dalam file config.json.
    /// </summary>
    public class AppConfig
    {
        public int backgroundAlpha { get; set; }
        public bool isPinned { get; set; }
        public string displayMode { get; set; }

        public string colorAccent { get; set; }
        public string colorBattery { get; set; }
        public string colorCpu { get; set; }
        public string colorRam { get; set; }
        public string colorDisk { get; set; }
        public string colorSound { get; set; }
        public string colorBrightness { get; set; }
        public string colorWifi { get; set; }

        public bool showSlideMedia { get; set; }
        public bool showSlideSystem { get; set; }
        public bool showSlideControl { get; set; }
        public bool showSlideClipboard { get; set; }
        public bool showSlideNotes { get; set; }
        public bool showSlideShelf { get; set; }

        public uint hotkeyModifiers { get; set; }
        public uint hotkeyKey { get; set; }
        public string hotkeyDisplay { get; set; }

        public bool isSearchInstalled { get; set; } = true;
        public bool isSearchEnabled { get; set; } = true;

        public bool enableSleepMode { get; set; } = true;
        public int sleepTimeoutSeconds { get; set; } = 5;

        public AppConfig()
        {
            backgroundAlpha = 175;
            displayMode = "screen1";
            colorAccent = "#1CE755";
            colorBattery = "#1CE755";
            colorCpu = "#38BDF8";
            colorRam = "#C084FC";
            colorDisk = "#34D399";
            colorSound = "#38BDF8";
            colorBrightness = "#FBBF24";
            colorWifi = "#10B981";
            showSlideMedia = true;
            showSlideSystem = true;
            showSlideControl = true;
            showSlideClipboard = true;
            showSlideNotes = true;
            showSlideShelf = true;

            hotkeyModifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT;
            hotkeyKey = NativeMethods.VK_P;
            hotkeyDisplay = "Ctrl + Shift + P";

            isSearchInstalled = true;
            isSearchEnabled = true;

            enableSleepMode = true;
            sleepTimeoutSeconds = 5;
        }
    }
}
