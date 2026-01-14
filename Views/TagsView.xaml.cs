using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PlayCutWin;

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
            var tag = TagInput.Text ?? "";
            AppState.Current.AddTag(tag);
            TagInput.Text = "";
            TagInput.Focus();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // ダブルクリックで削除（小さく便利）
        private void TagsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TagsList.SelectedItem is string tag)
            {
                var result = MessageBox.Show(
                    $"Remove tag?\n\n{tag}",
                    "Tags",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    AppState.Current.RemoveTag(tag);
                }
            }
        }
    }
}
