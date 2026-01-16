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

            // App全体の状態をこの画面にバインド
            DataContext = AppState.Current;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var text = TagInput.Text ?? "";
            AppState.Current.AddTagForSelected(text);
            TagInput.Clear();
            TagInput.Focus();
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            AppState.Current.ClearTagsForSelected();
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
    }
}
