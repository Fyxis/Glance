using System;

namespace Glance
{
    /// <summary>
    /// Representasi satu baris lirik tersinkronisasi (.LRC) beserta waktu kemunculannya dalam detik.
    /// </summary>
    public class LyricLine
    {
        public double TimeSec { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Model respons pencarian lirik dari API eksternal lrclib.net.
    /// </summary>
    public class LrcSearchItem
    {
        public string trackName { get; set; }
        public string artistName { get; set; }
        public string albumName { get; set; }
        public double duration { get; set; }
        public string syncedLyrics { get; set; }
        public string plainLyrics { get; set; }
    }

    /// <summary>
    /// Snapshot status pemutaran media dari Windows GSMTC (Global System Media Transport Controls).
    /// </summary>
    public class MediaSnapshot
    {
        public string Title = "";
        public string Artist = "";
        public string Status = "None";
        public double Position = 0;
        public double RawPosition = 0;
        public bool HasValidLastUpdated = false;
        public double Duration = 0;
        public byte[] ThumbnailBytes = null;
        public bool ThumbnailChanged = false;
    }
}
