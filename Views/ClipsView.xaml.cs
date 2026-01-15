using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        public ClipsView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                PlayCutWin.AppState.Instance.SetSelected(item.Path);
            }
        }
    }
}
