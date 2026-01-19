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
            // ここでDataContextをいじらない（親側が何を置いても壊れないように）
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e) => S.RequestPlayPause();
        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => S.RequestSeek(-5.0);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => S.RequestSeek(-1.0);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => S.RequestSeek(+1.0);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => S.RequestSeek(+5.0);

        private void Speed025_Click(object sender, RoutedEventArgs e) => S.RequestRate(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => S.RequestRate(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => S.RequestRate(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => S.RequestRate(2.0);
    }
}
