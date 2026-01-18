using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlayCutWin.Views
{
    public partial class PlayerControlsView : UserControl
    {
        private AppState S => AppState.Instance;

        public PlayerControlsView()
        {
            InitializeComponent();
        }

        // ===== Play / Pause toggle =====
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            var player = FindMediaElement("Player");
            if (player == null)
            {
                S.StatusMessage = "Player not found (x:Name=\"Player\")";
                return;
            }

            try
            {
                if (S.IsPlaying)
                {
                    player.Pause();
                    S.IsPlaying = false;
                    S.StatusMessage = "Paused";
                }
                else
                {
                    player.Play();
                    S.IsPlaying = true;
                    S.StatusMessage = "Playing";
                }
            }
            catch (Exception ex)
            {
                S.StatusMessage = $"Play/Pause failed: {ex.Message}";
            }
        }

        // ===== Seek =====
        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => SeekBy(-5);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => SeekBy(-1);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => SeekBy(+1);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => SeekBy(+5);

        private void SeekBy(double deltaSec)
        {
            var player = FindMediaElement("Player");
            if (player == null)
            {
                S.StatusMessage = "Player not found (x:Name=\"Player\")";
                return;
            }

            try
            {
                var pos = player.Position.TotalSeconds + deltaSec;
                if (pos < 0) pos = 0;
                player.Position = TimeSpan.FromSeconds(pos);
                S.PlaybackSeconds = pos;
                S.StatusMessage = $"{(deltaSec >= 0 ? "+" : "")}{deltaSec:0}s";
            }
            catch (Exception ex)
            {
                S.StatusMessage = $"Seek failed: {ex.Message}";
            }
        }

        // ===== Speed (いったん「見た目揃え」優先：速度は実装先に合わせて後で強化) =====
        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        private void SetSpeed(double speed)
        {
            // MediaElement は速度変更が直接できないので、将来MediaPlayer/VLCへ移す前提でメッセージだけ。
            S.StatusMessage = $"Speed {speed:0.##}x (MediaElement: speed control is limited)";
        }

        // ===== Clip Range buttons (今は状態だけ) =====
        private void ClipStart_Click(object sender, RoutedEventArgs e)
        {
            S.ClipStart = S.PlaybackSeconds;
            S.StatusMessage = $"Clip START set: {S.PlaybackPositionText}";
        }

        private void ClipEnd_Click(object sender, RoutedEventArgs e)
        {
            S.ClipEnd = S.PlaybackSeconds;
            S.StatusMessage = $"Clip END set: {S.PlaybackPositionText}";
        }

        private void SaveTeamA_Click(object sender, RoutedEventArgs e)
        {
            S.StatusMessage = "Save Team A (TODO)";
        }

        private void SaveTeamB_Click(object sender, RoutedEventArgs e)
        {
            S.StatusMessage = "Save Team B (TODO)";
        }

        // ===== helper: find MediaElement by name in VisualTree =====
        private MediaElement? FindMediaElement(string name)
        {
            var root = Application.Current?.MainWindow as DependencyObject;
            if (root == null) return null;

            return FindChildByName<MediaElement>(root, name);
        }

        private static T? FindChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T fe && fe.Name == name)
                    return fe;

                var result = FindChildByName<T>(child, name);
                if (result != null) return result;
            }

            return null;
        }
    }
}
