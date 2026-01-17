using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

                // 入れ替え前に一旦停止
                Player.Stop();
                Player.Source = null;

                Player.Source = new Uri(path, UriKind.Absolute);
                Player.LoadedBehavior = MediaState.Manual;
                Player.UnloadedBehavior = MediaState.Manual;

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
                if (Player.NaturalDuration.HasTimeSpan)
                    _app.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
                else
                    _app.PlaybackDuration = 0;

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

            var msg = e.ErrorException?.Message ?? "Unknown error";
            _app.StatusMessage = $"Media failed: {msg}";

            UpdatePlayerHint();
            MessageBox.Show(_app.StatusMessage, "Media Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // =========================
        // Controls buttons
        // =========================
        private void Play_Click(object sender, RoutedEventArgs e)
        {
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
        // CSV buttons (AppStateへ委譲)
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
            _app.StatusMessage = "Export All: not implemented yet (dummy)";
            MessageBox.Show("Export All は次で実装（今はダミー）", "Export All",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences (dummy)", "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // =========================
        // ✅ Tags handlers（ここが今回のビルドエラー原因だった3つ）
        // =========================
        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            AddTag_Click(sender, e);
            e.Handled = true;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var text = TagInput?.Text ?? "";
                _app.AddTagToSelected(text);

                if (TagInput != null)
                    TagInput.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Add Tag", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _app.ClearTagsForSelected();

                if (TagInput != null)
                    TagInput.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Clear Tags", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // =========================
        // Helpers
        // =========================
        private void SyncPositionToAppState()
        {
            if (Player.Source == null) return;

            try
            {
                var pos = Player.Position.TotalSeconds;
                if (pos < 0) pos = 0;

                _app.PlaybackSeconds = pos;

                if (_app.PlaybackDuration <= 0 && Player.NaturalDuration.HasTimeSpan)
                    _app.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
            }
            catch
            {
                // UIタイマーなので黙殺OK
            }
        }

        private void UpdatePlayerHint()
        {
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
