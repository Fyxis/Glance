using System;

namespace Glance
{
    /// <summary>
    /// Manifest informasi versi aplikasi untuk pengecekan Auto-Update via GitHub.
    /// </summary>
    public class AppVersionManifest
    {
        public string Version { get; set; } = "1.0.0";
        public long BuildNumber { get; set; } = 0;
        public string PublishedAt { get; set; } = "";
        public string DownloadUrl { get; set; } = "https://raw.githubusercontent.com/Fyxis/Glance/main/Glance.exe";
        public string ReleaseNotes { get; set; } = "";
    }

    /// <summary>
    /// Hasil pengecekan pembaruan dari GitHub.
    /// </summary>
    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public AppVersionManifest LocalVersion { get; set; }
        public AppVersionManifest RemoteVersion { get; set; }
        public string ErrorMessage { get; set; }
    }
}
