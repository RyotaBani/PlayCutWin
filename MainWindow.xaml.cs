using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private bool _isDraggingSlider;
        private bool _isPlaying;

        private double _currentSpeed = 1.0; // ★デフォルト 1x

        public MainWindow()
        {
            InitializeComponent();

            // ★起動時に 1x 選択状態
            UpdateSpeedButtons();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (_, __) =>
            {
                if (Player.Source == null) return;
                if (_isDraggingSlider) return;
                if (!Player.NaturalDuration.HasTimeSpan) return;

                TimelineSlider.Maximum = Player.NaturalDuration.TimeSpan.TotalSeconds;
                TimelineSlider.Value = Player.Position.TotalSeconds;
            };
        }

        // ========================= Video Load =========================
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select a video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.wmv;*.avi|All Files|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                Player.Stop();
                Player.Source = new Uri(dlg.FileName);

                // ヒントは読み込み開始で一旦隠す
                VideoHint.Visibility = Visibility.Collapsed;

                // 再生速度を反映（Loaded 前でも OK）
                Player.SpeedRatio = _currentSpeed;

                Player.Play();   // いったん再生して MediaOpened を発火させる
                _isPlaying = true;
                PlayPauseButton.Content = "Pause";

                StatusText.Text = $"Loaded: {System.IO.Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Load failed: {ex.Message}";
                VideoHint.Visibility = Visibility.Visible;
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (!Player.NaturalDuration.HasTimeSpan) return;

            TimelineSlider.Minimum = 0;
            TimelineSlider.Maximum = Player.NaturalDuration.TimeSpan.TotalSeconds;

            // 読み込めたのでヒントは消す
            VideoHint.Visibility = Visibility.Collapsed;

            _timer.Start();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            _isPlaying = false;
            PlayPauseButton.Content = "Play";
            // 終端に来たら止める（必要なら先頭へ戻す）
            // Player.Position = TimeSpan.Zero;
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            StatusText.Text = $"Media failed: {e.ErrorException?.Message}";
            VideoHint.Visibility = Visibility.Visible;
        }

        // ========================= Timeline =========================
        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingSlider) return;
            if (Player.Source == null) return;

            Player.Position = TimeSpan.FromSeconds(TimelineSlider.Value);
        }

        // もしドラッグ開始/終了を追加したいなら（必要ならXAMLにPreview系イベントを足す）
        // private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _isDraggingSlider = true;
        // private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e) { _isDraggingSlider = false; Player.Position = TimeSpan.FromSeconds(TimelineSlider.Value); }

        // ========================= Playback =========================
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;

            if (_isPlaying)
            {
                Player.Pause();
                _isPlaying = false;
                PlayPauseButton.Content = "Play";
            }
            else
            {
                Player.Play();
                _isPlaying = true;
                PlayPauseButton.Content = "Pause";
            }
        }

        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => SeekBy(-5);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => SeekBy(-1);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => SeekBy(+1);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => SeekBy(+5);

        private void SeekBy(int seconds)
        {
            if (Player.Source == null) return;

            var newPos = Player.Position.TotalSeconds + seconds;
            if (newPos < 0) newPos = 0;

            Player.Position = TimeSpan.FromSeconds(newPos);
            TimelineSlider.Value = newPos;
        }

        // ========================= Speed =========================
        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        private void SetSpeed(double speed)
        {
            _currentSpeed = speed;
            if (Player != null) Player.SpeedRatio = _currentSpeed;
            UpdateSpeedButtons();
            StatusText.Text = $"Speed: {speed}x";
        }

        private void UpdateSpeedButtons()
        {
            // 見た目の選択状態（1xがデフォルトでONになる）
            var normalBg = Brushes.DimGray;
            var selectedBg = Brushes.DodgerBlue;

            Speed025Button.Background = Math.Abs(_currentSpeed - 0.25) < 0.001 ? selectedBg : normalBg;
            Speed05Button.Background  = Math.Abs(_currentSpeed - 0.5)  < 0.001 ? selectedBg : normalBg;
            Speed1Button.Background   = Math.Abs(_currentSpeed - 1.0)  < 0.001 ? selectedBg : normalBg;
            Speed2Button.Background   = Math.Abs(_currentSpeed - 2.0)  < 0.001 ? selectedBg : normalBg;

            Speed025Button.Foreground = Brushes.White;
            Speed05Button.Foreground  = Brushes.White;
            Speed1Button.Foreground   = Brushes.White;
            Speed2Button.Foreground   = Brushes.White;
        }

        // ========================= Clip / Tags (次の処理の入口) =========================
        private void ClipStart_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            StatusText.Text = $"Clip START: {Player.Position:mm\\:ss}";
        }

        private void ClipEnd_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            StatusText.Text = $"Clip END: {Player.Position:mm\\:ss}";
        }

        private void SaveTeamA_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Saved to Team A (stub)";
        }

        private void SaveTeamB_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Saved to Team B (stub)";
        }

        private void Tag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                CurrentTagsText.Text = b.Content?.ToString() ?? "(No tags selected)";
            }
        }

        private void AddCustomTag_Click(object sender, RoutedEventArgs e)
        {
            var t = CustomTagTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(t)) return;
            CurrentTagsText.Text = t;
            CustomTagTextBox.Clear();
        }

        private void ClearCustomTag_Click(object sender, RoutedEventArgs e)
        {
            CurrentTagsText.Text = "(No tags selected)";
            CustomTagTextBox.Clear();
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences (stub)", "Play Cut");
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import CSV (stub)", "Play Cut");
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export CSV (stub)", "Play Cut");
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export All (stub)", "Play Cut");
        }
    }
}
