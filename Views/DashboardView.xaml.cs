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
        private AppState S => AppState.Instance;
        private readonly DispatcherTimer _timer = new DispatcherTimer();

        public DashboardView()
        {
            InitializeComponent();
            DataContext = S;

            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += (_, __) =>
            {
                try
                {
                    if (Player.Source != null)
                    {
                        S.PlaybackSeconds = Player.Position.TotalSeconds;
                    }
                }
                catch { /* ignore */ }
            };
            _timer.Start();

            UpdateHint();
        }

        // ===== MediaElement events =====

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    S.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
                }
                S.StatusMessage = "Media opened";
                UpdateHint();
            }
            catch (Exception ex)
            {
                S.StatusMessage = $"MediaOpened failed: {ex.Message}";
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            S.IsPlaying = false;
            S.StatusMessage = $"Media failed: {e.ErrorException.Message}";
            UpdateHint();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            // 終了したら停止状態へ
            S.IsPlaying = false;
            S.StatusMessage = "Ended";
        }

        private void UpdateHint()
        {
            PlayerHint.Visibility = (Player.Source == null) ? Visibility.Visible : Visibility.Collapsed;
        }

        // ===== Top buttons (右上) =====

        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video files|*.mp4;*.mov;*.m4v;*.avi;*.wmv;*.mkv|All files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            S.AddImportedVideo(dlg.FileName);

            try
            {
                Player.Stop();
                Player.Source = new Uri(dlg.FileName, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;

                // 読み込みだけ。勝手に再生しない（Macっぽく）
                S.IsPlaying = false;
                S.PlaybackSeconds = 0;
                S.StatusMessage = "Video loaded";
                UpdateHint();
            }
            catch (Exception ex)
            {
                S.StatusMessage = $"Load failed: {ex.Message}";
            }
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e) => S.ImportCsvFromDialog();
        private void ExportCsv_Click(object sender, RoutedEventArgs e) => S.ExportCsvToDialog();

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            // ここは後で実装（いまは落ちないように）
            S.StatusMessage = "Export All: (todo)";
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            S.StatusMessage = "Preferences: (todo)";
        }

        // ===== Controls (左下) =====

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                S.StatusMessage = "No video loaded";
                return;
            }

            try
            {
                if (S.IsPlaying)
                {
                    Player.Pause();
                    S.IsPlaying = false;
                    S.StatusMessage = "Paused";
                }
                else
                {
                    Player.Play();
                    S.IsPlaying = true;
                    S.StatusMessage = "Playing";
                }
            }
            catch (Exception ex)
            {
                S.StatusMessage = $"Play/Pause failed: {ex.Message}";
                S.IsPlaying = false;
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;

            try
            {
                Player.Stop();
                Player.Position = TimeSpan.Zero;
                S.IsPlaying = false;
                S.PlaybackSeconds = 0;
                S.StatusMessage = "Stopped";
            }
            catch (Exception ex)
            {
                S.StatusMessage = $"Stop failed: {ex.Message}";
            }
        }

        private void Minus05_Click(object sender, RoutedEventArgs e) => SeekBy(-0.5);
        private void Plus05_Click(object sender, RoutedEventArgs e) => SeekBy(+0.5);

        private void SeekBy(double deltaSec)
        {
            if (Player.Source == null) return;

            try
            {
                var t = Player.Position.TotalSeconds + deltaSec;
                if (t < 0) t = 0;

                Player.Position = TimeSpan.FromSeconds(t);
                S.PlaybackSeconds = t;
                S.StatusMessage = "Seek";
            }
            catch (Exception ex)
            {
                S.StatusMessage = $"Seek failed: {ex.Message}";
            }
        }

        // Clip Range
        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            S.ClipStart = S.PlaybackSeconds;
            S.StatusMessage = $"Clip START: {S.PlaybackPositionText}";
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            S.ClipEnd = S.PlaybackSeconds;
            S.StatusMessage = $"Clip END: {S.PlaybackPositionText}";
        }

        private void ResetClip_Click(object sender, RoutedEventArgs e)
        {
            S.ResetRange();
        }

        // ===== Tags (右下) =====

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var tag = (TagInput.Text ?? "").Trim();
            if (tag.Length == 0) return;

            S.AddTagToSelected(tag);
            TagInput.Text = "";
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            S.ClearTagsForSelected();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
