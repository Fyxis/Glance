using System;
using System.Runtime;

namespace Glance
{
    /// <summary>
    /// Mengoptimalkan konsumsi memori (RAM) dan meminimalkan Working Set proses Windows.
    /// </summary>
    public static class MemoryOptimizer
    {
        private static DateTime lastTrim = DateTime.MinValue;
        private static readonly object trimLock = new object();

        public static void TrimMemory(bool force = false)
        {
            try
            {
                lock (trimLock)
                {
                    if (!force && (DateTime.UtcNow - lastTrim).TotalSeconds < 2.0)
                    {
                        return;
                    }
                    lastTrim = DateTime.UtcNow;
                }

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(2, GCCollectionMode.Forced, true, true);

                IntPtr handle = NativeMethods.GetCurrentProcess();
                NativeMethods.SetProcessWorkingSetSize(handle, new IntPtr(-1), new IntPtr(-1));
                NativeMethods.EmptyWorkingSet(handle);
            }
            catch { }
        }
    }
}
