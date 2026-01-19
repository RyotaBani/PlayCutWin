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

        // --- Speed ---
        private void Rate025_Click(object sender, RoutedEventArgs e) => S.SendRate(0.25);
        private void Rate05_Click(object sender, RoutedEventArgs e)  => S.SendRate(0.5);
        private void Rate1_Click(object sender, RoutedEventArgs e)   => S.SendRate(1.0);
        private void Rate2_Click(object sender, RoutedEventArgs e)   => S.SendRate(2.0);

        // --- Seek ---
        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => S.SendSeekRelative(-5);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => S.SendSeekRelative(-1);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e)  => S.SendSeekRelative(+1);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e)  => S.SendSeekRelative(+5);

        // --- Playback ---
        private void PlayPause_Click(object sender, RoutedEventArgs e) => S.SendPlayPause();
        private void Stop_Click(object sender, RoutedEventArgs e) => S.SendStop();

        // --- Clip Range ---
        private void ClipStart_Click(object sender, RoutedEventArgs e) => S.SetClipStart();
        private void ClipEnd_Click(object sender, RoutedEventArgs e) => S.SetClipEnd();
        private void SaveA_Click(object sender, RoutedEventArgs e) => S.SaveClip("A");
        private void SaveB_Click(object sender, RoutedEventArgs e) => S.SaveClip("B");
    }
}
