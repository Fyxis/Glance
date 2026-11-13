using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Glance
{
    /// <summary>
    /// Jendela pop-up notifikasi pembaruan otomatis ("Versi baru tersedia, klik Update jika ingin update")
    /// dengan tombol Update dan Cancel serta indikator progress download.
    /// </summary>
    public class UpdateNotificationWindow : Window
    {
        private static UpdateNotificationWindow activeInstance;

        private readonly AppVersionManifest localManifest;
        private readonly AppVersionManifest remoteManifest;
        private CancellationTokenSource downloadCts;
        private bool isDownloading = false;

        private TextBlock statusText;
        private Border progressContainer;
        private Border progressTrack;
        private Border progressFill;
        private Button btnUpdate;
        private Button btnCancel;

        public static void ShowOrActivate(AppVersionManifest local, AppVersionManifest remote)
        {
            if (remote == null) return;

            if (activeInstance != null && activeInstance.IsLoaded)
            {
                activeInstance.Activate();
                return;
            }

            activeInstance = new UpdateNotificationWindow(local, remote);
            activeInstance.Closed += (s, e) => activeInstance = null;
            activeInstance.Show();
            activeInstance.Activate();
        }

        public UpdateNotificationWindow(AppVersionManifest local, AppVersionManifest remote)
        {
            localManifest = local ?? new AppVersionManifest();
            remoteManifest = remote ?? new AppVersionManifest();

            Title = "Glance — Pembaruan Tersedia";
            Width = 420;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;

            BuildUI();
        }

        private void BuildUI()
        {
            Color accentColor = Color.FromRgb(28, 231, 85);
            if (MainWindow.PrimaryInstance != null && !string.IsNullOrEmpty(MainWindow.PrimaryInstance.ColorAccent))
            {
                accentColor = MainWindow.ColorFromHex(MainWindow.PrimaryInstance.ColorAccent, accentColor);
            }

            Border outer = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(15, 15, 20)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(95, accentColor.R, accentColor.G, accentColor.B)),
                BorderThickness = new Thickness(1.2),
                CornerRadius = new CornerRadius(18),
                Margin = new Thickness(16),
                Padding = new Thickness(22, 18, 22, 18),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 26,
                    ShadowDepth = 6,
                    Opacity = 0.65,
                    Color = Colors.Black
                }
            };

            outer.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ButtonState == MouseButtonState.Pressed) DragMove();
            };

            StackPanel root = new StackPanel();

            // 1. Header Row (Icon + Title + Version Badge)
            DockPanel headerRow = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };

            Border verBadge = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, accentColor.R, accentColor.G, accentColor.B)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(120, accentColor.R, accentColor.G, accentColor.B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(9, 3, 9, 3),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            verBadge.Child = new TextBlock
            {
                Text = $"v{localManifest.Version} ➔ v{remoteManifest.Version}",
                Foreground = new SolidColorBrush(accentColor),
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            DockPanel.SetDock(verBadge, Dock.Right);
            headerRow.Children.Add(verBadge);

            StackPanel titleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border iconCircle = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(35, accentColor.R, accentColor.G, accentColor.B)),
                Margin = new Thickness(0, 0, 10, 0)
            };
            iconCircle.Child = new TextBlock
            {
                Text = "🚀",
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            titleRow.Children.Add(iconCircle);

            titleRow.Children.Add(new TextBlock
            {
                Text = "Glance Auto-Update",
                Foreground = Brushes.White,
                FontSize = 15.5,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            });

            headerRow.Children.Add(titleRow);
            root.Children.Add(headerRow);

            // 2. Main Message Box ("Versi baru tersedia, klik Update jika ingin update")
            Border msgCard = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(22, 22, 29)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(42, 42, 52)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 12, 14, 12),
                Margin = new Thickness(0, 0, 0, 14)
            };

            StackPanel msgStack = new StackPanel();
            msgStack.Children.Add(new TextBlock
            {
                Text = "Versi baru tersedia, klik Update jika ingin update.",
                Foreground = Brushes.White,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            });

            if (!string.IsNullOrWhiteSpace(remoteManifest.ReleaseNotes))
            {
                msgStack.Children.Add(new TextBlock
                {
                    Text = "Catatan pembaruan: " + remoteManifest.ReleaseNotes,
                    Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 180)),
                    FontSize = 11.5,
                    Margin = new Thickness(0, 6, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }

            if (!string.IsNullOrWhiteSpace(remoteManifest.PublishedAt))
            {
                msgStack.Children.Add(new TextBlock
                {
                    Text = "Dirilis pada: " + remoteManifest.PublishedAt,
                    Foreground = new SolidColorBrush(Color.FromRgb(125, 125, 140)),
                    FontSize = 10.5,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            msgCard.Child = msgStack;
            root.Children.Add(msgCard);

            // 3. Download Progress Area (Hidden until Update is clicked)
            progressContainer = new Border
            {
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 0, 14)
            };
            StackPanel progStack = new StackPanel();

            statusText = new TextBlock
            {
                Text = "Mengunduh pembaruan dari GitHub... 0%",
                Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 205)),
                FontSize = 11.5,
                Margin = new Thickness(0, 0, 0, 6)
            };
            progStack.Children.Add(statusText);

            progressTrack = new Border
            {
                Height = 7,
                CornerRadius = new CornerRadius(3.5),
                Background = new SolidColorBrush(Color.FromRgb(34, 34, 44)),
                ClipToBounds = true
            };
            progressFill = new Border
            {
                Height = 7,
                Width = 0,
                CornerRadius = new CornerRadius(3.5),
                Background = new SolidColorBrush(accentColor),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            progressTrack.Child = progressFill;
            progStack.Children.Add(progressTrack);

            progressContainer.Child = progStack;
            root.Children.Add(progressContainer);

            // 4. Action Buttons: Cancel & Update
            Grid btnGrid = new Grid();
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            btnGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.3, GridUnitType.Star) });

            btnCancel = new Button
            {
                Content = "Cancel",
                Height = 38,
                Background = new SolidColorBrush(Color.FromRgb(34, 34, 42)),
                Foreground = new SolidColorBrush(Color.FromRgb(215, 215, 225)),
                BorderThickness = new Thickness(0),
                FontSize = 12.5,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += OnCancelClicked;
            Grid.SetColumn(btnCancel, 0);

            btnUpdate = new Button
            {
                Content = "Update",
                Height = 38,
                Background = new SolidColorBrush(accentColor),
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };
            btnUpdate.Click += OnUpdateClicked;
            Grid.SetColumn(btnUpdate, 2);

            btnGrid.Children.Add(btnCancel);
            btnGrid.Children.Add(btnUpdate);
            root.Children.Add(btnGrid);

            outer.Child = root;
            Content = outer;
        }

        private async void OnUpdateClicked(object sender, RoutedEventArgs e)
        {
            if (isDownloading) return;
            isDownloading = true;

            btnUpdate.IsEnabled = false;
            btnUpdate.Content = "Mengunduh...";
            progressContainer.Visibility = Visibility.Visible;
            statusText.Foreground = new SolidColorBrush(Color.FromRgb(190, 190, 205));
            statusText.Text = "Menghubungkan ke GitHub & mengunduh versi terbaru...";

            downloadCts = new CancellationTokenSource();

            try
            {
                await GitHubUpdateService.DownloadAndApplyUpdateAsync(
                    remoteManifest,
                    pct =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            double trackW = progressTrack.ActualWidth > 0 ? progressTrack.ActualWidth : 340;
                            progressFill.Width = Math.Max(0, Math.Min(trackW, trackW * (pct / 100.0)));
                            statusText.Text = $"Mengunduh pembaruan dari GitHub... {pct}%";
                            btnUpdate.Content = $"Downloading {pct}%";
                        }));
                    },
                    downloadCts.Token
                );
            }
            catch (OperationCanceledException)
            {
                isDownloading = false;
                btnUpdate.IsEnabled = true;
                btnUpdate.Content = "Update";
                statusText.Text = "Pengunduhan dibatalkan.";
            }
            catch (Exception ex)
            {
                isDownloading = false;
                btnUpdate.IsEnabled = true;
                btnUpdate.Content = "Update";
                statusText.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
                statusText.Text = "Gagal mengunduh update: " + ex.Message;
            }
        }

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            if (isDownloading && downloadCts != null)
            {
                try { downloadCts.Cancel(); } catch { }
            }
            GitHubUpdateService.DismissBuildForSession(remoteManifest.BuildNumber);
            Close();
        }
    }
}
