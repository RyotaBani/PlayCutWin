using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class PlayerControlsView : UserControl
    {
        private AppState S => AppState.Instance;

        public PlayerControlsView()
        {
            InitializeComponent();
            DataContext = S;
        }

        private void Minus05_Click(object sender, RoutedEventArgs e)
        {
            S.PlaybackSeconds = System.Math.Max(0, S.PlaybackSeconds - 0.5);
        }

        private void Plus05_Click(object sender, RoutedEventArgs e)
        {
            S.PlaybackSeconds = S.PlaybackSeconds + 0.5;
        }

        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            S.ClipStart = S.PlaybackSeconds;
            S.StatusMessage = "Start set";
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            S.ClipEnd = S.PlaybackSeconds;
            S.StatusMessage = "End set";
        }

        private void ResetRange_Click(object sender, RoutedEventArgs e)
        {
            S.ResetRange();
        }
    }
}
