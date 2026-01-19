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

            // DataContext を AppState に
            DataContext = S;

            // 初期表示（▶ / ⏸）
            UpdatePlayPauseGlyph();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            S.SendPlayPause();
            // IsPlayingは Player側が最終的に更新する想定だけど、
            // UIが気持ちよく変わるように一旦トグル表示しておく
            S.IsPlaying = !S.IsPlaying;
            UpdatePlayPauseGlyph();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            S.SendStop();
            S.IsPlaying = false;
            UpdatePlayPauseGlyph();
        }

        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => S.SendSeekRelative(-5);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => S.SendSeekRelative(-1);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => S.SendSeekRelative(+1);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => S.SendSeekRelative(+5);

        private void Rate025_Click(object sender, RoutedEventArgs e) => S.SendRate(0.25);
        private void Rate05_Click(object sender, RoutedEventArgs e) => S.SendRate(0.5);
        private void Rate1_Click(object sender, RoutedEventArgs e) => S.SendRate(1.0);
        private void Rate2_Click(object sender, RoutedEventArgs e) => S.SendRate(2.0);

        private void UpdatePlayPauseGlyph()
        {
            // Segoe MDL2 Assets
            // Play:   (E768)  Pause:  (E769)
            if (PlayPauseButton == null) return;

            PlayPauseButton.Content = S.IsPlaying ? "\uE769" : "\uE768";
        }
    }
}
