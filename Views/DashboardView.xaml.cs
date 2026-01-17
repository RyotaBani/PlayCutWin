using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly AppState _app = AppState.Instance;
        private readonly DispatcherTimer _tick;

        public DashboardView()
        {
            InitializeComponent();
            DataContext = _app;

            // 位置更新（再生中じゃなくても Position を拾える）
            _tick = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _tick.Tick += (_, __) => SyncPositionToAppState();
            _tick.Start();

            UpdatePlayerHint();
        }

        // =========================
        // Video Load
        // =========================
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            _app.AddImportedVideo(dlg.FileName);

            // MediaElementへ反映
            LoadSelectedVideoToPlayer(autoPlay: false);
        }

        private void LoadSelectedVideoToPlayer(bool autoPlay)
        {
            try
            {
                var path = _app.SelectedVideoPath;

                if (string.IsNullOrWhiteSpace(path))
                {
                    Player.Source = null;
                    _app.PlaybackSeconds = 0;
                    _app.PlaybackDuration = 0;
                    _app.IsPlaying = false;
                    UpdatePlayerHint();
                    return;
                }

                // いったん止めてから差し替える
                Player.Stop();
                Player.Source = null;

                // 絶対パスを Uri で渡す（これが一番安定）
                Player.Source = new Uri(path, UriKind.Absolute);
                Player.LoadedBehavior = MediaState.Manual;
                Player.UnloadedBehavior = MediaState.Manual;

                // 位置・時間リセット（MediaOpenedでDurationが入る）
                _app.PlaybackSeconds = 0;
                _app.PlaybackDuration = 0;

                UpdatePlayerHint();

                if (autoPlay)
                {
                    Player.Play();
                    _app.IsPlaying = true;
                    _app.StatusMessage = "Playing";
                }
                else
                {
                    _app.IsPlaying = false;
                    _app.StatusMessage = "Video loaded";
                }
            }
            catch (Exception ex)
            {
                _app.StatusMessage = $"Load failed: {ex.Message}";
                MessageBox.Show(ex.Message, "Load Video", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================
        // MediaElement events
        // =========================
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                // Duration反映
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    _app.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
                }
                else
                {
                    _app.PlaybackDuration = 0;
                }

                _app.StatusMessage = "Media opened";
                UpdatePlayerHint();
            }
            catch (Exception ex)
            {
                _app.StatusMessage = $"MediaOpened error: {ex.Message}";
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _app.IsPlaying = false;
            _app.PlaybackDuration = 0;
            _app.StatusMessage = $"Media failed: {e.ErrorException?.Message ?? "Unknown error"}";
            UpdatePlayerHint();

            MessageBox.Show(_app.StatusMessage, "Media Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // =========================
        // Controls buttons
        // =========================
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            // まだ Source 無いなら、選択中をロードしてから再生
            if (Player.Source == null && !string.IsNullOrWhiteSpace(_app.SelectedVideoPath))
            {
                LoadSelectedVideoToPlayer(autoPlay: true);
                return;
            }

            if (Player.Source == null)
            {
                _app.StatusMessage = "No video selected";
                UpdatePlayerHint();
                return;
            }

            Player.Play();
            _app.IsPlaying = true;
            _app.StatusMessage = "Playing";
            UpdatePlayerHint();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Pause();
            _app.IsPlaying = false;
            _app.StatusMessage = "Paused";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Stop();
            _app.IsPlaying = false;
            _app.PlaybackSeconds = 0;
            _app.StatusMessage = "Stopped";
        }

        private void Minus05_Click(object sender, RoutedEventArgs e) => SeekDelta(-0.5);
        private void Plus05_Click(object sender, RoutedEventArgs e) => SeekDelta(+0.5);

        private void SeekDelta(double deltaSeconds)
        {
            if (Player.Source == null) return;

            try
            {
                var cur = Player.Position.TotalSeconds;
                var next = Math.Max(0, cur + deltaSeconds);

                // Durationが分かってるなら上限もかける
                if (_app.PlaybackDuration > 0)
                    next = Math.Min(_app.PlaybackDuration, next);

                Player.Position = TimeSpan.FromSeconds(next);
                _app.PlaybackSeconds = next;
                _app.StatusMessage = $"Seek {deltaSeconds:+0.0;-0.0}s";
            }
            catch (Exception ex)
            {
                _app.StatusMessage = $"Seek failed: {ex.Message}";
            }
        }

        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            _app.ClipStart = _app.PlaybackSeconds;
            _app.StatusMessage = $"Start set: {_app.PlaybackPositionText}";
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            _app.ClipEnd = _app.PlaybackSeconds;
            _app.StatusMessage = $"End set: {_app.PlaybackPositionText}";
        }

        private void ResetClip_Click(object sender, RoutedEventArgs e)
        {
            _app.ResetRange();
        }

        // =========================
        // CSV buttons (delegates to AppState)
        // =========================
        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            try { _app.ImportCsvFromDialog(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Import CSV", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try { _app.ExportCsvToDialog(); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Export CSV", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            // ここは次で実装（落ちないダミー）
            _app.StatusMessage = "Export All: not implemented yet (dummy)";
            MessageBox.Show("Export All は次で実装する（今はダミー）", "Export All",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences (dummy)", "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // =========================
        // Helpers
        // =========================
        private void SyncPositionToAppState()
        {
            if (Player.Source == null) return;

            try
            {
                // ここが “null警告” を出しやすい場所なので、例外/状態を丁寧に扱う
                var pos = Player.Position.TotalSeconds;
                if (pos < 0) pos = 0;

                _app.PlaybackSeconds = pos;

                // NaturalDuration を拾えるなら随時反映（MediaOpenedが来ない動画でも保険）
                if (_app.PlaybackDuration <= 0 && Player.NaturalDuration.HasTimeSpan)
                {
                    _app.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
                }
            }
            catch
            {
                // ここは黙殺でOK（UIタイマーなので）
            }
        }

        private void UpdatePlayerHint()
        {
            // XAMLに PlayerHint TextBlock がいる前提（あなたのDashboardView.xamlにある）
            try
            {
                if (PlayerHint == null) return;

                var hasVideo = !string.IsNullOrWhiteSpace(_app.SelectedVideoPath);
                PlayerHint.Visibility = hasVideo ? Visibility.Collapsed : Visibility.Visible;
            }
            catch { }
        }
    }
}
