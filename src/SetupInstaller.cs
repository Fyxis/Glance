using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using Path = System.Windows.Shapes.Path;

namespace GlanceSetup
{
    // Native COM IShellLink for creating real Windows .lnk shortcuts without external dependencies
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    public class SetupWindow : Window
    {
        private const string RunRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string UninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Glance";
        private const string LegacyUninstallRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\WindowsDynamicIsland";
        private const string AppRegName = "Glance";
        private const string LegacyAppRegName = "WindowsDynamicIsland";

        private TextBox txtInstallDir;
        private CheckBox chkRunAtStartup;
        private CheckBox chkDesktopShortcut;
        private CheckBox chkStartMenuShortcut;
        private CheckBox chkLaunchAfterInstall;
        private TextBlock statusLabel;
        private Border progressFill;
        private Border progressTrack;
        private Button btnInstall;
        private Button btnUninstall;

        public SetupWindow()
        {
            Title = "Glance — Setup Installer";
            Width = 520;
            Height = 470;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;

            string defaultDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Glance"
            );

            BuildUI(defaultDir);
        }

        private void BuildUI(string defaultDir)
        {
            Border outer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 19)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(26, 22, 26, 22),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 28,
                    ShadowDepth = 8,
                    Opacity = 0.65,
                    Color = Colors.Black
                }
            };

            outer.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            StackPanel root = new StackPanel();

            // 1. Top Window Bar
            DockPanel topBar = new DockPanel { Margin = new Thickness(0, 0, 0, 16) };
            Button closeBtn = new Button
            {
                Content = "✕",
                Width = 28,
                Height = 28,
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 38)),
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            closeBtn.Click += (s, e) => Close();
            DockPanel.SetDock(closeBtn, Dock.Right);

            // Mini Notch Logo Preview
            StackPanel brandRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border miniNotch = new Border
            {
                Width = 68,
                Height = 24,
                CornerRadius = new CornerRadius(12),
                Background = Brushes.Black,
                BorderBrush = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(0, 0, 12, 0)
            };
            StackPanel miniInner = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Ellipse greenDot = new Ellipse
            {
                Width = 10,
                Height = 10,
                Stroke = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                StrokeThickness = 2.2,
                Margin = new Thickness(0, 0, 6, 0)
            };
            TextBlock miniTime = new TextBlock
            {
                Text = "11:54",
                Foreground = Brushes.White,
                FontSize = 9.5,
                FontWeight = FontWeights.Bold
            };
            miniInner.Children.Add(greenDot);
            miniInner.Children.Add(miniTime);
            miniNotch.Child = miniInner;

            StackPanel titleStack = new StackPanel();
            titleStack.Children.Add(new TextBlock
            {
                Text = "Glance Setup",
                Foreground = Brushes.White,
                FontSize = 17,
                FontWeight = FontWeights.Bold
            });
            titleStack.Children.Add(new TextBlock
            {
                Text = "Native Windows Desktop Companion (v1.0 • Tanpa Electron/Chromium)",
                Foreground = new SolidColorBrush(Color.FromRgb(145, 145, 155)),
                FontSize = 11.5
            });

            brandRow.Children.Add(miniNotch);
            brandRow.Children.Add(titleStack);

            topBar.Children.Add(closeBtn);
            topBar.Children.Add(brandRow);
            root.Children.Add(topBar);

            // 2. Install Directory Box
            root.Children.Add(new TextBlock
            {
                Text = "Lokasi Instalasi Aplikasi:",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 210)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 2, 0, 6)
            });

            Grid dirRow = new Grid { Margin = new Thickness(0, 0, 0, 14) };
            dirRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            dirRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            dirRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(82) });

            txtInstallDir = new TextBox
            {
                Text = defaultDir,
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(24, 24, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 56)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 5, 8, 5),
                FontSize = 11.5,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(txtInstallDir, 0);

            Button btnBrowse = new Button
            {
                Content = "Pilih...",
                Height = 32,
                Background = new SolidColorBrush(Color.FromRgb(38, 38, 46)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            btnBrowse.Click += (s, e) =>
            {
                using (var dlg = new Forms.FolderBrowserDialog())
                {
                    dlg.Description = "Pilih folder instalasi Glance";
                    dlg.SelectedPath = txtInstallDir.Text;
                    if (dlg.ShowDialog() == Forms.DialogResult.OK)
                    {
                        txtInstallDir.Text = dlg.SelectedPath;
                    }
                }
            };
            Grid.SetColumn(btnBrowse, 2);

            dirRow.Children.Add(txtInstallDir);
            dirRow.Children.Add(btnBrowse);
            root.Children.Add(dirRow);

            // 3. Setup Options (Checkboxes including "Run at Startup Windows")
            root.Children.Add(new TextBlock
            {
                Text = "Opsi Pengaturan & Startup:",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 210)),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 6)
            });

            Border optionsCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 22, 28)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 50)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 14)
            };

            StackPanel optsStack = new StackPanel();

            chkRunAtStartup = CreateSetupCheckBox(
                "Run at Startup Windows (Jalankan otomatis saat Windows menyala)",
                true,
                true
            );
            chkDesktopShortcut = CreateSetupCheckBox(
                "Buat Shortcut di Desktop (Glance)",
                true,
                false
            );
            chkStartMenuShortcut = CreateSetupCheckBox(
                "Buat Shortcut di Start Menu Windows",
                true,
                false
            );
            chkLaunchAfterInstall = CreateSetupCheckBox(
                "Jalankan Glance setelah instalasi selesai",
                true,
                false
            );

            optsStack.Children.Add(chkRunAtStartup);
            optsStack.Children.Add(chkDesktopShortcut);
            optsStack.Children.Add(chkStartMenuShortcut);
            optsStack.Children.Add(chkLaunchAfterInstall);
            optionsCard.Child = optsStack;
            root.Children.Add(optionsCard);

            // 4. Progress & Status Area
            statusLabel = new TextBlock
            {
                Text = "Siap memasang Glance di komputer Anda.",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 175)),
                FontSize = 11.5,
                Margin = new Thickness(2, 0, 2, 6)
            };
            root.Children.Add(statusLabel);

            progressTrack = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromRgb(34, 34, 42)),
                Margin = new Thickness(0, 0, 0, 18),
                ClipToBounds = true
            };
            progressFill = new Border
            {
                Height = 6,
                Width = 0,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            progressTrack.Child = progressFill;
            root.Children.Add(progressTrack);

            // 5. Bottom Action Buttons (Uninstall + Install)
            Grid btnRow = new Grid();
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(135) });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            btnRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            btnUninstall = new Button
            {
                Content = "Uninstall / Hapus",
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(42, 28, 32)),
                Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113)),
                BorderThickness = new Thickness(0),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            btnUninstall.Click += OnUninstallClick;
            Grid.SetColumn(btnUninstall, 0);

            btnInstall = new Button
            {
                Content = "Install / Pasang Sekarang",
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                FontSize = 13.5,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            btnInstall.Click += OnInstallClick;
            Grid.SetColumn(btnInstall, 2);

            btnRow.Children.Add(btnUninstall);
            btnRow.Children.Add(btnInstall);
            root.Children.Add(btnRow);

            outer.Child = root;
            Content = outer;
        }

        private CheckBox CreateSetupCheckBox(string text, bool isChecked, bool highlight)
        {
            return new CheckBox
            {
                Content = text,
                IsChecked = isChecked,
                Foreground = highlight
                    ? new SolidColorBrush(Color.FromRgb(28, 231, 85))
                    : new SolidColorBrush(Color.FromRgb(230, 230, 238)),
                FontWeight = highlight ? FontWeights.Bold : FontWeights.Medium,
                FontSize = 12.5,
                Margin = new Thickness(0, 4, 0, 5),
                Cursor = Cursors.Hand,
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        private void OnInstallClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string installDir = txtInstallDir.Text.Trim();
                if (string.IsNullOrEmpty(installDir))
                {
                    MessageBox.Show("Silakan pilih folder instalasi terlebih dahulu.", "Setup", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 1. Stop any running Glance.exe / DynamicIsland.exe before overwriting
                foreach (var p in Process.GetProcessesByName("Glance").Concat(Process.GetProcessesByName("DynamicIsland")))
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }

                Directory.CreateDirectory(installDir);
                string targetExe = System.IO.Path.Combine(installDir, "Glance.exe");

                // 2. Extract embedded Glance.exe resource (or copy from local folder if alongside)
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream resStream = asm.GetManifestResourceStream("Glance.exe") ?? asm.GetManifestResourceStream("DynamicIsland.exe");
                if (resStream != null)
                {
                    using (resStream)
                    using (FileStream fs = File.Create(targetExe))
                    {
                        resStream.CopyTo(fs);
                    }
                }
                else
                {
                    string localExe = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Glance.exe");
                    if (!File.Exists(localExe)) localExe = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DynamicIsland.exe");
                    File.Copy(localExe, targetExe, true);
                }

                // 3. Configure "Run at Startup Windows" registry key
                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
                {
                    if (runKey != null)
                    {
                        try { runKey.DeleteValue(LegacyAppRegName, false); } catch { }
                        if (chkRunAtStartup.IsChecked == true)
                        {
                            runKey.SetValue(AppRegName, "\"" + targetExe + "\"");
                        }
                        else
                        {
                            runKey.DeleteValue(AppRegName, false);
                        }
                    }
                }

                // 4. Create Desktop Shortcut if checked
                string desktopLnk = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "Glance.lnk"
                );
                string oldDesktopLnk = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "Windows Dynamic Island.lnk"
                );
                try { if (File.Exists(oldDesktopLnk)) File.Delete(oldDesktopLnk); } catch { }

                if (chkDesktopShortcut.IsChecked == true)
                {
                    CreateShortcut(desktopLnk, targetExe, installDir, "Glance Desktop Companion");
                }

                // 5. Create Start Menu Shortcut if checked
                string startMenuDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs"
                );
                string startMenuLnk = System.IO.Path.Combine(startMenuDir, "Glance.lnk");
                string oldStartMenuLnk = System.IO.Path.Combine(startMenuDir, "Windows Dynamic Island.lnk");
                try { if (File.Exists(oldStartMenuLnk)) File.Delete(oldStartMenuLnk); } catch { }

                if (chkStartMenuShortcut.IsChecked == true)
                {
                    CreateShortcut(startMenuLnk, targetExe, installDir, "Glance Desktop Companion");
                }

                // 6. Register in Windows Installed Apps (Programs & Features)
                try
                {
                    using (RegistryKey uninstKey = Registry.CurrentUser.CreateSubKey(UninstallRegistryPath))
                    {
                        if (uninstKey != null)
                        {
                            uninstKey.SetValue("DisplayName", "Glance");
                            uninstKey.SetValue("DisplayVersion", "1.0.0");
                            uninstKey.SetValue("Publisher", "Glance");
                            uninstKey.SetValue("DisplayIcon", targetExe);
                            uninstKey.SetValue("InstallLocation", installDir);
                            uninstKey.SetValue("UninstallString", "\"" + Process.GetCurrentProcess().MainModule.FileName + "\"");
                            uninstKey.SetValue("NoModify", 1, RegistryValueKind.DWord);
                            uninstKey.SetValue("NoRepair", 1, RegistryValueKind.DWord);
                        }
                    }
                }
                catch { }

                progressFill.Width = progressTrack.ActualWidth;
                statusLabel.Text = "✓ Instalasi berhasil! Run at Startup Windows: " + (chkRunAtStartup.IsChecked == true ? "AKTIF" : "NONAKTIF");
                statusLabel.Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85));

                if (chkLaunchAfterInstall.IsChecked == true)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = targetExe,
                        WorkingDirectory = installDir,
                        UseShellExecute = true
                    });
                }

                MessageBox.Show(
                    "Glance berhasil dipasang!\n\n" +
                    "• Lokasi: " + targetExe + "\n" +
                    "• Run at Startup Windows: " + (chkRunAtStartup.IsChecked == true ? "Aktif (Otomatis jalan saat Windows nyala)" : "Nonaktif") + "\n\n" +
                    "Anda juga dapat mengubah opsi Run at Startup kapan saja melalui ikon ⚙️ di Glance atau klik kanan ikon di System Tray.",
                    "Instalasi Selesai",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal memasang aplikasi: " + ex.Message, "Error Setup", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnUninstallClick(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName("Glance").Concat(Process.GetProcessesByName("DynamicIsland")))
                {
                    try { p.Kill(); p.WaitForExit(1500); } catch { }
                }

                using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
                {
                    if (runKey != null)
                    {
                        runKey.DeleteValue(AppRegName, false);
                        runKey.DeleteValue(LegacyAppRegName, false);
                    }
                }

                Registry.CurrentUser.DeleteSubKeyTree(UninstallRegistryPath, false);
                Registry.CurrentUser.DeleteSubKeyTree(LegacyUninstallRegistryPath, false);

                string desktopLnk = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "Glance.lnk"
                );
                if (File.Exists(desktopLnk)) File.Delete(desktopLnk);
                string oldDesktopLnk = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "Windows Dynamic Island.lnk"
                );
                if (File.Exists(oldDesktopLnk)) File.Delete(oldDesktopLnk);

                string startMenuLnk = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    "Glance.lnk"
                );
                if (File.Exists(startMenuLnk)) File.Delete(startMenuLnk);
                string oldStartMenuLnk = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                    "Programs",
                    "Windows Dynamic Island.lnk"
                );
                if (File.Exists(oldStartMenuLnk)) File.Delete(oldStartMenuLnk);

                string installDir = txtInstallDir.Text.Trim();
                if (Directory.Exists(installDir))
                {
                    try { Directory.Delete(installDir, true); } catch { }
                }

                progressFill.Width = progressTrack.ActualWidth;
                statusLabel.Text = "✓ Glance & Startup berhasil dihapus.";
                statusLabel.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));

                MessageBox.Show("Glance dan opsi Startup telah berhasil dihapus dari sistem.", "Uninstall Selesai", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal menghapus instalasi: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetExe, string workDir, string description)
        {
            try
            {
                IShellLink link = (IShellLink)new ShellLink();
                link.SetDescription(description);
                link.SetPath(targetExe);
                link.SetWorkingDirectory(workDir);
                link.SetIconLocation(targetExe, 0);
                IPersistFile file = (IPersistFile)link;
                file.Save(shortcutPath, false);
            }
            catch { }
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application app = new Application();
            app.Run(new SetupWindow());
        }
    }
}
