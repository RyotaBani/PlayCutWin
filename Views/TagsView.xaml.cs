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

            UpdateSelected();
            AppState.Current.PropertyChanged += Current_PropertyChanged;
        }

        private void Current_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SelectedVideo) ||
                e.PropertyName == nameof(AppState.SelectedVideoDisplay))
            {
                UpdateSelected();
            }
        }

        private void UpdateSelected()
        {
            var sel = AppState.Current.SelectedVideo;
            SelectedPathText.Text = sel == null ? "(none)" : sel.Path;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var sel = AppState.Current.SelectedVideo;
            if (sel == null)
            {
                MessageBox.Show("先に Clips で動画を選択してね。", "PlayCut",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var tag = TagInput.Text?.Trim();
            if (string.IsNullOrEmpty(tag))
            {
                InfoText.Text = "タグが空です。";
                return;
            }

            // まだタグモデルは作ってないので表示だけ
            InfoText.Text = $"(dummy) Added tag '{tag}' to {sel.Name}";
            TagInput.Text = "";
        }
    }
}
