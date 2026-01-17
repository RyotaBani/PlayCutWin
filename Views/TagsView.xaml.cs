using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        private AppState S => AppState.Instance;

        public TagsView()
        {
            InitializeComponent();
            DataContext = S;

            TagsList.MouseDoubleClick += TagsList_MouseDoubleClick;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddTag();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            S.ClearTagsForSelected();
            TagInput.Focus();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag();
                e.Handled = true;
            }
        }

        private void AddTag()
        {
            var text = TagInput.Text?.Trim() ?? "";
            if (text.Length == 0) return;

            S.AddTagToSelected(text);
            TagInput.Clear();
            TagInput.Focus();
        }

        private void TagsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TagsList.SelectedItem is TagEntry entry)
            {
                S.RemoveSelectedTag(entry);
            }
        }
    }
}
