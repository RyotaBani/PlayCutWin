using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly AppState _state = AppState.Instance;
        private readonly DispatcherTimer _timer;

        public DashboardView()
        {
            InitializeComponent();
            DataContext = _state;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (_, __) =>
            {
                try
                {
                    if (Player.Source != null)
                    {
                        _state.PlaybackSeconds = Player.Position.TotalSeconds;
                    }
                }
                catch { }
            };
        }

        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video files|*.mp4;*.mov;*.m4v;*.wmv;*.avi|All files|*.*",
                Multiselect = false
            };
            if (dlg.ShowDialog() != true) return;

            _state.AddImportedVideo(dlg.FileName);

            try
            {
                Player.Source = new Uri(_state.SelectedVideoPath);
                Player.LoadedBehavior = MediaState.Manual;
                Player.UnloadedBehavior = MediaState.Manual;

                // 読み込み直後は停止状態
                _state.IsPlaying = false;
                _state.PlaybackSeconds = 0;
                _state.PlaybackDuration = 0;

                PlayerHint.Visibility = Visibility.Collapsed;
                _state.StatusMessage = "Media opened";
            }
            catch (Exception ex)
            {
                _state.StatusMessage = $"Load failed: {ex.Message}";
                PlayerHint.Visibility = Visibility.Visible;
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                    _state.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
                else
                    _state.PlaybackDuration = 0;

                _state.StatusMessage = "Media opened";
            }
            catch { }

            PlayerHint.Visibility = Visibility.Collapsed;
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _state.StatusMessage = $"Media failed: {e.ErrorException?.Message}";
            PlayerHint.Visibility = Visibility.Visible;
        }

        // ▶ / ⏸ トグル
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                _state.StatusMessage = "No video loaded";
                return;
            }

            try
            {
                if (_state.IsPlaying)
                {
                    Player.Pause();
                    _state.IsPlaying = false;
                    _state.StatusMessage = "Paused";
                    _timer.Stop();
                }
                else
                {
                    Player.Play();
                    _state.IsPlaying = true;
                    _state.StatusMessage = "Playing";
                    _timer.Start();
                }
            }
            catch (Exception ex)
            {
                _state.StatusMessage = $"Play/Pause failed: {ex.Message}";
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Stop();
                _state.IsPlaying = false;
                _state.PlaybackSeconds = 0;
                _state.StatusMessage = "Stopped";
                _timer.Stop();
            }
            catch (Exception ex)
            {
                _state.StatusMessage = $"Stop failed: {ex.Message}";
            }
        }

        private void Minus05_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Position = Player.Position - TimeSpan.FromSeconds(0.5);
                _state.PlaybackSeconds = Player.Position.TotalSeconds;
            }
            catch { }
        }

        private void Plus05_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Position = Player.Position + TimeSpan.FromSeconds(0.5);
                _state.PlaybackSeconds = Player.Position.TotalSeconds;
            }
            catch { }
        }

        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            _state.ClipStart = _state.PlaybackSeconds;
            _state.StatusMessage = "Start set";
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            _state.ClipEnd = _state.PlaybackSeconds;
            _state.StatusMessage = "End set";
        }

        private void ResetClip_Click(object sender, RoutedEventArgs e)
        {
            _state.ResetRange();
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e) => _state.ImportCsvFromDialog();
        private void ExportCsv_Click(object sender, RoutedEventArgs e) => _state.ExportCsvToDialog();
        private void ExportAll_Click(object sender, RoutedEventArgs e) => _state.StatusMessage = "Export All (TODO)";
        private void Preferences_Click(object sender, RoutedEventArgs e) => _state.StatusMessage = "Preferences (TODO)";

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            _state.AddTagToSelected(TagInput.Text);
            TagInput.Text = "";
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e) => _state.ClearTagsForSelected();

        private void TagInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _state.AddTagToSelected(TagInput.Text);
                TagInput.Text = "";
            }
        }
    }
}
