using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };

        public DashboardView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;

            Loaded += (_, __) =>
            {
                _timer.Tick += Timer_Tick;
                _timer.Start();
                RefreshHint();
            };

            Unloaded += (_, __) =>
            {
                _timer.Stop();
                _timer.Tick -= Timer_Tick;
            };
        }

        // ===== MediaElement =====
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
                AppState.Instance.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
            else
                AppState.Instance.PlaybackDuration = 0;

            AppState.Instance.StatusMessage = "Media opened";
            RefreshHint();
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            AppState.Instance.StatusMessage = $"Media failed: {e.ErrorException.Message}";
            RefreshHint();
        }

        // ===== Top buttons =====
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            AppState.Instance.AddImportedVideo(dlg.FileName);

            if (AppState.Instance.SelectedVideo == null && AppState.Instance.ImportedVideos.Any())
                AppState.Instance.SetSelected(AppState.Instance.ImportedVideos.First());

            ApplySelectedVideoToPlayer(autoplay: false);
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
            MessageBox.Show("Export All (TODO: clip export)", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ===== Controls =====
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                ApplySelectedVideoToPlayer(autoplay: true);
                return;
            }

            Player.Play();
            AppState.Instance.IsPlaying = true;
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            Player.Pause();
            AppState.Instance.IsPlaying = false;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Player.Stop();
            AppState.Instance.IsPlaying = false;
            AppState.Instance.PlaybackSeconds = 0;
        }

        private void Minus05_Click(object sender, RoutedEventArgs e) => SeekBy(-0.5);
        private void Plus05_Click(object sender, RoutedEventArgs e) => SeekBy(+0.5);

        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClipStart = Player?.Position.TotalSeconds ?? 0;
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClipEnd = Player?.Position.TotalSeconds ?? 0;
        }

        private void ResetClip_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ResetRange();
        }

        // ===== Tags =====
        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences (dummy)", "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var text = TagInput?.Text?.Trim() ?? "";
            if (text.Length == 0) return;

            AppState.Instance.AddTagToSelected(text);
            TagInput.Text = "";
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
                e.Handled = true;
            }
        }

        // ===== Internal =====
        private void ApplySelectedVideoToPlayer(bool autoplay)
        {
            var path = AppState.Instance.SelectedVideoPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                AppState.Instance.StatusMessage = "Video path not found.";
                RefreshHint();
                return;
            }

            try
            {
                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);

                AppState.Instance.PlaybackDuration = 0;
                AppState.Instance.PlaybackSeconds = 0;
                AppState.Instance.IsPlaying = false;

                if (autoplay)
                {
                    Player.Play();
                    AppState.Instance.IsPlaying = true;
                }

                AppState.Instance.StatusMessage = $"Loaded: {System.IO.Path.GetFileName(path)}";
                RefreshHint();
            }
            catch (Exception ex)
            {
                AppState.Instance.StatusMessage = $"Load failed: {ex.Message}";
                RefreshHint();
            }
        }

        private void SeekBy(double deltaSeconds)
        {
            if (Player.Source == null) return;

            var cur = Player.Position.TotalSeconds;
            var next = Math.Max(0, cur + deltaSeconds);

            var dur = AppState.Instance.PlaybackDuration;
            if (dur > 0) next = Math.Min(dur, next);

            Player.Position = TimeSpan.FromSeconds(next);
            AppState.Instance.PlaybackSeconds = next;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var sec = Player?.Position.TotalSeconds ?? 0;
            if (sec < 0) sec = 0;

            if (Math.Abs(AppState.Instance.PlaybackSeconds - sec) > 0.05)
                AppState.Instance.PlaybackSeconds = sec;

            RefreshHint();
        }

        private void RefreshHint()
        {
            PlayerHint.Visibility = (Player.Source == null) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
