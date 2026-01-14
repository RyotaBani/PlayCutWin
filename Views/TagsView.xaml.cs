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

            MessageBox.Show($"Add Tag (dummy): {text}\n（将来：選択動画にタグ付けして保存）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
            TagInput.Clear();
        }
    }
}
