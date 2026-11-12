using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Glance
{
    /// <summary>
    /// Slide 4: Quick Notes / Scratchpad (Catatan cepat & daftar tugas To-Do).
    /// </summary>
    public partial class MainWindow
    {
        private Grid BuildNotesSlide()
        {
            Grid g = new Grid();
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });
            g.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            Grid inputRow = new Grid { Margin = new Thickness(0, 0, 0, 5) };
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
            inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });

            noteInputBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Foreground = Brushes.White,
                CaretBrush = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(7, 3, 7, 3),
                FontSize = 11.5,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            noteInputBox.GotFocus += (s, e) => isInputFocused = true;
            noteInputBox.LostFocus += (s, e) => isInputFocused = false;
            noteInputBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    AddNoteFromInput();
                }
            };
            Grid.SetColumn(noteInputBox, 0);

            Border addBtn = new Border
            {
                Width = 26,
                Height = 25,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.FromRgb(28, 231, 85)),
                Cursor = Cursors.Hand
            };
            addBtn.Child = new TextBlock
            {
                Text = "+",
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold,
                FontSize = 15,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            };
            addBtn.MouseLeftButtonUp += (s, e) => { e.Handled = true; AddNoteFromInput(); };
            Grid.SetColumn(addBtn, 2);

            inputRow.Children.Add(noteInputBox);
            inputRow.Children.Add(addBtn);
            Grid.SetRow(inputRow, 0);

            ScrollViewer sv = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
            notesListPanel = new StackPanel();
            sv.Content = notesListPanel;
            Grid.SetRow(sv, 1);

            g.Children.Add(inputRow);
            g.Children.Add(sv);
            RenderNotesList();
            return g;
        }

        private void AddNoteFromInput()
        {
            string val = (noteInputBox.Text ?? "").Trim();
            if (string.IsNullOrEmpty(val)) return;
            noteInputBox.Text = "";
            notesList.Insert(0, new NoteItem
            {
                id = Guid.NewGuid().ToString("N"),
                text = val,
                done = false
            });
            SaveNotesData();
            RenderNotesList();
        }

        private void RenderNotesList()
        {
            if (notesListPanel == null) return;
            notesListPanel.Children.Clear();
            if (notesList.Count == 0)
            {
                notesListPanel.Children.Add(new TextBlock
                {
                    Text = "No quick notes yet",
                    Foreground = new SolidColorBrush(Color.FromRgb(145, 145, 155)),
                    FontSize = 11.5,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(0, 22, 0, 0)
                });
                return;
            }

            foreach (var note in notesList)
            {
                Border row = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(42, 255, 255, 255)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 5, 8, 5),
                    Margin = new Thickness(0, 0, 0, 5),
                    Cursor = Cursors.Hand
                };

                DockPanel dp = new DockPanel();
                TextBlock checkTxt = new TextBlock
                {
                    Text = note.done ? "☑" : "☐",
                    Foreground = note.done
                        ? new SolidColorBrush(Color.FromRgb(28, 231, 85))
                        : new SolidColorBrush(Color.FromRgb(180, 180, 190)),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 7, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                DockPanel.SetDock(checkTxt, Dock.Left);

                TextBlock delTxt = new TextBlock
                {
                    Text = "✕",
                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 160)),
                    FontSize = 11,
                    Margin = new Thickness(6, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                delTxt.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    notesList.RemoveAll(n => n.id == note.id);
                    SaveNotesData();
                    RenderNotesList();
                };
                DockPanel.SetDock(delTxt, Dock.Right);

                TextBlock bodyTxt = new TextBlock
                {
                    Text = note.text,
                    Foreground = note.done
                        ? new SolidColorBrush(Color.FromRgb(140, 140, 150))
                        : Brushes.White,
                    TextDecorations = note.done ? TextDecorations.Strikethrough : null,
                    FontSize = 11.5,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                dp.Children.Add(checkTxt);
                dp.Children.Add(delTxt);
                dp.Children.Add(bodyTxt);
                row.Child = dp;

                row.MouseLeftButtonUp += (s, ev) =>
                {
                    ev.Handled = true;
                    note.done = !note.done;
                    SaveNotesData();
                    RenderNotesList();
                };

                notesListPanel.Children.Add(row);
            }
        }
    }
}
