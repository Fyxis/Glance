using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Glance
{
    /// <summary>
    /// Mengelola pendaftaran otomatis aplikasi pada Windows Startup melalui Registry (HKCU\...\Run).
    /// </summary>
    public static class StartupManager
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppValueName = "Glance";
        private const string LegacyAppValueName = "WindowsDynamicIsland";

        public static bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(AppValueName) ?? key.GetValue(LegacyAppValueName);
                    return val != null && !string.IsNullOrWhiteSpace(val.ToString());
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetStartup(bool enable, string exePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Process.GetCurrentProcess().MainModule.FileName;
                }
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null) return;
                    try { key.DeleteValue(LegacyAppValueName, false); } catch { }
                    if (enable)
                    {
                        key.SetValue(AppValueName, "\"" + exePath + "\"");
                    }
                    else
                    {
                        key.DeleteValue(AppValueName, false);
                    }
                }
            }
            catch { }
        }
    }
}
