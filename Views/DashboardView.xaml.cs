using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += (_, __) =>
            {
                if (Player.Source == null) return;

                // WPF MediaElement は時々 NaturalDuration が取れない/遅れるので安全に
                try
                {
                    _state.PlaybackSeconds = Player.Position.TotalSeconds;
                }
                catch { }
            };
            _timer.Start();
        }

        // =====================
        // Video load / playback
        // =====================
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
            _state.StatusMessage = "Loading video...";

            PlayerHint.Visibility = Visibility.Collapsed;
            Player.LoadedBehavior = MediaState.Manual;
            Player.UnloadedBehavior = MediaState.Manual;

            Player.Stop();
            Player.Source = new Uri(dlg.FileName);
            Player.Position = TimeSpan.Zero;

            // 読み込み後に Play 押してね、の挙動に合わせて自動再生はしない
            _state.IsPlaying = false;
            _state.StatusMessage = "Media opened";
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                    _state.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
                else
                    _state.PlaybackDuration = 0;
            }
            catch
            {
                _state.PlaybackDuration = 0;
            }

            Player.SpeedRatio = _state.PlaybackSpeed;
            _state.StatusMessage = "Media opened";
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            PlayerHint.Visibility = Visibility.Visible;
            _state.IsPlaying = false;
            _state.StatusMessage = $"Media failed: {e.ErrorException?.Message ?? "unknown error"}";
        }

        // ▶ / ⏸ トグル
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                _state.StatusMessage = "No video loaded";
                return;
            }

            if (_state.IsPlaying)
            {
                Player.Pause();
                _state.IsPlaying = false;
                _state.StatusMessage = "Paused";
            }
            else
            {
                Player.Play();
                _state.IsPlaying = true;
                _state.StatusMessage = "Playing";
            }
        }

        // =====================
        // Speed
        // =====================
        private void SetSpeed(double speed)
        {
            _state.PlaybackSpeed = speed;
            try { Player.SpeedRatio = speed; } catch { }
            _state.StatusMessage = $"Speed: {speed:0.##}x";
        }

        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        // =====================
        // Seek
        // =====================
        private void SeekBy(double sec)
        {
            if (Player.Source == null) return;

            try
            {
                var pos = Player.Position.TotalSeconds + sec;
                if (pos < 0) pos = 0;

                // duration が取れていれば上限も守る
                if (_state.PlaybackDuration > 0 && pos > _state.PlaybackDuration)
                    pos = _state.PlaybackDuration;

                Player.Position = TimeSpan.FromSeconds(pos);
                _state.StatusMessage = $"Seek: {sec:+0;-0;0}s";
            }
            catch { }
        }

        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => SeekBy(-5);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => SeekBy(-1);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => SeekBy(+1);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => SeekBy(+5);

        // =====================
        // Clip range
        // =====================
        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            _state.ClipStart = _state.PlaybackSeconds;
            _state.StatusMessage = "Clip START set";
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            _state.ClipEnd = _state.PlaybackSeconds;
            _state.StatusMessage = "Clip END set";
        }

        private void SaveTeamA_Click(object sender, RoutedEventArgs e)
        {
            _state.SaveClip("A");
        }

        private void SaveTeamB_Click(object sender, RoutedEventArgs e)
        {
            _state.SaveClip("B");
        }

        // =====================
        // Tags
        // =====================
        private void PresetTag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Content is string tag)
                _state.AddTagToSelected(tag);
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            _state.AddTagToSelected(TagInput.Text);
            TagInput.Text = "";
            TagInput.Focus();
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            _state.ClearTagsForSelected();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag_Click(sender, e);
                e.Handled = true;
            }
        }

        private void TagsList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ダブルクリックで消せる（Macの挙動寄せ）
            if (sender is ListBox lb && lb.SelectedItem is AppState.TagEntry te)
                _state.RemoveSelectedTag(te);
        }

        // =====================
        // CSV (既存のAppStateを呼ぶだけ)
        // =====================
        private void ImportCsv_Click(object sender, RoutedEventArgs e) => _state.ImportCsvFromDialog();
        private void ExportCsv_Click(object sender, RoutedEventArgs e) => _state.ExportCsvToDialog();
        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            _state.StatusMessage = "Export All: (not implemented yet)";
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            _state.StatusMessage = "Preferences: (not implemented yet)";
        }
    }
}
