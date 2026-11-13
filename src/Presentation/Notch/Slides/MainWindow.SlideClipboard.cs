using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Glance
{
    /// <summary>
    /// Slide 3: Clipboard History Manager (Menangkap teks Ctrl+C secara otomatis & salin ulang sekali klik).
    /// </summary>
    public partial class MainWindow
    {
        private Grid BuildClipboardSlide()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(24) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            DockPanel header = new DockPanel { Margin = new Thickness(0, 0, 0, 4) };
            TextBlock title = new TextBlock
            {
                Text = "Clipboard History (Ctrl+C)",
                Foreground = new SolidColorBrush(Color.FromRgb(228, 228, 235)),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };
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
                clipList.RemoveAll(c => !c.pinned);
                SaveClipData();
                RenderClipboardList();
            };
            DockPanel.SetDock(clearBtn, Dock.Right);
            header.Children.Add(clearBtn);
            header.Children.Add(title);
            Grid.SetRow(header, 0);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            clipListPanel = new StackPanel();
            sv.Content = clipListPanel;
            Grid.SetRow(sv, 1);

            g.Children.Add(header);
            g.Children.Add(sv);
            RenderClipboardList();
            return g;
        }

        private void OnClipboardTick(object sender, EventArgs e)
        {
            if (!ShowSlideClipboard) return;
            try
            {
                if (Clipboard.ContainsText())
                {
                    string txt = (Clipboard.GetText() ?? "").Trim();
                    if (!string.IsNullOrEmpty(txt) && txt != lastClipboardText)
                    {
                        lastClipboardText = txt;
                        clipList.RemoveAll(c => c.text == txt);
                        clipList.Insert(0, new ClipItem
                        {
                            id = Guid.NewGuid().ToString("N"),
                            text = txt.Length > 300 ? txt.Substring(0, 300) : txt,
                            time = DateTime.Now.ToString("HH:mm"),
                            pinned = false
                        });
                        clipList = clipList.OrderByDescending(c => c.pinned).Take(25).ToList();
                        SaveClipData();
                        RenderClipboardList();

                        if (telemetryInitialized)
                        {
                            string preview = txt.Replace("\r", " ").Replace("\n", " ").Trim();
                            if (preview.Length > 14) preview = preview.Substring(0, 14) + "…";
                            Color acc = ColorFromHex(ColorAccent, Color.FromRgb(28, 231, 85));
                            ShowLiveActivityToast("📋", "Copied", preview, 100, false, acc, 1500);
                        }
                    }
                }
            }
            catch { }
        }

        private void RenderClipboardList()
        {
            if (clipListPanel == null) return;
            clipListPanel.Children.Clear();
            if (clipList.Count == 0)
            {
                clipListPanel.Children.Add(new TextBlock
                {
                    Text = "Copy text (Ctrl+C) to save snippets here",
                    Foreground = new SolidColorBrush(Color.FromRgb(145, 145, 155)),
                    FontSize = 11.5,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 26, 0, 0)
                });
                return;
            }

            foreach (var item in clipList)
            {
                Border row = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand,
                    ToolTip = "Click to copy back to clipboard"
                };

                DockPanel dp = new DockPanel();
                TextBlock delBtn = new TextBlock
                {
                    Text = "✕",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                    FontSize = 11,
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                delBtn.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    clipList.RemoveAll(x => x.id == item.id);
                    SaveClipData();
                    RenderClipboardList();
                };
                DockPanel.SetDock(delBtn, Dock.Right);

                TextBlock txt = new TextBlock
                {
                    Text = item.text.Replace("\r", " ").Replace("\n", " "),
                    Foreground = Brushes.White,
                    FontSize = 11.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                dp.Children.Add(delBtn);
                dp.Children.Add(txt);
                row.Child = dp;

                row.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    try
                    {
                        lastClipboardText = item.text;
                        Clipboard.SetText(item.text);
                        txt.Text = "✓ Copied to Clipboard!";
                        txt.Foreground = new SolidColorBrush(Color.FromRgb(28, 231, 85));
                    }
                    catch { }
                };

                clipListPanel.Children.Add(row);
            }
        }
    }
}
