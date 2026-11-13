using System;
using System.Windows.Media;

namespace Glance
{
    /// <summary>
    /// Informasi kapasitas dan pemakaian partisi penyimpanan (C:, D:, dst.) pada Slide 1.
    /// </summary>
    public class DriveStatInfo
    {
        public string Name = "";
        public int Percent = 0;
        public double UsedGB = 0;
        public double TotalGB = 0;
    }

    /// <summary>
    /// Entri hasil pencarian multifungsi pada Glance Search (Aplikasi, Kalkulator, Perintah Sistem, & Web Search).
    /// </summary>
    public class AppSearchEntry
    {
        public string Name;
        public string LowerName;
        public string[] Words;
        public string LaunchPath;
        public string Subtitle;
        public ImageSource IconSource;

        /// <summary>
        /// Jenis item: "app", "calc", "command", atau "web".
        /// </summary>
        public string EntryKind = "app";
        public string EmojiIcon = "";
        public string BadgeColorHex = "#1CE755";
        public string ActionHint = "Open ↵";
        public string CopyText = "";
        public string CommandId = "";
    }
}
