using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Glance
{
    /// <summary>
    /// Bagian partial MainWindow untuk pembacaan dan penyimpanan file JSON lokal
    /// (config.json, notes.json, clipboard.json, shelf.json).
    /// </summary>
    public partial class MainWindow
    {
        public void SaveAppConfig()
        {
            try
            {
                var cfg = new AppConfig
                {
                    backgroundAlpha = BackgroundAlpha,
                    isPinned = IsPinned,
                    displayMode = DisplayMode,

                    colorAccent = ColorAccent,
                    colorBattery = ColorBattery,
                    colorCpu = ColorCpu,
                    colorRam = ColorRam,
                    colorDisk = ColorDisk,
                    colorSound = ColorSound,
                    colorBrightness = ColorBrightness,
                    colorWifi = ColorWifi,

                    showSlideMedia = ShowSlideMedia,
                    showSlideSystem = ShowSlideSystem,
                    showSlideControl = ShowSlideControl,
                    showSlideClipboard = ShowSlideClipboard,
                    showSlideNotes = ShowSlideNotes,
                    showSlideShelf = ShowSlideShelf,

                    hotkeyModifiers = HotkeyModifiers,
                    hotkeyKey = HotkeyKey,
                    hotkeyDisplay = HotkeyDisplay,
                    isSearchInstalled = IsSearchInstalled,
                    isSearchEnabled = IsSearchEnabled,
                    enableSleepMode = EnableSleepMode,
                    sleepTimeoutSeconds = SleepTimeoutSeconds
                };
                File.WriteAllText(configFilePath, json.Serialize(cfg), Encoding.UTF8);
            }
            catch { }
        }

        private void LoadSavedData()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    var cfg = json.Deserialize<AppConfig>(File.ReadAllText(configFilePath, Encoding.UTF8));
                    if (cfg != null)
                    {
                        if (cfg.backgroundAlpha >= 60 && cfg.backgroundAlpha <= 255)
                        {
                            BackgroundAlpha = (byte)cfg.backgroundAlpha;
                        }
                        IsPinned = cfg.isPinned;
                        if (!string.IsNullOrEmpty(cfg.displayMode))
                        {
                            DisplayMode = cfg.displayMode;
                        }

                        if (!string.IsNullOrEmpty(cfg.colorAccent)) ColorAccent = cfg.colorAccent;
                        if (!string.IsNullOrEmpty(cfg.colorBattery)) ColorBattery = cfg.colorBattery;
                        if (!string.IsNullOrEmpty(cfg.colorCpu)) ColorCpu = cfg.colorCpu;
                        if (!string.IsNullOrEmpty(cfg.colorRam)) ColorRam = cfg.colorRam;
                        if (!string.IsNullOrEmpty(cfg.colorDisk)) ColorDisk = cfg.colorDisk;
                        if (!string.IsNullOrEmpty(cfg.colorSound)) ColorSound = cfg.colorSound;
                        if (!string.IsNullOrEmpty(cfg.colorBrightness)) ColorBrightness = cfg.colorBrightness;
                        if (!string.IsNullOrEmpty(cfg.colorWifi)) ColorWifi = cfg.colorWifi;

                        ShowSlideMedia = cfg.showSlideMedia;
                        ShowSlideSystem = cfg.showSlideSystem;
                        ShowSlideControl = cfg.showSlideControl;
                        ShowSlideClipboard = cfg.showSlideClipboard;
                        ShowSlideNotes = cfg.showSlideNotes;
                        ShowSlideShelf = cfg.showSlideShelf;

                        if (cfg.hotkeyModifiers != 0 && cfg.hotkeyKey != 0)
                        {
                            HotkeyModifiers = cfg.hotkeyModifiers;
                            HotkeyKey = cfg.hotkeyKey;
                            HotkeyDisplay = !string.IsNullOrEmpty(cfg.hotkeyDisplay) ? cfg.hotkeyDisplay : "Ctrl + Shift + P";
                        }

                        IsSearchInstalled = cfg.isSearchInstalled;
                        IsSearchEnabled = cfg.isSearchEnabled;

                        EnableSleepMode = cfg.enableSleepMode;
                        SleepTimeoutSeconds = (cfg.sleepTimeoutSeconds >= 2 && cfg.sleepTimeoutSeconds <= 120)
                            ? cfg.sleepTimeoutSeconds
                            : 5;
                    }
                }
            }
            catch { }

            try
            {
                if (File.Exists(notesFilePath))
                {
                    notesList = json.Deserialize<List<NoteItem>>(File.ReadAllText(notesFilePath, Encoding.UTF8)) ?? new List<NoteItem>();
                }
                else
                {
                    notesList.Add(new NoteItem
                    {
                        id = "n1",
                        text = "Welcome to Glance! Press " + HotkeyDisplay + " to launch Glance Search.",
                        done = false
                    });
                    SaveNotesData();
                }
            }
            catch { notesList = new List<NoteItem>(); }

            try
            {
                if (File.Exists(clipFilePath))
                {
                    clipList = json.Deserialize<List<ClipItem>>(File.ReadAllText(clipFilePath, Encoding.UTF8)) ?? new List<ClipItem>();
                }
            }
            catch { clipList = new List<ClipItem>(); }

            try
            {
                if (File.Exists(shelfFilePath))
                {
                    shelfList = json.Deserialize<List<ShelfItem>>(File.ReadAllText(shelfFilePath, Encoding.UTF8)) ?? new List<ShelfItem>();
                }
            }
            catch { shelfList = new List<ShelfItem>(); }
        }

        private void SaveNotesData()
        {
            try { File.WriteAllText(notesFilePath, json.Serialize(notesList), Encoding.UTF8); } catch { }
            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.notesList = new List<NoteItem>(notesList);
                CompanionInstance.RenderNotesList();
            }
            else if (IsCompanionWindow && PrimaryInstance != null)
            {
                PrimaryInstance.notesList = new List<NoteItem>(notesList);
                PrimaryInstance.RenderNotesList();
            }
        }

        private void SaveClipData()
        {
            try { File.WriteAllText(clipFilePath, json.Serialize(clipList.Take(25).ToList()), Encoding.UTF8); } catch { }
            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.clipList = new List<ClipItem>(clipList);
                CompanionInstance.RenderClipboardList();
            }
            else if (IsCompanionWindow && PrimaryInstance != null)
            {
                PrimaryInstance.clipList = new List<ClipItem>(clipList);
                PrimaryInstance.RenderClipboardList();
            }
        }

        private void SaveShelfData()
        {
            try { File.WriteAllText(shelfFilePath, json.Serialize(shelfList.Take(30).ToList()), Encoding.UTF8); } catch { }
            if (!IsCompanionWindow && CompanionInstance != null)
            {
                CompanionInstance.shelfList = new List<ShelfItem>(shelfList);
                CompanionInstance.RenderShelfList();
            }
            else if (IsCompanionWindow && PrimaryInstance != null)
            {
                PrimaryInstance.shelfList = new List<ShelfItem>(shelfList);
                PrimaryInstance.RenderShelfList();
            }
        }
    }
}
