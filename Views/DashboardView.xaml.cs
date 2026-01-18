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
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public DashboardView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;

            // 再生位置の更新（UIの 00:01 / 01:29:26 を動かす）
            _timer.Interval = TimeSpan.FromMilliseconds(120);
            _timer.Tick += (_, __) =>
            {
                try
                {
                    if (Player.Source != null)
                        AppState.Instance.PlaybackSeconds = Player.Position.TotalSeconds;
                }
                catch { /* ignore */ }
            };
            _timer.Start();

            UpdateHint();
        }

        // ====== Player events ======
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                    AppState.Instance.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
                else
                    AppState.Instance.PlaybackDuration = 0;

                UpdateHint();
                AppState.Instance.StatusMessage = "Media opened";
            }
            catch
            {
                // ignore
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            AppState.Instance.IsPlaying = false;
            AppState.Instance.StatusMessage = $"Media failed: {e.ErrorException?.Message}";
            UpdateHint();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            AppState.Instance.IsPlaying = false;
            AppState.Instance.StatusMessage = "Ended";
            UpdateHint();
        }

        private void UpdateHint()
        {
            PlayerHint.Visibility = (Player.Source == null) ? Visibility.Visible : Visibility.Collapsed;
        }

        // ====== Buttons (Top Right) ======
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                AppState.Instance.AddImportedVideo(dlg.FileName);

                // Playerにセット
                Player.Stop();
                Player.Source = new Uri(dlg.FileName);
                Player.Position = TimeSpan.Zero;

                AppState.Instance.PlaybackSeconds = 0;
                AppState.Instance.IsPlaying = false;

                UpdateHint();
                AppState.Instance.StatusMessage = "Video loaded";
            }
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e) => AppState.Instance.ImportCsvFromDialog();
        private void ExportCsv_Click(object sender, RoutedEventArgs e) => AppState.Instance.ExportCsvToDialog();
        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.StatusMessage = "Export All: (todo)";
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences (todo)", "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ====== Controls ======

        // ★ここ：再生状態で ▶ ⇄ ⏸ を切り替える
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            // まず動画が無ければ何もしない
            if (Player.Source == null)
            {
                AppState.Instance.StatusMessage = "No video loaded";
                return;
            }

            try
            {
                if (!AppState.Instance.IsPlaying)
                {
                    Player.Play();
                    AppState.Instance.IsPlaying = true;
                    AppState.Instance.StatusMessage = "Playing";
                }
                else
                {
                    Player.Pause();
                    AppState.Instance.IsPlaying = false;
                    AppState.Instance.StatusMessage = "Paused";
                }
            }
            catch (Exception ex)
            {
                AppState.Instance.IsPlaying = false;
                AppState.Instance.StatusMessage = $"Play/Pause failed: {ex.Message}";
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Stop();
                Player.Position = TimeSpan.Zero;
            }
            catch { /* ignore */ }

            AppState.Instance.IsPlaying = false;
            AppState.Instance.PlaybackSeconds = 0;
            AppState.Instance.StatusMessage = "Stopped";
        }

        private void Minus05_Click(object sender, RoutedEventArgs e) => SeekBy(-0.5);
        private void Plus05_Click(object sender, RoutedEventArgs e) => SeekBy(+0.5);

        private void SeekBy(double seconds)
        {
            if (Player.Source == null) return;

            try
            {
                var next = Player.Position.TotalSeconds + seconds;
                if (next < 0) next = 0;

                // durationが取れてるなら上限も守る
                var dur = AppState.Instance.PlaybackDuration;
                if (dur > 0 && next > dur) next = dur;

                Player.Position = TimeSpan.FromSeconds(next);
                AppState.Instance.PlaybackSeconds = next;
            }
            catch { /* ignore */ }
        }

        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClipStart = AppState.Instance.PlaybackSeconds;
            AppState.Instance.StatusMessage = "Start set";
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClipEnd = AppState.Instance.PlaybackSeconds;
            AppState.Instance.StatusMessage = "End set";
        }

        private void ResetClip_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ResetRange();
        }

        // ====== Tags ======
        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag();
                e.Handled = true;
            }
        }

        private void AddTag_Click(object sender, RoutedEventArgs e) => AddTag();

        private void AddTag()
        {
            var text = (TagInput.Text ?? "").Trim();
            if (text.Length == 0) return;

            AppState.Instance.AddTagToSelected(text);
            TagInput.Text = "";
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClearTagsForSelected();
        }
    }
}
