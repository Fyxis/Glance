using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Glance
{
    /// <summary>
    /// Layanan infrastruktur untuk mengecek, mengunduh, dan memasang pembaruan Glance langsung dari GitHub.
    /// </summary>
    public static class GitHubUpdateService
    {
        private const string RemoteManifestUrl = "https://raw.githubusercontent.com/Fyxis/Glance/main/version.json";
        private const string DefaultBinaryUrl = "https://raw.githubusercontent.com/Fyxis/Glance/main/Glance.exe";

        private static readonly HttpClient httpClient = CreateHttpClient();
        private static readonly JsonSerializerOptions jsonOpts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private static long dismissedBuildNumber = -1;

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Glance-Updater/1.1 (Windows NT 10.0; Win64; x64)");
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
            return client;
        }

        /// <summary>
        /// Membaca informasi versi lokal yang tertanam di dalam executable Glance.exe.
        /// </summary>
        public static AppVersionManifest GetLocalVersion()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream stream = asm.GetManifestResourceStream("version.json"))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                        {
                            string json = reader.ReadToEnd();
                            var manifest = JsonSerializer.Deserialize<AppVersionManifest>(json, jsonOpts);
                            if (manifest != null) return manifest;
                        }
                    }
                }
            }
            catch { }

            try
            {
                string localFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.json");
                if (File.Exists(localFile))
                {
                    string json = File.ReadAllText(localFile, Encoding.UTF8);
                    var manifest = JsonSerializer.Deserialize<AppVersionManifest>(json, jsonOpts);
                    if (manifest != null) return manifest;
                }
            }
            catch { }

            return new AppVersionManifest
            {
                Version = "1.0.0",
                BuildNumber = 0
            };
        }

        /// <summary>
        /// Menandai build tertentu agar tidak terus memunculkan pop-up otomatis selama sesi berjalan jika user klik Cancel.
        /// </summary>
        public static void DismissBuildForSession(long buildNumber)
        {
            dismissedBuildNumber = buildNumber;
        }

        public static bool IsBuildDismissed(long buildNumber)
        {
            return buildNumber > 0 && dismissedBuildNumber == buildNumber;
        }

        /// <summary>
        /// Mengecek ke GitHub apakah ada versi baru yang tersedia.
        /// </summary>
        public static async Task<UpdateCheckResult> CheckForUpdateAsync()
        {
            var local = GetLocalVersion();
            try
            {
                string url = RemoteManifestUrl + "?t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                string json = await httpClient.GetStringAsync(url);
                var remote = JsonSerializer.Deserialize<AppVersionManifest>(json, jsonOpts);
                if (remote == null)
                {
                    return new UpdateCheckResult
                    {
                        IsUpdateAvailable = false,
                        LocalVersion = local,
                        ErrorMessage = "Gagal membaca manifest versi dari GitHub."
                    };
                }

                if (string.IsNullOrWhiteSpace(remote.DownloadUrl))
                {
                    remote.DownloadUrl = DefaultBinaryUrl;
                }

                bool isNewer = IsRemoteNewer(local, remote);
                return new UpdateCheckResult
                {
                    IsUpdateAvailable = isNewer,
                    LocalVersion = local,
                    RemoteVersion = remote
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    IsUpdateAvailable = false,
                    LocalVersion = local,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static bool IsRemoteNewer(AppVersionManifest local, AppVersionManifest remote)
        {
            if (remote == null) return false;

            if (remote.BuildNumber > 0 && local.BuildNumber > 0)
            {
                if (remote.BuildNumber > local.BuildNumber) return true;
            }

            if (Version.TryParse(CleanVersionString(local.Version), out Version vLocal) &&
                Version.TryParse(CleanVersionString(remote.Version), out Version vRemote))
            {
                if (vRemote > vLocal) return true;
            }

            return false;
        }

        private static string CleanVersionString(string ver)
        {
            if (string.IsNullOrWhiteSpace(ver)) return "1.0.0";
            ver = ver.Trim();
            if (ver.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                ver = ver.Substring(1).Trim();
            }
            return ver;
        }

        /// <summary>
        /// Mengunduh Glance.exe terbaru dari GitHub, menimpa aplikasi saat ini, dan me-restart Glance.
        /// </summary>
        public static async Task DownloadAndApplyUpdateAsync(
            AppVersionManifest remote,
            Action<int> onProgress,
            CancellationToken cancellationToken = default)
        {
            string downloadUrl = !string.IsNullOrWhiteSpace(remote?.DownloadUrl)
                ? remote.DownloadUrl
                : DefaultBinaryUrl;
            downloadUrl += (downloadUrl.Contains("?") ? "&" : "?") + "t=" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string tempUpdateDir = Path.Combine(Path.GetTempPath(), "GlanceUpdate");
            Directory.CreateDirectory(tempUpdateDir);
            string tempNewExe = Path.Combine(tempUpdateDir, "Glance_new.exe");

            using (var downloadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            {
                downloadClient.DefaultRequestHeaders.UserAgent.ParseAdd("Glance-Updater/1.1");
                downloadClient.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

                using (var response = await downloadClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    long? totalBytes = response.Content.Headers.ContentLength;

                    using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken))
                    using (var fileStream = new FileStream(tempNewExe, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                    {
                        byte[] buffer = new byte[81920];
                        long totalRead = 0;
                        int bytesRead;
                        int lastPct = -1;

                        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                            totalRead += bytesRead;

                            if (totalBytes.HasValue && totalBytes.Value > 0)
                            {
                                int pct = (int)Math.Clamp(Math.Round(totalRead * 100.0 / totalBytes.Value), 0, 100);
                                if (pct != lastPct)
                                {
                                    lastPct = pct;
                                    onProgress?.Invoke(pct);
                                }
                            }
                        }
                    }
                }
            }

            // Validasi file hasil unduhan adalah file PE executable Windows yang valid ('MZ')
            FileInfo fi = new FileInfo(tempNewExe);
            if (!fi.Exists || fi.Length < 50000)
            {
                throw new InvalidDataException("File pembaruan yang diunduh tidak lengkap atau rusak.");
            }

            byte[] header = new byte[2];
            using (FileStream fs = new FileStream(tempNewExe, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (fs.Read(header, 0, 2) < 2 || header[0] != 0x4D || header[1] != 0x5A)
                {
                    throw new InvalidDataException("File yang diunduh bukan executable Windows yang valid.");
                }
            }

            onProgress?.Invoke(100);

            string currentExePath = Process.GetCurrentProcess().MainModule.FileName;
            string currentDir = Path.GetDirectoryName(currentExePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            int currentPid = Process.GetCurrentProcess().Id;

            string installedDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Glance");
            string installedExePath = Path.Combine(installedDir, "Glance.exe");

            // Buat script batch updater di %TEMP% untuk mengganti binary setelah proses keluar dan langsung me-restart Glance
            string cmdPath = Path.Combine(tempUpdateDir, "apply_update.cmd");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("@echo off");
            sb.AppendLine("timeout /t 1 /nobreak >nul");
            sb.AppendLine($"taskkill /PID {currentPid} /F >nul 2>&1");
            sb.AppendLine("timeout /t 1 /nobreak >nul");
            sb.AppendLine($"copy /Y \"{tempNewExe}\" \"{currentExePath}\" >nul 2>&1");
            if (File.Exists(installedExePath) && !string.Equals(currentExePath, installedExePath, StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"copy /Y \"{tempNewExe}\" \"{installedExePath}\" >nul 2>&1");
            }
            sb.AppendLine($"del /F /Q \"{tempNewExe}\" >nul 2>&1");
            sb.AppendLine($"start \"\" /D \"{currentDir}\" \"{currentExePath}\"");
            sb.AppendLine("del \"%~f0\" >nul 2>&1");

            File.WriteAllText(cmdPath, sb.ToString(), Encoding.Default);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C \"" + cmdPath + "\"",
                WorkingDirectory = tempUpdateDir,
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            Application.Current.Dispatcher.Invoke(() =>
            {
                Application.Current.Shutdown();
            });
        }
    }
}
