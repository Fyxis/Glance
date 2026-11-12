using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Glance
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try { File.WriteAllText("d:\\Coding-an\\windows-notch\\error.log", e.ExceptionObject.ToString()); } catch { }
                };

                int currentPid = Process.GetCurrentProcess().Id;
                var existingProcs = Process.GetProcessesByName("Glance")
                    .Concat(Process.GetProcessesByName("DynamicIsland"));
                foreach (var p in existingProcs)
                {
                    if (p.Id != currentPid)
                    {
                        try { p.Kill(); } catch { }
                    }
                }

                Application app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.DispatcherUnhandledException += (s, e) =>
                {
                    e.Handled = true;
                    try { File.WriteAllText("d:\\Coding-an\\windows-notch\\error.log", e.Exception.ToString()); } catch { }
                };

                MainWindow mainWin = new MainWindow();
                app.MainWindow = mainWin;
                mainWin.Show();

                if (args != null && args.Any(a => a.Equals("--settings", StringComparison.OrdinalIgnoreCase)))
                {
                    mainWin.Dispatcher.BeginInvoke(new Action(() => mainWin.OpenSettingsDialog()));
                }

                app.Run();
            }
            catch (Exception ex)
            {
                File.WriteAllText("d:\\Coding-an\\windows-notch\\error.log", ex.ToString());
            }
        }
    }
}
