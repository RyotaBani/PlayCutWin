using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        AppState state = AppState.Instance;

        public TagsView()
        {
            InitializeComponent();
            DataContext = state;
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                state.AddTag(((TextBox)sender).Text);
                ((TextBox)sender).Text = "";
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
            => state.ClearTagsForSelected();
    }
}
