using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        public TagsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddFromInput();
                e.Handled = true;
            }
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            AddFromInput();
        }

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button b)
                {
                    var text = b.Tag?.ToString() ?? b.Content?.ToString();
                    if (string.IsNullOrWhiteSpace(text)) return;
                    AddTag(text.Trim());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = AppState.Instance;
                if (TagsGrid.SelectedItem is TagEntry te)
                    app.Tags.Remove(te);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppState.Instance.ClearTagsForSelected();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AddFromInput()
        {
            var text = TagInput.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("タグを入れてね（仮）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AddTag(text);
            TagInput.Text = "";
            TagInput.Focus();
        }

        private void AddTag(string text)
        {
            var app = AppState.Instance;

            if (string.IsNullOrWhiteSpace(app.SelectedVideoPath))
            {
                MessageBox.Show("先に Clips で動画を選択してね（仮）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sec = Math.Max(0, app.PlaybackSeconds);
            app.AddTag(app.SelectedVideoPath, sec, text);

            if (app.Tags.Count > 0)
            {
                var last = app.Tags.LastOrDefault();
                if (last != null)
                {
                    TagsGrid.SelectedItem = last;
                    TagsGrid.ScrollIntoView(last);
                }
            }
        }
    }
}
