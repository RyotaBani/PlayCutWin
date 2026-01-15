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
            DataContext = PlayCutWin.AppState.Instance;
        }

        // Add Tag button
        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            AddTagFromInput();
        }

        // Enter key in input
        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTagFromInput();
                e.Handled = true;
            }
        }

        // Preset buttons (Content is the tag name)
        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                var text = (b.Content?.ToString() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(text)) return;

                PlayCutWin.AppState.Instance.AddTagAtCurrentPosition(text);
            }
        }

        // Clear pending tags
        private void ClearPending_Click(object sender, RoutedEventArgs e)
        {
            PlayCutWin.AppState.Instance.ClearPendingTags();
            PlayCutWin.AppState.Instance.StatusMessage = "Pending tags cleared.";
            TagInput.Focus();
        }

        private void AddTagFromInput()
        {
            var text = (TagInput.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("タグを入力してね", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ✅ 現在の再生位置で Pendingタグに追加
            PlayCutWin.AppState.Instance.AddTagAtCurrentPosition(text);

            TagInput.Text = "";
            TagInput.Focus();
        }
    }
}
