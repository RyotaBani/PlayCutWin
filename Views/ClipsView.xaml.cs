using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        AppState state = AppState.Instance;

        public ClipsView()
        {
            InitializeComponent();
            DataContext = state;
        }

        private void Clips_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            state.SelectedClip = (VideoItem)((ListBox)sender).SelectedItem;
            state.PlaybackSeconds = 0;
        }

        private void Plus05_Click(object sender, RoutedEventArgs e)
            => state.PlaybackSeconds += 0.5;

        private void Minus05_Click(object sender, RoutedEventArgs e)
            => state.PlaybackSeconds = Math.Max(0, state.PlaybackSeconds - 0.5);

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => state.PlaybackSeconds = e.NewValue;
    }
}
