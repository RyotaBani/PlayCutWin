using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        public TagsView()
        {
            InitializeComponent();
            Refresh();

            PlayCutWin.AppState.Current.PropertyChanged += OnStateChanged;
        }

        private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideo) ||
                e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideoPath))
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var path = PlayCutWin.AppState.Current.SelectedVideoPath;
            SelectedPathText.Text = string.IsNullOrWhiteSpace(path) ? "(none)" : path;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var tag = TagInput.Text?.Trim() ?? "";
            if (tag.Length == 0)
            {
                MessageBox.Show("タグを入力してね（仮）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var selected = PlayCutWin.AppState.Current.SelectedVideoName;
            MessageBox.Show($"(dummy)\nTag: {tag}\nSelected: {selected}", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
            TagInput.Text = "";
        }
    }
}
