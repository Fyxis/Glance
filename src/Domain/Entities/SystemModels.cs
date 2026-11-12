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
    /// Entri aplikasi terindeks untuk pencarian instan pada Glance Search (Spotlight).
    /// </summary>
    public class AppSearchEntry
    {
        public string Name;
        public string LowerName;
        public string[] Words;
        public string LaunchPath;
        public string Subtitle;
        public ImageSource IconSource;
    }
}
