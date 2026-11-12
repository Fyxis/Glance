using System;

namespace Glance
{
    /// <summary>
    /// Item catatan cepat (Quick Notes / To-Do) pada Slide 4.
    /// </summary>
    public class NoteItem
    {
        public string id { get; set; }
        public string text { get; set; }
        public bool done { get; set; }
    }

    /// <summary>
    /// Item riwayat clipboard (teks yang disalin dengan Ctrl+C) pada Slide 3.
    /// </summary>
    public class ClipItem
    {
        public string id { get; set; }
        public string text { get; set; }
        public string time { get; set; }
        public bool pinned { get; set; }
    }

    /// <summary>
    /// Item file yang ditampung sementara pada Drop Shelf (Slide 5).
    /// </summary>
    public class ShelfItem
    {
        public string id { get; set; }
        public string filePath { get; set; }
        public string fileName { get; set; }
        public long fileSizeBytes { get; set; }
        public string timeAdded { get; set; }
    }
}
