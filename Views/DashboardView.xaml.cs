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
            DataContext = AppState.Instance;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (_, __) =>
            {
                // 再生中も停止中も Position を監視して表示更新
                if (AppState.Instance.HasVideo)
                {
                    try
                    {
                        AppState.Instance.PlaybackSeconds = VideoPlayer.Position.TotalSeconds;
                    }
                    catch { /* ignore */ }
                }
            };
            _timer.Start();
        }

        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            var path = dlg.FileName;
            var fileName = Path.GetFileName(path);

            AppState.Instance.SetVideo(path, fileName);

            try
            {
                // 重要：Uriで渡す
                VideoPlayer.Stop();
                VideoPlayer.Source = new Uri(path, UriKind.Absolute);

                // 再生開始はMediaOpened後の方が確実だけど、
                // ここでも一回Playしておくと初期化が走りやすい
                VideoPlayer.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load video.\n{ex.Message}", "Load error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            // 総時間はここで確定
            if (VideoPlayer.NaturalDuration.HasTimeSpan)
            {
                AppState.Instance.DurationSeconds = VideoPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            }
            else
            {
                AppState.Instance.DurationSeconds = 0;
            }

            // 先頭で停止表示（Macっぽく“表示はするが勝手に流れない”）
            VideoPlayer.Position = TimeSpan.Zero;
            AppState.Instance.PlaybackSeconds = 0;

            // Playで初期化は済ませたいが、開いた直後にPauseで止める
            VideoPlayer.Pause();
        }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show($"Media failed:\n{e.ErrorException?.Message}", "MediaElement", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (!AppState.Instance.HasVideo) return;
            VideoPlayer.Play();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (!AppState.Instance.HasVideo) return;
            VideoPlayer.Pause();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (!AppState.Instance.HasVideo) return;
            VideoPlayer.Stop();
            VideoPlayer.Position = TimeSpan.Zero;
            AppState.Instance.PlaybackSeconds = 0;
        }

        private void Minus05_Click(object sender, RoutedEventArgs e)
        {
            SeekBy(-0.5);
        }

        private void Plus05_Click(object sender, RoutedEventArgs e)
        {
            SeekBy(+0.5);
        }

        private void SeekBy(double deltaSeconds)
        {
            if (!AppState.Instance.HasVideo) return;

            var cur = VideoPlayer.Position.TotalSeconds;
            var next = cur + deltaSeconds;
            if (next < 0) next = 0;

            // Durationが取れてる場合は上限も切る
            var dur = AppState.Instance.DurationSeconds;
            if (dur > 0 && next > dur) next = dur;

            VideoPlayer.Position = TimeSpan.FromSeconds(next);
            AppState.Instance.PlaybackSeconds = next;
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import CSV (dummy)", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export CSV (dummy)", "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export All (dummy)", "Export All", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
