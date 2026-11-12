using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Glance
{
    /// <summary>
    /// Layanan logika bisnis (Application Layer) untuk membersihkan judul lagu,
    /// memberi skor kandidat lirik berdasarkan kecocokan durasi, dan mem-parsing format .LRC.
    /// </summary>
    public static class LyricsScoringService
    {
        public static string CleanTrackTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return "";
            return Regex.Replace(
                title,
                @"\s*(\((feat|ft|with|official|video|audio|lyrics|visualizer|explicit|remaster).*?\)|\[.*?\])",
                "",
                RegexOptions.IgnoreCase
            ).Trim();
        }

        public static string CleanArtistName(string artist)
        {
            if (string.IsNullOrWhiteSpace(artist)) return "";
            string clean = Regex.Replace(artist, @"\s*(- Topic|VEVO)\b", "", RegexOptions.IgnoreCase).Trim();
            return Regex.Replace(clean, @"\s*[,&].*$", "").Trim();
        }

        public static double ScoreCandidate(LrcSearchItem item, string rawTitle, string cleanTitle, string cleanArtist, double targetDur)
        {
            if (item == null) return -9999.0;
            bool hasSynced = !string.IsNullOrWhiteSpace(item.syncedLyrics);
            bool hasPlain = !string.IsNullOrWhiteSpace(item.plainLyrics);
            if (!hasSynced && !hasPlain) return -9999.0;

            double score = hasSynced ? 500.0 : 100.0;

            if (targetDur > 10.0 && item.duration > 10.0)
            {
                double diff = Math.Abs(item.duration - targetDur);
                if (diff <= 1.5) score += 160.0 - (diff * 10.0);
                else if (diff <= 3.0) score += 110.0 - (diff * 12.0);
                else if (diff <= 6.0) score += 40.0 - (diff * 5.0);
                else score -= Math.Min(220.0, diff * 8.0);
            }

            string itemTrack = (item.trackName ?? "").Trim();
            string itemArtist = (item.artistName ?? "").Trim();
            string itemAlbum = (item.albumName ?? "").Trim();

            if (!string.IsNullOrEmpty(cleanTitle) && !string.IsNullOrEmpty(itemTrack))
            {
                if (string.Equals(itemTrack, cleanTitle, StringComparison.OrdinalIgnoreCase)) score += 35.0;
                else if (itemTrack.IndexOf(cleanTitle, StringComparison.OrdinalIgnoreCase) >= 0) score += 15.0;
            }

            if (!string.IsNullOrEmpty(cleanArtist) && !string.IsNullOrEmpty(itemArtist))
            {
                if (string.Equals(itemArtist, cleanArtist, StringComparison.OrdinalIgnoreCase)) score += 35.0;
                else if (itemArtist.IndexOf(cleanArtist, StringComparison.OrdinalIgnoreCase) >= 0 ||
                         cleanArtist.IndexOf(itemArtist, StringComparison.OrdinalIgnoreCase) >= 0) score += 25.0;
            }

            string combinedMeta = (itemTrack + " " + itemAlbum).ToLowerInvariant();
            string rawLower = (rawTitle ?? "").ToLowerInvariant();
            string[] variantKeywords = { "slowed", "reverb", "sped up", "nightcore", "8d", "chopped", "instrumental", "karaoke", "live", "remix", "acoustic" };
            foreach (string kw in variantKeywords)
            {
                if (combinedMeta.Contains(kw) && !rawLower.Contains(kw))
                {
                    score -= 120.0;
                }
            }

            return score;
        }

        public static List<LyricLine> ParseSyncedLrc(string rawSynced)
        {
            var result = new List<LyricLine>();
            if (string.IsNullOrWhiteSpace(rawSynced)) return result;

            foreach (string line in rawSynced.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Match m = Regex.Match(line, @"^\[(\d+):(\d+(?:\.\d+)?)\]\s*(.*)$");
                if (m.Success)
                {
                    double sec = int.Parse(m.Groups[1].Value) * 60.0 + double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
                    string txt = m.Groups[3].Value.Trim();
                    if (!string.IsNullOrEmpty(txt))
                    {
                        result.Add(new LyricLine { TimeSec = sec, Text = txt });
                    }
                }
            }
            return result;
        }
    }
}
