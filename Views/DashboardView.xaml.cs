using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly DispatcherTimer _timer;

        public DashboardView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (_, __) => TickUpdate();
            _timer.Start();

            UpdateHint();
        }

        // ----------------------------
        // Player
        // ----------------------------
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var path = dlg.FileName;

                AppState.Instance.AddImportedVideo(path);
                AppState.Instance.SetSelected(path);

                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;

                AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                AppState.Instance.PlaybackDuration = TimeSpan.Zero;
                AppState.Instance.StatusMessage = $"Loaded: {Path.GetFileName(path)}";

                // Macっぽく：ロードしたら勝手に再生する
                Player.Play();

                UpdateHint();
            }
            catch (Exception ex)
            {
                AppState.Instance.StatusMessage = $"Load failed: {ex.Message}";
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    AppState.Instance.PlaybackDuration = Player.NaturalDuration.TimeSpan;
                }
                else
                {
                    AppState.Instance.PlaybackDuration = TimeSpan.Zero;
                }

                AppState.Instance.PlaybackPosition = Player.Position;
                AppState.Instance.StatusMessage = "Ready";
            }
            catch
            {
                // ignore
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            AppState.Instance.StatusMessage = $"MediaFailed: {e.ErrorException?.Message ?? "unknown"}";
        }

        private void TickUpdate()
        {
            if (Player.Source == null) return;

            try
            {
                // 再生位置
                AppState.Instance.PlaybackPosition = Player.Position;

                // Duration が取れてない場合、MediaOpened後に取れることがあるので再トライ
                if (AppState.Instance.PlaybackDuration == TimeSpan.Zero && Player.NaturalDuration.HasTimeSpan)
                {
                    AppState.Instance.PlaybackDuration = Player.NaturalDuration.TimeSpan;
                }
            }
            catch
            {
                // ignore
            }
        }

        private void UpdateHint()
        {
            PlayerHint.Visibility = (Player.Source == null) ? Visibility.Visible : Visibility.Collapsed;
        }

        // ----------------------------
        // Controls
        // ----------------------------
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Pause();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Stop();
            Player.Position = TimeSpan.Zero;
            AppState.Instance.PlaybackPosition = TimeSpan.Zero;
        }

        private void Minus05_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            var t = Player.Position.TotalSeconds - 0.5;
            if (t < 0) t = 0;
            Player.Position = TimeSpan.FromSeconds(t);
        }

        private void Plus05_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            var t = Player.Position.TotalSeconds + 0.5;

            var dur = AppState.Instance.PlaybackDuration.TotalSeconds;
            if (dur > 0 && t > dur) t = dur;

            Player.Position = TimeSpan.FromSeconds(t);
        }

        private void SetStart_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            AppState.Instance.ClipStartSeconds = Player.Position.TotalSeconds;
            AppState.Instance.StatusMessage = "Set START";
        }

        private void SetEnd_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            AppState.Instance.ClipEndSeconds = Player.Position.TotalSeconds;
            AppState.Instance.StatusMessage = "Set END";
        }

        private void ResetClip_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClipStartSeconds = 0;
            AppState.Instance.ClipEndSeconds = 0;
            AppState.Instance.StatusMessage = "Reset clip range";
        }

        // ----------------------------
        // Tags
        // ----------------------------
        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.AddTag(TagInput.Text);
            TagInput.Text = "";
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.ClearTagsForSelected();
        }

        private void TagInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddTag_Click(sender, e);
                e.Handled = true;
            }
        }

        // ----------------------------
        // Clips buttons (ダミーでOK)
        // ----------------------------
        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import CSV (dummy)", "Import CSV");
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export CSV (dummy)", "Export CSV");
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export All (dummy)", "Export All");
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences (dummy)", "Preferences");
        }
    }
}
