using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        private AppState State => AppState.Instance;

        public TagsView()
        {
            InitializeComponent();
            DataContext = State;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AddTag();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            State.ClearTags();
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
            var text = TagInput.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            State.AddTag(text);
            TagInput.Clear();
            TagInput.Focus();
        }
    }
}
