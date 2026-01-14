using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        public TagsView()
        {
            InitializeComponent();
            DataContext = AppState.Current;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var text = TagInput.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("タグを入れてね（仮）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(AppState.Current.SelectedVideoPath))
            {
                MessageBox.Show("先に Clips で動画を選択してね", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AppState.Current.AddTagToSelectedVideo(text);
            TagInput.Clear();
        }
    }
}
