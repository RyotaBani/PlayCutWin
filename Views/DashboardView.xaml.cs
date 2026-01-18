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
        private readonly DispatcherTimer _timer;

        public DashboardView()
        {
            InitializeComponent();

            // 再生位置更新用
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += (_, __) =>
            {
                if (Player.Source != null)
                {
                    AppState.Instance.PlaybackSeconds = Player.Position.TotalSeconds;
                }
            };
            _timer.Start();

            UpdateHint();
        }

        // ===== MediaElement events =====
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.TotalSeconds : 0;
            AppState.Instance.PlaybackDuration = dur;
            AppState.Instance.StatusMessage = "Media opened";
            UpdateHint();
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            AppState.Instance.StatusMessage = $"Media failed: {e.ErrorException?.Message}";
            AppState.Instance.IsPlaying = false;
            UpdateHint();
        }

        private void UpdateHint()
        {
            PlayerHint.Visibility = (Player.Source == null) ? Visibility.Visible : Visibility.Collapsed;
        }

        // ===== Top Buttons =====
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            AppState.Instance.AddImportedVideo(dlg.FileName);

            try
            {
                Player.Stop();
                Player.Source = new Uri(dlg.FileName, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;

                AppState.Instance.IsPlaying = false;
                AppState.Instance.PlaybackSeconds = 0;
                // Duration は MediaOpened で入る
                AppState.Instance.StatusMessage = "Loaded";
            }
            catch (Exception ex)
            {
                AppState.Instance.StatusMessage = $"Load failed: {ex.Message}";
            }

            UpdateHint();
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ImportCsvFromDialog();
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ExportCsvToDialog();
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            // ここは後で「Clip START/ENDの範囲を切り出して書き出し」に拡張
            AppState.Instance.StatusMessage = "Export All (dummy)";
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences (dummy)", "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ===== Controls: ▶ / ⏸ toggle =====
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                AppState.Instance.StatusMessage = "No video loaded";
                return;
            }

            if (AppState.Instance.IsPlaying)
            {
                Player.Pause();
                AppState.Instance.IsPlaying = false;
                AppState.Instance.StatusMessage = "Paused";
            }
            else
            {
                Player.Play();
                AppState.Instance.IsPlaying = true;
                AppState.Instance.StatusMessage = "Playing";
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;

            Player.Stop();
            Player.Position = TimeSpan.Zero;

            AppState.Instance.IsPlaying = false;
            AppState.Instance.PlaybackSeconds = 0;
            AppState.Instance.StatusMessage = "Stopped";
        }

        private void Minus05_Click(object sender, RoutedEventArgs e)
        {
            SeekBy(-0.5);
        }

        private void Plus05_Click(object sender, RoutedEventArgs e)
        {
            SeekBy(+0.5);
        }

        private void SeekBy(double seconds)
        {
            if (Player.Source == null) return;

            var cur = Player.Position.TotalSeconds;
            var next = cur + seconds;
            if (next < 0) next = 0;

            // Durationが取れていれば上限をクリップ
            if (AppState.Instance.PlaybackDuration > 0 && next > AppState.Instance.PlaybackDuration)
                next = AppState.Instance.PlaybackDuration;

            Player.Position = TimeSpan.FromSeconds(next);
            AppState.Instance.PlaybackSeconds = next;
            AppState.Instance.StatusMessage = $"Seek {seconds:+0.0;-0.0}s";
        }

        // ===== Clip START / Clip END =====
        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClipStart = AppState.Instance.PlaybackSeconds;
            AppState.Instance.StatusMessage = "Clip START set";
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClipEnd = AppState.Instance.PlaybackSeconds;
            AppState.Instance.StatusMessage = "Clip END set";
        }

        private void ResetClip_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ResetRange();
        }

        // ===== Tags =====
        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var t = TagInput.Text?.Trim() ?? "";
            if (t.Length == 0) return;

            AppState.Instance.AddTagToSelected(t);
            TagInput.Clear();
            TagInput.Focus();
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClearTagsForSelected();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag_Click(sender, e);
            }
        }
    }
}
