using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Windows.Point;

namespace Glance
{
    /// <summary>
    /// Slide 5: Drop Shelf (Area penampungan file sementara untuk Drag & Drop antar aplikasi).
    /// </summary>
    public partial class MainWindow
    {
        private Grid BuildShelfSlide()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            DockPanel header = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };

            StackPanel titleSp = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            TextBlock title = new TextBlock
            {
                Text = "Drop Shelf (File Staging)",
                Foreground = new SolidColorBrush(Color.FromRgb(228, 228, 235)),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            shelfCountText = new TextBlock
            {
                Text = "",
                Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(6, 0, 0, 0)
            };
            titleSp.Children.Add(title);
            titleSp.Children.Add(shelfCountText);

            Border clearBtn = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 2, 8, 2),
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            TextBlock clearTxt = new TextBlock
            {
                Text = "Clear All",
                Foreground = new SolidColorBrush(Color.FromRgb(210, 210, 220)),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold
            };
            clearBtn.Child = clearTxt;
            clearBtn.MouseLeftButtonUp += (s, e) =>
            {
                e.Handled = true;
                shelfList.Clear();
                SaveShelfData();
                RenderShelfList();
            };
            DockPanel.SetDock(clearBtn, Dock.Right);
            header.Children.Add(clearBtn);
            header.Children.Add(titleSp);
            Grid.SetRow(header, 0);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            shelfListPanel = new StackPanel();
            sv.Content = shelfListPanel;
            Grid.SetRow(sv, 1);

            g.AllowDrop = true;
            g.Drop += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    AddFilesToShelf(files);
                }
            };

            g.Children.Add(header);
            g.Children.Add(sv);
            RenderShelfList();
            return g;
        }

        private void AddFilesToShelf(string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0) return;
            foreach (string path in filePaths)
            {
                if (string.IsNullOrWhiteSpace(path)) continue;
                shelfList.RemoveAll(x => string.Equals(x.filePath, path, StringComparison.OrdinalIgnoreCase));

                long size = 0;
                try { if (File.Exists(path)) size = new FileInfo(path).Length; } catch { }

                shelfList.Insert(0, new ShelfItem
                {
                    id = Guid.NewGuid().ToString("N"),
                    filePath = path,
                    fileName = System.IO.Path.GetFileName(path),
                    fileSizeBytes = size,
                    timeAdded = DateTime.Now.ToString("HH:mm")
                });
            }

            if (shelfList.Count > 30) shelfList = shelfList.Take(30).ToList();
            SaveShelfData();
            RenderShelfList();
        }

        private void RenderShelfList()
        {
            if (shelfListPanel == null) return;
            shelfListPanel.Children.Clear();

            if (shelfCountText != null)
            {
                shelfCountText.Text = shelfList.Count > 0 ? string.Format("({0} files)", shelfList.Count) : "";
            }

            if (shelfList.Count == 0)
            {
                Border emptyZone = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(15, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(14, 20, 14, 20),
                    Margin = new Thickness(0, 10, 0, 0),
                    Cursor = Cursors.Hand
                };
                StackPanel sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                TextBlock iconTb = new TextBlock
                {
                    Text = "📥",
                    FontSize = 26,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 6)
                };
                TextBlock t1 = new TextBlock
                {
                    Text = "Drag & Drop files here",
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                TextBlock t2 = new TextBlock
                {
                    Text = "Files are held here for quick staging and dragging into other apps",
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 150)),
                    FontSize = 10.5,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                sp.Children.Add(iconTb);
                sp.Children.Add(t1);
                sp.Children.Add(t2);
                emptyZone.Child = sp;
                shelfListPanel.Children.Add(emptyZone);
                return;
            }

            foreach (var item in shelfList)
            {
                var curItem = item;
                Border row = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand,
                    ToolTip = curItem.filePath + "\n(Drag to drop into other apps)"
                };

                Grid rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                Image iconImg = new Image
                {
                    Width = 18,
                    Height = 18,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                ImageSource src = GetFileIcon(curItem.filePath);
                if (src != null)
                {
                    iconImg.Source = src;
                }
                Grid.SetColumn(iconImg, 0);

                StackPanel textSp = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 6, 0)
                };
                TextBlock nameTb = new TextBlock
                {
                    Text = curItem.fileName,
                    Foreground = Brushes.White,
                    FontSize = 11.5,
                    FontWeight = FontWeights.SemiBold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                TextBlock sizeTb = new TextBlock
                {
                    Text = FormatFileSize(curItem.fileSizeBytes) + " • " + curItem.timeAdded,
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 140, 152)),
                    FontSize = 10
                };
                textSp.Children.Add(nameTb);
                textSp.Children.Add(sizeTb);
                Grid.SetColumn(textSp, 1);

                StackPanel actSp = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock openBtn = new TextBlock
                {
                    Text = "📂",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 180)),
                    Margin = new Thickness(0, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Open containing folder"
                };
                openBtn.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    try
                    {
                        if (File.Exists(curItem.filePath))
                        {
                            Process.Start("explorer.exe", "/select,\"" + curItem.filePath + "\"");
                        }
                    }
                    catch { }
                };

                TextBlock delBtn = new TextBlock
                {
                    Text = "✕",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 170)),
                    Cursor = Cursors.Hand,
                    ToolTip = "Remove from Shelf"
                };
                delBtn.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    shelfList.RemoveAll(x => x.id == curItem.id);
                    SaveShelfData();
                    RenderShelfList();
                };

                actSp.Children.Add(openBtn);
                actSp.Children.Add(delBtn);
                Grid.SetColumn(actSp, 2);

                rowGrid.Children.Add(iconImg);
                rowGrid.Children.Add(textSp);
                rowGrid.Children.Add(actSp);
                row.Child = rowGrid;

                Point dragStartPoint = new Point(0, 0);
                bool isRowMouseDown = false;

                row.PreviewMouseLeftButtonDown += (s, ev) =>
                {
                    dragStartPoint = ev.GetPosition(null);
                    isRowMouseDown = true;
                };

                row.PreviewMouseMove += (s, ev) =>
                {
                    if (isRowMouseDown && ev.LeftButton == MouseButtonState.Pressed)
                    {
                        Point currentPoint = ev.GetPosition(null);
                        Vector diff = dragStartPoint - currentPoint;
                        if (Math.Abs(diff.X) > 4 || Math.Abs(diff.Y) > 4)
                        {
                            isRowMouseDown = false;
                            try
                            {
                                if (File.Exists(curItem.filePath))
                                {
                                    DataObject data = new DataObject(DataFormats.FileDrop, new string[] { curItem.filePath });
                                    DragDrop.DoDragDrop(row, data, DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);
                                }
                            }
                            catch { }
                        }
                    }
                };

                row.PreviewMouseLeftButtonUp += (s, ev) =>
                {
                    isRowMouseDown = false;
                };

                shelfListPanel.Children.Add(row);
            }
        }

        private static ImageSource GetFileIcon(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            string ext = System.IO.Path.GetExtension(path) ?? "";
            lock (fileIconCache)
            {
                if (fileIconCache.ContainsKey(ext)) return fileIconCache[ext];
            }

            try
            {
                if (File.Exists(path))
                {
                    using (var icon = System.Drawing.Icon.ExtractAssociatedIcon(path))
                    {
                        if (icon != null)
                        {
                            var bmp = Imaging.CreateBitmapSourceFromHIcon(
                                icon.Handle,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions()
                            );
                            bmp.Freeze();
                            lock (fileIconCache)
                            {
                                fileIconCache[ext] = bmp;
                            }
                            return bmp;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024) return kb.ToString("F1", CultureInfo.InvariantCulture) + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024) return mb.ToString("F1", CultureInfo.InvariantCulture) + " MB";
            double gb = mb / 1024.0;
            return gb.ToString("F2", CultureInfo.InvariantCulture) + " GB";
        }
    }
}
