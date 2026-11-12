using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Glance
{
    /// <summary>
    /// Slide 1: System & Storage Telemetry (Battery, CPU, RAM & Partisi Penyimpanan C:, D:, dst.).
    /// </summary>
    public partial class MainWindow
    {
        private Grid BuildSystemAndDrivesSlide()
        {
            Grid g = new Grid { Margin = new Thickness(0, 1, 0, 1) };
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(7) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Top Row: 3 Columns [Baterai] [CPU] [RAM]
            Grid topRow = new Grid();
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
            topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Color colBattery = ColorFromHex(ColorBattery, Color.FromRgb(28, 231, 85));
            Color colCpu = ColorFromHex(ColorCpu, Color.FromRgb(56, 189, 248));
            Color colRam = ColorFromHex(ColorRam, Color.FromRgb(192, 132, 252));

            Border batCard = CreateTransparentStatCard("Battery", colBattery, out sysBatVal, out sysBatBar, out sysBatSub);
            Grid.SetColumn(batCard, 0);

            Border cpuCard = CreateTransparentStatCard("CPU", colCpu, out sysCpuVal, out sysCpuBar, out sysCpuSub);
            Grid.SetColumn(cpuCard, 2);

            Border ramCard = CreateTransparentStatCard("RAM", colRam, out sysRamVal, out sysRamBar, out sysRamSub);
            Grid.SetColumn(ramCard, 4);

            topRow.Children.Add(batCard);
            topRow.Children.Add(cpuCard);
            topRow.Children.Add(ramCard);
            Grid.SetRow(topRow, 0);

            drivesContainerGrid = new Grid();
            Grid.SetRow(drivesContainerGrid, 2);

            g.Children.Add(topRow);
            g.Children.Add(drivesContainerGrid);

            RebuildDrivesGrid(GetFreshDrives());
            return g;
        }

        private static List<DriveStatInfo> GetFreshDrives()
        {
            var list = new List<DriveStatInfo>();
            try
            {
                foreach (var drv in DriveInfo.GetDrives())
                {
                    if (drv.IsReady && (drv.DriveType == DriveType.Fixed || drv.DriveType == DriveType.Removable))
                    {
                        double tot = drv.TotalSize / (1024.0 * 1024 * 1024);
                        double used = (drv.TotalSize - drv.AvailableFreeSpace) / (1024.0 * 1024 * 1024);
                        int pct = tot > 0 ? (int)Math.Round((used / tot) * 100.0) : 0;
                        list.Add(new DriveStatInfo
                        {
                            Name = drv.Name,
                            Percent = pct,
                            UsedGB = used,
                            TotalGB = tot
                        });
                    }
                }
            }
            catch { }
            return list;
        }

        private Border CreateTransparentStatCard(string label, Color accent, out TextBlock valText, out Border fillBar, out TextBlock subText)
        {
            Border card = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(24, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(28, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(10, 6, 10, 6)
            };

            StackPanel sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            DockPanel top = new DockPanel();
            TextBlock lbl = new TextBlock
            {
                Text = label,
                Foreground = new SolidColorBrush(Color.FromRgb(210, 210, 220)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold
            };
            valText = new TextBlock
            {
                Text = "0%",
                Foreground = new SolidColorBrush(accent),
                FontSize = 11.5,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(valText, Dock.Right);
            top.Children.Add(valText);
            top.Children.Add(lbl);

            Border barTrack = new Border
            {
                Height = 6,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)),
                Margin = new Thickness(0, 5, 0, 5),
                ClipToBounds = true
            };
            fillBar = new Border
            {
                Height = 6,
                Width = 0,
                CornerRadius = new CornerRadius(3),
                Background = new SolidColorBrush(accent),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            barTrack.Child = fillBar;

            subText = new TextBlock
            {
                Text = "...",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 172)),
                FontSize = 10,
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            sp.Children.Add(top);
            sp.Children.Add(barTrack);
            sp.Children.Add(subText);
            card.Child = sp;
            return card;
        }

        private void RebuildDrivesGrid(List<DriveStatInfo> drives)
        {
            if (drivesContainerGrid == null) return;
            drivesContainerGrid.Children.Clear();
            drivesContainerGrid.ColumnDefinitions.Clear();
            driveValTexts.Clear();
            driveFillBars.Clear();
            driveSubTexts.Clear();

            Color diskCol = ColorFromHex(ColorDisk, Color.FromRgb(52, 211, 153));
            Color[] driveAccents =
            {
                diskCol,
                Color.FromRgb(45, 212, 191),
                Color.FromRgb(244, 114, 182),
                Color.FromRgb(96, 165, 250)
            };

            int count = (drives != null && drives.Count > 0) ? drives.Count : 1;
            double maxDriveBarWidth = (drives != null && drives.Count > 0) ? Math.Max(60.0, (290.0 / drives.Count) - 24.0) : 130.0;

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    drivesContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7) });
                }
                drivesContainerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                string drvLabel = (drives != null && i < drives.Count) ? ("Disk (" + drives[i].Name.TrimEnd('\\') + ")") : "Disk";
                Color accent = driveAccents[i % driveAccents.Length];

                TextBlock valTxt, subTxt;
                Border fillBar;
                Border card = CreateTransparentStatCard(drvLabel, accent, out valTxt, out fillBar, out subTxt);
                Grid.SetColumn(card, i * 2);
                drivesContainerGrid.Children.Add(card);

                if (drives != null && i < drives.Count)
                {
                    valTxt.Text = drives[i].Percent + "%";
                    subTxt.Text = string.Format(CultureInfo.InvariantCulture, "{0:F0} / {1:F0} GB", drives[i].UsedGB, drives[i].TotalGB);
                    fillBar.Width = Math.Max(4, maxDriveBarWidth * drives[i].Percent / 100.0);
                }

                driveValTexts.Add(valTxt);
                driveFillBars.Add(fillBar);
                driveSubTexts.Add(subTxt);
            }
        }
    }
}
