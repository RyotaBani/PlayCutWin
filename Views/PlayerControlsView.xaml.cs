using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class PlayerControlsView : UserControl
    {
        private AppState State => AppState.Instance;

        public PlayerControlsView()
        {
            InitializeComponent();
            DataContext = State;
        }

        private void Minus05_Click(object sender, RoutedEventArgs e)
        {
            State.PlaybackSeconds = System.Math.Max(0, State.PlaybackSeconds - 0.5);
        }

        private void Plus05_Click(object sender, RoutedEventArgs e)
        {
            State.PlaybackSeconds += 0.5;
        }

        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            State.ClipStart = State.PlaybackSeconds;
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            State.ClipEnd = State.PlaybackSeconds;
        }
    }
}
