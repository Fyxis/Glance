using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;

namespace Glance
{
    /// <summary>
    /// Memantau kecepatan internet real-time (Download & Upload) dari kartu jaringan aktif.
    /// </summary>
    public static class NetworkSpeedMonitor
    {
        private static long prevRx = -1;
        private static long prevTx = -1;
        private static int prevIfaceCount = -1;
        private static readonly Stopwatch sampleWatch = new Stopwatch();
        private static double smoothDown = 0;
        private static double smoothUp = 0;
        private static NetworkInterface[] cachedInterfaces = null;
        private static DateTime lastInterfaceRefresh = DateTime.MinValue;

        private static readonly string[] ignoredAdapterKeywords =
        {
            "virtual", "hyper-v", "vethernet", "vmware", "virtualbox", "pseudo", "loopback", "wan miniport", "filter", "npcap", "bluetooth"
        };

        private static void RefreshInterfacesIfNeeded()
        {
            if (cachedInterfaces != null && (DateTime.UtcNow - lastInterfaceRefresh).TotalSeconds < 60.0)
            {
                return;
            }

            try
            {
                var list = new List<NetworkInterface>();
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                    string desc = ((ni.Description ?? "") + " " + (ni.Name ?? "")).ToLowerInvariant();
                    if (ignoredAdapterKeywords.Any(k => desc.Contains(k))) continue;

                    list.Add(ni);
                }
                cachedInterfaces = list.ToArray();
                lastInterfaceRefresh = DateTime.UtcNow;
            }
            catch
            {
                cachedInterfaces = new NetworkInterface[0];
            }
        }

        public static void Sample(out double downBytesPerSec, out double upBytesPerSec)
        {
            RefreshInterfacesIfNeeded();

            long rx = 0, tx = 0;
            int ifaceCount = 0;
            try
            {
                var ifaces = cachedInterfaces;
                if (ifaces != null)
                {
                    for (int i = 0; i < ifaces.Length; i++)
                    {
                        var ni = ifaces[i];
                        if (ni.OperationalStatus != OperationalStatus.Up) continue;
                        IPv4InterfaceStatistics st = ni.GetIPv4Statistics();
                        rx += st.BytesReceived;
                        tx += st.BytesSent;
                        ifaceCount++;
                    }
                }
            }
            catch
            {
                cachedInterfaces = null; // force refresh on next sample if error
            }

            double elapsed = sampleWatch.IsRunning ? sampleWatch.Elapsed.TotalSeconds : 0;
            sampleWatch.Restart();

            // Skip a sample when adapters appear/disappear to avoid huge fake spikes
            if (prevRx >= 0 && elapsed > 0.05 && ifaceCount == prevIfaceCount)
            {
                double d = Math.Max(0, (rx - prevRx) / elapsed);
                double u = Math.Max(0, (tx - prevTx) / elapsed);
                smoothDown = smoothDown * 0.35 + d * 0.65;
                smoothUp = smoothUp * 0.35 + u * 0.65;
            }

            prevRx = rx;
            prevTx = tx;
            prevIfaceCount = ifaceCount;
            downBytesPerSec = smoothDown;
            upBytesPerSec = smoothUp;
        }

        public static string Format(double bytesPerSec)
        {
            double kb = bytesPerSec / 1024.0;
            if (kb < 1) return "0 KB/s";
            if (kb < 10) return kb.ToString("F1", CultureInfo.InvariantCulture) + " KB/s";
            if (kb < 1024) return kb.ToString("F0", CultureInfo.InvariantCulture) + " KB/s";
            double mb = kb / 1024.0;
            return mb.ToString(mb < 100 ? "F1" : "F0", CultureInfo.InvariantCulture) + " MB/s";
        }
    }
}
