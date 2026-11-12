using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Path = System.Windows.Shapes.Path;
using Point = System.Windows.Point;

namespace Glance
{
    /// <summary>
    /// Slide 2: Control Center (Status Wi-Fi, Kecepatan Internet, Slider Volume Audio & Slider Kecerahan Layar).
    /// </summary>
    public partial class MainWindow
    {
        private Grid BuildControlCenterSlide()
        {
            Grid g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(44) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

            Border wifiCard = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 6, 12, 6),
                Cursor = Cursors.Hand,
                ToolTip = "Click to open Windows Wi-Fi Settings"
            };
            wifiCard.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                try
                {
                    Process.Start(new ProcessStartInfo("ms-availablenetworks:") { UseShellExecute = true });
                }
                catch
                {
                    try { Process.Start(new ProcessStartInfo("ms-settings:network-wifi") { UseShellExecute = true }); } catch { }
                }
            };

            DockPanel wifiDock = new DockPanel { VerticalAlignment = VerticalAlignment.Center };

            StackPanel speedStack = new StackPanel
            {
                Width = 96,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Real-time network speed (Download / Upload)"
            };
            netDownText = new TextBlock
            {
                Text = "↓ 0 KB/s",
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };
            netUpText = new TextBlock
            {
                Text = "↑ 0 KB/s",
                Foreground = new SolidColorBrush(Color.FromRgb(251, 146, 60)),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right,
                Margin = new Thickness(0, 1, 0, 0)
            };
            speedStack.Children.Add(netDownText);
            speedStack.Children.Add(netUpText);
            DockPanel.SetDock(speedStack, Dock.Right);

            Color colWifi = ColorFromHex(ColorWifi, Color.FromRgb(16, 185, 129));
            wifiSignalPill = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(45, colWifi.R, colWifi.G, colWifi.B)),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5, 0, 5, 0),
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            wifiSubText = new TextBlock
            {
                Text = "0%",
                Foreground = new SolidColorBrush(colWifi),
                FontSize = 9.5,
                FontWeight = FontWeights.Bold
            };
            wifiSignalPill.Child = wifiSubText;

            StackPanel wifiLeft = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            Border wifiIconCircle = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(14),
                Background = new SolidColorBrush(Color.FromArgb(55, 56, 189, 248)),
                Margin = new Thickness(0, 0, 10, 0)
            };
            Path wifiIconPath = new Path
            {
                Data = Geometry.Parse("M 1,9 L 3,11 C 7.97,6.03 16.03,6.03 21,11 L 23,9 C 16.93,2.93 7.08,2.93 1,9 Z M 9,17 L 12,20 L 15,17 C 13.35,15.34 10.66,15.34 9,17 Z M 5,13 L 7,15 C 9.76,12.24 14.24,12.24 17,15 L 19,13 C 15.14,9.14 8.87,9.14 5,13 Z"),
                Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                Width = 15,
                Height = 15,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            wifiIconCircle.Child = wifiIconPath;

            StackPanel wifiTextStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            StackPanel wifiLabelRow = new StackPanel { Orientation = Orientation.Horizontal };
            wifiLabelRow.Children.Add(new TextBlock
            {
                Text = "Wi-Fi",
                Foreground = new SolidColorBrush(Color.FromRgb(165, 165, 175)),
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Center
            });
            wifiLabelRow.Children.Add(wifiSignalPill);
            wifiTextStack.Children.Add(wifiLabelRow);
            wifiSsidText = new TextBlock
            {
                Text = "Checking...",
                Foreground = Brushes.White,
                FontSize = 12.5,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 150
            };
            wifiTextStack.Children.Add(wifiSsidText);

            wifiLeft.Children.Add(wifiIconCircle);
            wifiLeft.Children.Add(wifiTextStack);

            wifiDock.Children.Add(speedStack);
            wifiDock.Children.Add(wifiLeft);
            wifiCard.Child = wifiDock;
            Grid.SetRow(wifiCard, 0);

            Color colSound = ColorFromHex(ColorSound, Color.FromRgb(56, 189, 248));
            Border soundCard = CreateSliderControlCard(
                "🔊",
                "Sound",
                colSound,
                out soundIconText,
                out soundTrackBorder,
                out soundFillBorder,
                out soundValText
            );
            soundIconText.Cursor = Cursors.Hand;
            soundIconText.ToolTip = "Click to Mute / Unmute Audio";
            soundIconText.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                Task.Run(() => HardwareController.ToggleAudioMute());
            };
            AttachInteractiveSlider(soundTrackBorder, soundFillBorder, soundValText, true);
            Grid.SetRow(soundCard, 2);

            Color colBright = ColorFromHex(ColorBrightness, Color.FromRgb(251, 191, 36));
            TextBlock dummyBrightIcon;
            Border brightCard = CreateSliderControlCard(
                "☀",
                "Brightness",
                colBright,
                out dummyBrightIcon,
                out brightTrackBorder,
                out brightFillBorder,
                out brightValText
            );
            AttachInteractiveSlider(brightTrackBorder, brightFillBorder, brightValText, false);
            Grid.SetRow(brightCard, 4);

            g.Children.Add(wifiCard);
            g.Children.Add(soundCard);
            g.Children.Add(brightCard);
            return g;
        }

        private Border CreateSliderControlCard(
            string iconChar,
            string titleLabel,
            Color accent,
            out TextBlock iconTextBlock,
            out Border trackBorder,
            out Border fillBorder,
            out TextBlock percentTextBlock)
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(12, 5, 12, 5)
            };

            Grid row = new Grid { VerticalAlignment = VerticalAlignment.Center };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });

            iconTextBlock = new TextBlock
            {
                Text = iconChar,
                Foreground = new SolidColorBrush(accent),
                FontSize = 13.5,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(iconTextBlock, 0);

            TextBlock lbl = new TextBlock
            {
                Text = titleLabel,
                Foreground = new SolidColorBrush(Color.FromRgb(220, 220, 230)),
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(lbl, 1);

            trackBorder = new Border
            {
                Height = 11,
                CornerRadius = new CornerRadius(5.5),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                VerticalAlignment = VerticalAlignment.Center,
                Cursor = Cursors.Hand,
                ClipToBounds = true
            };
            fillBorder = new Border
            {
                Height = 11,
                Width = 0,
                CornerRadius = new CornerRadius(5.5),
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            trackBorder.Child = fillBorder;
            Grid.SetColumn(trackBorder, 2);

            percentTextBlock = new TextBlock
            {
                Text = "50%",
                Foreground = new SolidColorBrush(accent),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(percentTextBlock, 4);

            row.Children.Add(iconTextBlock);
            row.Children.Add(lbl);
            row.Children.Add(trackBorder);
            row.Children.Add(percentTextBlock);
            card.Child = row;
            return card;
        }

        private void AttachInteractiveSlider(Border track, Border fill, TextBlock valLabel, bool isAudioSlider)
        {
            Action<MouseEventArgs> updateFromMouse = (e) =>
            {
                if (track.ActualWidth <= 0) return;
                Point pt = e.GetPosition(track);
                double ratio = Math.Max(0.0, Math.Min(1.0, pt.X / track.ActualWidth));
                int pct = (int)Math.Round(ratio * 100.0);
                fill.Width = track.ActualWidth * ratio;
                valLabel.Text = pct + "%";

                if (isAudioSlider)
                {
                    Task.Run(() => HardwareController.SetAudioVolume(pct));
                }
                else
                {
                    Task.Run(() => HardwareController.SetScreenBrightness(pct));
                }
            };

            track.MouseLeftButtonDown += (s, e) =>
            {
                e.Handled = true;
                if (isAudioSlider) isDraggingSound = true;
                else isDraggingBright = true;
                track.CaptureMouse();
                updateFromMouse(e);
            };

            track.MouseMove += (s, e) =>
            {
                if ((isAudioSlider && isDraggingSound) || (!isAudioSlider && isDraggingBright))
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        updateFromMouse(e);
                    }
                }
            };

            track.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                if (isAudioSlider) isDraggingSound = false;
                else isDraggingBright = false;
                track.ReleaseMouseCapture();
            };
        }
    }
}
