using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        public TagsView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            AddTagFromInput();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTagFromInput();
                e.Handled = true;
            }
        }

        private void AddTagFromInput()
        {
            var text = TagInput.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("タグを入力してね（仮）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PlayCutWin.AppState.Instance.AddTag(text);
            TagInput.Text = "";
            TagInput.Focus();
        }
    }
}
