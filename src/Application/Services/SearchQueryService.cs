using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Glance
{
    /// <summary>
    /// Layanan logika aplikasi untuk Glance Search: Kalkulator Instan, Perintah Sistem (System Commands),
    /// dan Pencarian Web Cepat (Google, YouTube, GitHub, ChatGPT).
    /// </summary>
    public static class SearchQueryService
    {
        private static readonly List<AppSearchEntry> systemCommands = BuildSystemCommands();

        private static List<AppSearchEntry> BuildSystemCommands()
        {
            var list = new List<AppSearchEntry>
            {
                CreateCmd("cmd_settings", "Glance Settings & Customizer", "Open Glance preferences, colors, shortcuts & sleep mode", "⚙️", "#1CE755", "settings", "pengaturan", "customize", "glance", "theme", "color"),
                CreateCmd("cmd_update", "Check for Glance Updates", "Check GitHub for the latest version of Glance", "🔄", "#38BDF8", "update", "pembaruan", "upgrade", "version", "github"),
                CreateCmd("cmd_mute", "Mute / Unmute System Audio", "Toggle master speaker mute status", "🔊", "#38BDF8", "mute", "unmute", "audio", "sound", "volume", "suara"),
                CreateCmd("cmd_pin", "Pin / Unpin Glance Notch HUD", "Keep Glance expanded or allow auto-collapse", "📌", "#F59E0B", "pin", "unpin", "notch", "lock hud"),
                CreateCmd("cmd_aod", "Toggle Notch Sleep Mode (AOD)", "Enable or disable Always-On Display sleep mode when mouse is idle", "🌙", "#A855F7", "sleep mode", "aod", "always on", "idle"),
                CreateCmd("cmd_lock", "Lock Windows Screen", "Lock your PC workstation immediately (Win + L)", "🔒", "#F43F5E", "lock", "lock screen", "kunci", "kunci layar"),
                CreateCmd("cmd_sleep_pc", "Sleep Computer (Standby)", "Put your Windows PC into low-power sleep state", "💤", "#8B5CF6", "sleep pc", "standby", "tidur", "hibernate"),
                CreateCmd("cmd_recycle", "Empty Recycle Bin", "Permanently clear all files in the Windows Recycle Bin", "🗑️", "#EF4444", "empty recycle bin", "recycle", "trash", "sampah", "bersihkan"),
                CreateCmd("cmd_taskmgr", "Open Task Manager", "Monitor running processes and system performance", "📊", "#10B981", "task manager", "taskmgr", "proses", "ram"),
                CreateCmd("cmd_winsettings", "Open Windows Settings", "Launch native Windows 10/11 system settings", "🪟", "#0EA5E9", "windows settings", "control panel", "system settings"),
                CreateCmd("cmd_explorer_restart", "Restart Windows Explorer", "Restart explorer.exe shell & taskbar cleanly", "🔃", "#F97316", "restart explorer", "explorer", "taskbar")
            };
            return list;
        }

        private static AppSearchEntry CreateCmd(string id, string name, string subtitle, string emoji, string colorHex, params string[] keywords)
        {
            string lower = name.ToLowerInvariant();
            var allWords = Regex.Split(lower, @"[\s\-_\./]+")
                .Concat(keywords.SelectMany(k => Regex.Split(k.ToLowerInvariant(), @"[\s\-_\./]+")))
                .Where(w => w.Length > 0)
                .Distinct()
                .ToArray();

            return new AppSearchEntry
            {
                EntryKind = "command",
                CommandId = id,
                Name = name,
                LowerName = lower + " " + string.Join(" ", keywords).ToLowerInvariant(),
                Words = allWords,
                Subtitle = "System Command • " + subtitle,
                EmojiIcon = emoji,
                BadgeColorHex = colorHex,
                ActionHint = "Run ↵"
            };
        }

        public static List<AppSearchEntry> GetMatchingSystemCommands(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<AppSearchEntry>();
            query = query.Trim().ToLowerInvariant();

            var matches = new List<Tuple<int, AppSearchEntry>>();
            foreach (var cmd in systemCommands)
            {
                if (cmd.Name.ToLowerInvariant().StartsWith(query))
                {
                    matches.Add(Tuple.Create(1, cmd));
                }
                else if (cmd.Words != null && cmd.Words.Any(w => w.StartsWith(query)))
                {
                    matches.Add(Tuple.Create(2, cmd));
                }
                else if (cmd.LowerName.Contains(query))
                {
                    matches.Add(Tuple.Create(3, cmd));
                }
            }

            return matches
                .OrderBy(t => t.Item1)
                .ThenBy(t => t.Item2.Name.Length)
                .Select(t => t.Item2)
                .Take(4)
                .ToList();
        }

        /// <summary>
        /// Mengevaluasi ekspresi matematika instan (misal: "250 * 14", "150000 - 20%", "2^10", "sqrt(144)").
        /// </summary>
        public static bool TryEvaluateCalculator(string rawInput, out AppSearchEntry calcEntry)
        {
            calcEntry = null;
            if (string.IsNullOrWhiteSpace(rawInput)) return false;

            string expr = rawInput.Trim();
            if (expr.StartsWith("=")) expr = expr.Substring(1).Trim();
            if (expr.Length < 2) return false;

            // Pastikan mengandung angka dan operator matematika atau fungsi
            bool hasDigit = expr.Any(char.IsDigit);
            bool hasMathOp = expr.IndexOfAny(new char[] { '+', '-', '*', '/', '^', '%', '×', '÷', '(' }) >= 0 ||
                             Regex.IsMatch(expr, @"\b(sqrt|sin|cos|tan|abs|round|log)\b", RegexOptions.IgnoreCase) ||
                             Regex.IsMatch(expr, @"\d\s*[xX]\s*\d");

            if (!hasDigit || !hasMathOp) return false;

            // Jangan evaluasi jika terlihat seperti nama aplikasi biasa (misal "Windows 11")
            if (Regex.IsMatch(expr, @"^[a-zA-Z]+\s+\d+$")) return false;

            try
            {
                string normalized = NormalizeMathExpression(expr);
                if (string.IsNullOrWhiteSpace(normalized)) return false;

                using (DataTable dt = new DataTable())
                {
                    object rawObj = dt.Compute(normalized, "");
                    if (rawObj == null || rawObj == DBNull.Value) return false;

                    double val = Convert.ToDouble(rawObj, CultureInfo.InvariantCulture);
                    if (double.IsNaN(val) || double.IsInfinity(val)) return false;

                    string rawStr = Math.Abs(val - Math.Round(val)) < 1e-9
                        ? ((long)Math.Round(val)).ToString(CultureInfo.InvariantCulture)
                        : Math.Round(val, 6).ToString(CultureInfo.InvariantCulture);

                    string formattedStr = Math.Abs(val - Math.Round(val)) < 1e-9
                        ? ((long)Math.Round(val)).ToString("N0", CultureInfo.InvariantCulture)
                        : val.ToString("#,##0.######", CultureInfo.InvariantCulture);

                    calcEntry = new AppSearchEntry
                    {
                        EntryKind = "calc",
                        Name = $"= {formattedStr}",
                        Subtitle = $"Calculator ({rawInput.Trim()}) • Press Enter to copy \"{rawStr}\"",
                        EmojiIcon = "🧮",
                        BadgeColorHex = "#38BDF8",
                        ActionHint = "Copy ↵",
                        CopyText = rawStr
                    };
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeMathExpression(string expr)
        {
            string s = expr
                .Replace("×", "*")
                .Replace("÷", "/")
                .Replace(",", "");

            // Ganti 'x' di antara angka menjadi '*' (misal 25 x 4 -> 25 * 4)
            s = Regex.Replace(s, @"(?<=\d\s*)[xX](?=\s*\d)", "*");

            // Konstanta pi
            s = Regex.Replace(s, @"\bpi\b", Math.PI.ToString(CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);

            // Fungsi matematika dasar: sqrt(x), abs(x), round(x)
            s = EvaluateMathFunctions(s);

            // Persentase penjumlahan/pengurangan: "150000 - 20%" -> "150000 - (150000 * 0.20)"
            s = Regex.Replace(s, @"(\d+(?:\.\d+)?)\s*([\+\-])\s*(\d+(?:\.\d+)?)\s*%", m =>
            {
                double baseNum = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                string op = m.Groups[2].Value;
                double pct = double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture) / 100.0;
                double delta = baseNum * pct;
                return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2}", baseNum, op, delta);
            });

            // Persentase umum: "20%" -> "(20 / 100.0)"
            s = Regex.Replace(s, @"(\d+(?:\.\d+)?)\s*%", m =>
            {
                double pct = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) / 100.0;
                return pct.ToString(CultureInfo.InvariantCulture);
            });

            // Pangkat: "2^10" -> hasil Math.Pow(2, 10)
            while (Regex.IsMatch(s, @"(\d+(?:\.\d+)?)\s*\^\s*(\d+(?:\.\d+)?)"))
            {
                s = Regex.Replace(s, @"(\d+(?:\.\d+)?)\s*\^\s*(\d+(?:\.\d+)?)", m =>
                {
                    double b = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
                    double e = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    return Math.Pow(b, e).ToString(CultureInfo.InvariantCulture);
                });
            }

            // Hanya izinkan karakter matematika yang aman untuk DataTable.Compute
            if (!Regex.IsMatch(s, @"^[\d\s\+\-\*\/\(\)\.]+$"))
            {
                return null;
            }

            return s;
        }

        private static string EvaluateMathFunctions(string input)
        {
            string pattern = @"\b(sqrt|sin|cos|tan|abs|round)\s*\(\s*(\-?\d+(?:\.\d+)?)\s*\)";
            int safety = 0;
            while (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase) && safety++ < 10)
            {
                input = Regex.Replace(input, pattern, m =>
                {
                    string fn = m.Groups[1].Value.ToLowerInvariant();
                    double arg = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    double res = arg;
                    switch (fn)
                    {
                        case "sqrt": res = Math.Sqrt(arg); break;
                        case "sin": res = Math.Sin(arg * Math.PI / 180.0); break;
                        case "cos": res = Math.Cos(arg * Math.PI / 180.0); break;
                        case "tan": res = Math.Tan(arg * Math.PI / 180.0); break;
                        case "abs": res = Math.Abs(arg); break;
                        case "round": res = Math.Round(arg); break;
                    }
                    return res.ToString(CultureInfo.InvariantCulture);
                }, RegexOptions.IgnoreCase);
            }
            return input;
        }

        /// <summary>
        /// Menghasilkan opsi pencarian web instan (prefix "g ", "yt ", "gh ", "ai " atau fallback pencarian web).
        /// </summary>
        public static List<AppSearchEntry> GetWebSearchEntries(string rawInput, out bool isExplicitWebPrefix)
        {
            isExplicitWebPrefix = false;
            var list = new List<AppSearchEntry>();
            if (string.IsNullOrWhiteSpace(rawInput)) return list;

            string trimmed = rawInput.Trim();
            string lower = trimmed.ToLowerInvariant();

            if (lower.StartsWith("g ") || lower.StartsWith("? "))
            {
                string q = trimmed.Substring(2).Trim();
                if (!string.IsNullOrEmpty(q))
                {
                    isExplicitWebPrefix = true;
                    list.Add(CreateWebEntry($"Search Google for \"{q}\"", "Google Web Search • Opens in default browser", "🌐", "#38BDF8", "https://www.google.com/search?q=" + Uri.EscapeDataString(q)));
                }
                return list;
            }

            if (lower.StartsWith("yt "))
            {
                string q = trimmed.Substring(3).Trim();
                if (!string.IsNullOrEmpty(q))
                {
                    isExplicitWebPrefix = true;
                    list.Add(CreateWebEntry($"Search YouTube for \"{q}\"", "YouTube Video Search • Opens in default browser", "▶️", "#EF4444", "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(q)));
                }
                return list;
            }

            if (lower.StartsWith("gh "))
            {
                string q = trimmed.Substring(3).Trim();
                if (!string.IsNullOrEmpty(q))
                {
                    isExplicitWebPrefix = true;
                    list.Add(CreateWebEntry($"Search GitHub for \"{q}\"", "GitHub Repositories & Code • Opens in default browser", "🐙", "#A855F7", "https://github.com/search?q=" + Uri.EscapeDataString(q)));
                }
                return list;
            }

            if (lower.StartsWith("ai ") || lower.StartsWith("gpt "))
            {
                int idx = trimmed.IndexOf(' ');
                string q = idx > 0 ? trimmed.Substring(idx + 1).Trim() : "";
                if (!string.IsNullOrEmpty(q))
                {
                    isExplicitWebPrefix = true;
                    list.Add(CreateWebEntry($"Ask ChatGPT: \"{q}\"", "AI Assistant Query • Opens in default browser", "✨", "#10B981", "https://chatgpt.com/?q=" + Uri.EscapeDataString(q)));
                }
                return list;
            }

            // Fallback web search items
            list.Add(CreateWebEntry($"Search Google for \"{trimmed}\"", "Web Search • google.com", "🌐", "#38BDF8", "https://www.google.com/search?q=" + Uri.EscapeDataString(trimmed)));
            list.Add(CreateWebEntry($"Search YouTube for \"{trimmed}\"", "Video Search • youtube.com", "▶️", "#EF4444", "https://www.youtube.com/results?search_query=" + Uri.EscapeDataString(trimmed)));
            return list;
        }

        private static AppSearchEntry CreateWebEntry(string title, string subtitle, string emoji, string colorHex, string url)
        {
            return new AppSearchEntry
            {
                EntryKind = "web",
                Name = title,
                Subtitle = subtitle,
                EmojiIcon = emoji,
                BadgeColorHex = colorHex,
                LaunchPath = url,
                ActionHint = "Search ↵"
            };
        }
    }
}
