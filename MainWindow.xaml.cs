using Microsoft.Win32;
using System;
using System.Windows;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 初期状態：ヒント表示
            if (VideoHint != null) VideoHint.Visibility = Visibility.Visible;
        }

        // ===== MediaElement events =====
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            // 動画が開けたらヒントを消す
            if (VideoHint != null) VideoHint.Visibility = Visibility.Collapsed;
            if (StatusText != null) StatusText.Text = "Video loaded";
        }

        // ★ Actionsの Player_MediaEnded が無いエラーを潰す
        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 終了したら停止状態へ（必要なら先頭へ）
                Player.Stop();
                Player.Position = TimeSpan.Zero;
                if (StatusText != null) StatusText.Text = "Ended";
            }
            catch { /* no-op */ }
        }

        // ===== Top-right buttons =====
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.wmv;*.avi|All Files|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    Player.Source = new Uri(ofd.FileName);
                    Player.LoadedBehavior = System.Windows.Controls.MediaState.Manual;
                    Player.UnloadedBehavior = System.Windows.Controls.MediaState.Manual;
                    Player.Stop();

                    if (VideoHint != null) VideoHint.Visibility = Visibility.Collapsed;
                    if (StatusText != null) StatusText.Text = "Ready";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load video:\n" + ex.Message);
                }
            }
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e) { }
        private void ExportCsv_Click(object sender, RoutedEventArgs e) { }
        private void ExportAll_Click(object sender, RoutedEventArgs e) { }

        // ===== Controls =====
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 簡易：Play/Pause切替
                //（厳密な状態管理は後ででOK）
                Player.Play();
                if (StatusText != null) StatusText.Text = "Playing";
            }
            catch { }
        }

        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => SeekBy(TimeSpan.FromSeconds(-5));
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => SeekBy(TimeSpan.FromSeconds(-1));
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => SeekBy(TimeSpan.FromSeconds(1));
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => SeekBy(TimeSpan.FromSeconds(5));

        private void SeekBy(TimeSpan delta)
        {
            try
            {
                var p = Player.Position + delta;
                if (p < TimeSpan.Zero) p = TimeSpan.Zero;
                Player.Position = p;
            }
            catch { }
        }

        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        private void SetSpeed(double speed)
        {
            try
            {
                Player.SpeedRatio = speed;
                if (StatusText != null) StatusText.Text = $"Speed {speed:0.##}x";
            }
            catch { }
        }

        private void ClipStart_Click(object sender, RoutedEventArgs e) { }
        private void ClipEnd_Click(object sender, RoutedEventArgs e) { }
        private void SaveTeamA_Click(object sender, RoutedEventArgs e) { }
        private void SaveTeamB_Click(object sender, RoutedEventArgs e) { }

        // ===== Tags =====
        private void Preferences_Click(object sender, RoutedEventArgs e) { }
        private void AddCustomTag_Click(object sender, RoutedEventArgs e) { }
        private void ClearTagSelection_Click(object sender, RoutedEventArgs e) { }
    }
}
