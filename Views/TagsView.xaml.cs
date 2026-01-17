using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        public TagsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var text = TagInput?.Text?.Trim() ?? "";
            if (text.Length == 0) return;

            AppState.Instance.AddTagToSelected(text);
            TagInput.Text = "";
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClearTagsForSelected();
        }

        private void TagsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 既存XAMLの ItemsSource が AppState.Tags を見てる想定
            // 選択要素の型は AppState.TagEntry
            if (TagsList?.SelectedItem is AppState.TagEntry entry)
            {
                AppState.Instance.RemoveSelectedTag(entry);
            }
        }
    }
}
