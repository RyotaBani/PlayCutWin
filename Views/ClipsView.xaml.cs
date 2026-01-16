using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private readonly DispatcherTimer _timer;
        private bool _isDraggingSeek;

        public ClipsView()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += (_, __) => UpdateUiFromPlayer();

            Loaded += (_, __) =>
            {
                // 初期表示（Count）
                UpdateCountText();
            };

            Unloaded += (_, __) =>
            {
                try { _timer.Stop(); } catch { }
            };
        }

        // -------------------------
        // Grid selection
        // -------------------------
        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var path = GetSelectedVideoPath();
            ApplySelected(path, autoPlay: false);
        }

        private void VideosGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var path = GetSelectedVideoPath();
            ApplySelected(path, autoPlay: true);
        }

        private string GetSelectedVideoPath()
        {
            try
            {
                var item = VideosGrid?.SelectedItem;
                if (item == null) return "";

                // 期待: item.Path or item.FullPath など
                var p = item.GetType().GetProperty("Path")?.GetValue(item)?.ToString();
                if (!string.IsNullOrWhiteSpace(p)) return p;

                var p2 = item.GetType().GetProperty("FullPath")?.GetValue(item)?.ToString();
                if (!string.IsNullOrWhiteSpace(p2)) return p2;

                return item.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        private void ApplySelected(string path, bool autoPlay)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SelectedTitle.Text = "Selected: (none)";
                SeekSlider.IsEnabled = false;
                SeekSlider.Minimum = 0;
                SeekSlider.Maximum = 1;
                SeekSlider.Value = 0;
                TimeText.Text = "00:00 / 00:00";
                try { Player.Stop(); } catch { }
                _timer.Stop();
                return;
            }

            SelectedTitle.Text = $"Selected: {System.IO.Path.GetFileName(path)}";

            // AppStateへの反映（プロパティが無くてもコンパイル通るようReflectionで安全に）
            TrySetOnAppState("SelectedVideoPath", path);
            TrySetOnAppState("SelectedVideoText", path);

            try
            {
                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;
            }
            catch
            {
                // 何もしない（Sourceが壊れてる等）
            }

            // MediaOpenedまで無効
            SeekSlider.IsEnabled = false;
            SeekSlider.Minimum = 0;
            SeekSlider.Maximum = 1;
            SeekSlider.Value = 0;
            TimeText.Text = "00:00 / 00:00";

            if (autoPlay)
            {
                try { Player.Play(); } catch { }
            }
        }

        // -------------------------
        // MediaElement
        // -------------------------
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    var total = Math.Max(0.001, Player.NaturalDuration.TimeSpan.TotalSeconds);
                    SeekSlider.Minimum = 0;
                    SeekSlider.Maximum = total;
                    SeekSlider.IsEnabled = true;

                    UpdateUiFromPlayer();
                    _timer.Start();

                    // AppStateへ duration 反映（あれば）
                    TrySetOnAppState("PlaybackDurationSeconds", total);
                    TrySetOnAppState("PlaybackDuration", total);
                }
                else
                {
                    SeekSlider.IsEnabled = false;
                }
            }
            catch
            {
                SeekSlider.IsEnabled = false;
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Stop();
                Player.Position = TimeSpan.Zero;
                SeekSlider.Value = 0;
            }
            catch { }

            UpdateUiFromPlayer();
        }

        // -------------------------
        // Seek slider
        // -------------------------
        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSeek = true;
            try { SeekSlider.CaptureMouse(); } catch { }
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var sec = SeekSlider.Value;
                Player.Position = TimeSpan.FromSeconds(sec);

                // AppStateへ position 反映（あれば）
                TrySetOnAppState("PlaybackSeconds", sec);
                TrySetOnAppState("PlaybackPosition", sec);
            }
            catch { }

            _isDraggingSeek = false;
            try { SeekSlider.ReleaseMouseCapture(); } catch { }

            UpdateUiFromPlayer();
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isDraggingSeek) return;

            // ドラッグ中はプレビュー的に表示だけ更新
            try
            {
                var current = TimeSpan.FromSeconds(SeekSlider.Value);
                var total = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;
                TimeText.Text = $"{FormatTime(current)} / {FormatTime(total)}";
            }
            catch { }
        }

        // -------------------------
        // Buttons
        // -------------------------
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try { Player.Play(); _timer.Start(); } catch { }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            try { Player.Pause(); } catch { }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Stop();
                Player.Position = TimeSpan.Zero;
                SeekSlider.Value = 0;
            }
            catch { }

            UpdateUiFromPlayer();
        }

        // -------------------------
        // UI helpers
        // -------------------------
        private void UpdateUiFromPlayer()
        {
            try
            {
                UpdateCountText();

                if (!Player.NaturalDuration.HasTimeSpan)
                {
                    TimeText.Text = "00:00 / 00:00";
                    return;
                }

                var total = Player.NaturalDuration.TimeSpan;
                var pos = Player.Position;

                if (!_isDraggingSeek && SeekSlider.IsEnabled)
                {
                    var v = Math.Max(0, Math.Min(SeekSlider.Maximum, pos.TotalSeconds));
                    SeekSlider.Value = v;
                }

                TimeText.Text = $"{FormatTime(pos)} / {FormatTime(total)}";

                // AppStateへ position 反映（あれば）
                TrySetOnAppState("PlaybackSeconds", pos.TotalSeconds);
                TrySetOnAppState("PlaybackPosition", pos.TotalSeconds);
            }
            catch
            {
                // ignore
            }
        }

        private void UpdateCountText()
        {
            try
            {
                // ItemsSourceの件数を表示（Binding先が何でも動く）
                var count = VideosGrid?.Items?.Count ?? 0;
                if (CountText != null) CountText.Text = $"Count: {count}";
            }
            catch { }
        }

        private static string FormatTime(TimeSpan t)
        {
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours:0}:{t.Minutes:00}:{t.Seconds:00}";
            return $"{t.Minutes:00}:{t.Seconds:00}";
        }

        // -------------------------
        // AppState reflection (safe)
        // -------------------------
        private object? GetAppState()
        {
            // DataContext が AppState の想定（MainWindow側でセットしてるはず）
            return DataContext;
        }

        private void TrySetOnAppState(string propertyName, object value)
        {
            try
            {
                var appState = GetAppState();
                if (appState == null) return;

                var pi = appState.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (pi == null) return;
                if (!pi.CanWrite) return;

                // 型変換できる範囲で合わせる
                var targetType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType;
                object converted = value;

                if (targetType == typeof(double) && value is not double)
                    converted = Convert.ToDouble(value);
                else if (targetType == typeof(string) && value is not string)
                    converted = value?.ToString() ?? "";

                pi.SetValue(appState, converted);
            }
            catch
            {
                // ignore（存在しない/型違い等）
            }
        }
    }
}
