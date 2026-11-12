using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace Glance
{
    /// <summary>
    /// Mengontrol hardware sistem secara native: Volume Audio, Peak Level Meter, Kecerahan Layar (WMI), dan Status Wi-Fi (WLAN API).
    /// </summary>
    public static class HardwareController
    {
        [DllImport("wlanapi.dll")]
        private static extern uint WlanOpenHandle(uint dwClientVersion, IntPtr pReserved, out uint pdwNegotiatedVersion, out IntPtr phClientHandle);
        [DllImport("wlanapi.dll")]
        private static extern uint WlanCloseHandle(IntPtr hClientHandle, IntPtr pReserved);
        [DllImport("wlanapi.dll")]
        private static extern uint WlanEnumInterfaces(IntPtr hClientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);
        [DllImport("wlanapi.dll")]
        private static extern uint WlanQueryInterface(IntPtr hClientHandle, ref Guid pInterfaceGuid, int OpCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, IntPtr pWlanOpcodeValueType);
        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr pMemory);

        private static readonly object audioLock = new object();
        private static IAudioEndpointVolume cachedVolume = null;
        private static IAudioMeterInformation cachedMeter = null;
        private static DateTime lastAudioInit = DateTime.MinValue;

        private static void SafeReleaseCom(ref object comObj)
        {
            if (comObj != null)
            {
                try
                {
                    if (Marshal.IsComObject(comObj))
                    {
                        Marshal.ReleaseComObject(comObj);
                    }
                }
                catch { }
                comObj = null;
            }
        }

        private static void EnsureAudioEndpoints(bool forceRefresh = false)
        {
            lock (audioLock)
            {
                if (!forceRefresh && cachedVolume != null && cachedMeter != null && (DateTime.UtcNow - lastAudioInit).TotalSeconds < 30.0)
                {
                    return;
                }

                object enumObj = null;
                IMMDeviceEnumerator enumerator = null;
                IMMDevice dev = null;
                try
                {
                    enumObj = new MMDeviceEnumeratorComObject();
                    enumerator = enumObj as IMMDeviceEnumerator;
                    if (enumerator != null)
                    {
                        enumerator.GetDefaultAudioEndpoint(0, 1, out dev);
                        if (dev != null)
                        {
                            object oldVol = cachedVolume;
                            cachedVolume = null;
                            SafeReleaseCom(ref oldVol);

                            object oldMeter = cachedMeter;
                            cachedMeter = null;
                            SafeReleaseCom(ref oldMeter);

                            Guid iidVol = typeof(IAudioEndpointVolume).GUID;
                            object oVol;
                            dev.Activate(ref iidVol, 23, IntPtr.Zero, out oVol);
                            cachedVolume = oVol as IAudioEndpointVolume;

                            Guid iidMeter = typeof(IAudioMeterInformation).GUID;
                            object oMeter;
                            dev.Activate(ref iidMeter, 23, IntPtr.Zero, out oMeter);
                            cachedMeter = oMeter as IAudioMeterInformation;

                            lastAudioInit = DateTime.UtcNow;
                        }
                    }
                }
                catch { }
                finally
                {
                    object d = dev;
                    SafeReleaseCom(ref d);
                    object e = enumerator;
                    SafeReleaseCom(ref e);
                    SafeReleaseCom(ref enumObj);
                }
            }
        }

        /// <summary>
        /// Returns the current output peak level (0..1) of the default speaker, or -1 if unavailable.
        /// </summary>
        public static float GetPeakLevel()
        {
            try
            {
                EnsureAudioEndpoints();
                IAudioMeterInformation meter = cachedMeter;
                if (meter == null) return -1f;

                float peak;
                if (meter.GetPeakValue(out peak) != 0)
                {
                    EnsureAudioEndpoints(forceRefresh: true);
                    return -1f;
                }
                return peak;
            }
            catch
            {
                return -1f;
            }
        }

        public static void GetAudioStatus(out int volumePercent, out bool isMuted)
        {
            volumePercent = 50;
            isMuted = false;
            try
            {
                EnsureAudioEndpoints();
                IAudioEndpointVolume vol = cachedVolume;
                if (vol != null)
                {
                    float level;
                    bool muted;
                    vol.GetMasterVolumeLevelScalar(out level);
                    vol.GetMute(out muted);
                    volumePercent = (int)Math.Round(level * 100.0f);
                    isMuted = muted;
                }
            }
            catch
            {
                EnsureAudioEndpoints(forceRefresh: true);
            }
        }

        public static void SetAudioVolume(int volumePercent)
        {
            try
            {
                EnsureAudioEndpoints();
                IAudioEndpointVolume vol = cachedVolume;
                if (vol != null)
                {
                    float scalar = Math.Max(0f, Math.Min(1f, volumePercent / 100.0f));
                    Guid ctx = Guid.Empty;
                    vol.SetMasterVolumeLevelScalar(scalar, ref ctx);
                    if (volumePercent > 0)
                    {
                        vol.SetMute(false, ref ctx);
                    }
                }
            }
            catch { }
        }

        public static void ToggleAudioMute()
        {
            try
            {
                EnsureAudioEndpoints();
                IAudioEndpointVolume vol = cachedVolume;
                if (vol != null)
                {
                    bool muted;
                    vol.GetMute(out muted);
                    Guid ctx = Guid.Empty;
                    vol.SetMute(!muted, ref ctx);
                }
            }
            catch { }
        }

        public static int GetScreenBrightness()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT CurrentBrightness FROM WmiMonitorBrightness"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        using (obj)
                        {
                            return Convert.ToInt32(obj["CurrentBrightness"]);
                        }
                    }
                }
            }
            catch { }
            return 100;
        }

        public static void SetScreenBrightness(int percent)
        {
            try
            {
                int val = Math.Max(5, Math.Min(100, percent));
                using (var searcher = new ManagementObjectSearcher("root\\WMI", "SELECT * FROM WmiMonitorBrightnessMethods"))
                using (var results = searcher.Get())
                {
                    foreach (ManagementObject obj in results)
                    {
                        using (obj)
                        {
                            obj.InvokeMethod("WmiSetBrightness", new object[] { (uint)1, (byte)val });
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        public static void GetWifiStatus(out bool connected, out string ssid, out int signalPercent)
        {
            connected = false;
            ssid = "Not Connected";
            signalPercent = 0;

            try
            {
                uint ver;
                IntPtr client;
                if (WlanOpenHandle(2, IntPtr.Zero, out ver, out client) == 0)
                {
                    IntPtr listPtr;
                    if (WlanEnumInterfaces(client, IntPtr.Zero, out listPtr) == 0)
                    {
                        uint count = (uint)Marshal.ReadInt32(listPtr, 0);
                        for (int i = 0; i < count; i++)
                        {
                            IntPtr itemPtr = new IntPtr(listPtr.ToInt64() + 8 + (i * 532));
                            byte[] guidBytes = new byte[16];
                            Marshal.Copy(itemPtr, guidBytes, 0, 16);
                            Guid ifGuid = new Guid(guidBytes);
                            int state = Marshal.ReadInt32(itemPtr, 528);
                            if (state == 1)
                            {
                                uint dataSize;
                                IntPtr dataPtr;
                                if (WlanQueryInterface(client, ref ifGuid, 7, IntPtr.Zero, out dataSize, out dataPtr, IntPtr.Zero) == 0)
                                {
                                    int ssidLen = Marshal.ReadInt32(dataPtr, 520);
                                    if (ssidLen > 0 && ssidLen <= 32)
                                    {
                                        byte[] ssidBytes = new byte[ssidLen];
                                        Marshal.Copy(new IntPtr(dataPtr.ToInt64() + 524), ssidBytes, 0, ssidLen);
                                        ssid = Encoding.UTF8.GetString(ssidBytes);
                                    }
                                    signalPercent = Math.Max(0, Math.Min(100, Marshal.ReadInt32(dataPtr, 576)));
                                    connected = true;
                                    WlanFreeMemory(dataPtr);
                                    break;
                                }
                            }
                        }
                        WlanFreeMemory(listPtr);
                    }
                    WlanCloseHandle(client, IntPtr.Zero);
                }
            }
            catch { }
        }
    }
}
