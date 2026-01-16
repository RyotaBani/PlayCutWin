using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private bool _isSeekDragging = false;

        public ClipsView()
        {
            InitializeComponent();
        }

        // -------------------------
        // Video list
        // -------------------------
        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var path = GetSelectedPath();
            if (string.IsNullOrWhiteSpace(path)) return;

            TrySetAppState("SelectedVideoPath", path);
            TrySetAppState("SelectedVideoText", path);

            try
            {
                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;

                if (SeekSlider != null)
                    SeekSlider.Value = 0;

                TrySetAppState("PlaybackSeconds", 0.0);
            }
            catch { }
        }

        private void VideosGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try { Player.Play(); } catch { }
        }

        private string GetSelectedPath()
        {
            try
            {
                var item = VideosGrid?.SelectedItem;
                if (item == null) return "";

                // VideoItem { Name, Path } を想定
                var p = item.GetType().GetProperty("Path")?.GetValue(item)?.ToString();
                if (!string.IsNullOrWhiteSpace(p)) return p;

                return item.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        // -------------------------
        // Player
        // -------------------------
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan && SeekSlider != null)
                {
                    var sec = Player.NaturalDuration.TimeSpan.TotalSeconds;
                    if (sec < 0) sec = 0;

                    SeekSlider.Minimum = 0;
                    SeekSlider.Maximum = sec;
                    SeekSlider.Value = Math.Min(SeekSlider.Value, sec);

                    TrySetAppState("PlaybackDurationSeconds", sec);
                }
            }
            catch { }
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                // ドラッグ中は確定シークしない（ガタつき防止）
                TrySetAppState("PlaybackSeconds", SeekSlider.Value);

                if (_isSeekDragging) return;

                Player.Position = TimeSpan.FromSeconds(SeekSlider.Value);
            }
            catch { }
        }

        // ✅ XAMLで参照されているやつ（これが無いせいでビルド落ちてた）
        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSeekDragging = true;
        }

        // ✅ XAMLで参照されているやつ（これが無いせいでビルド落ちてた）
        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isSeekDragging = false;

            try
            {
                Player.Position = TimeSpan.FromSeconds(SeekSlider.Value);
                TrySetAppState("PlaybackSeconds", SeekSlider.Value);
            }
            catch { }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try { Player.Play(); } catch { }
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
                if (SeekSlider != null) SeekSlider.Value = 0;

                TrySetAppState("PlaybackSeconds", 0.0);
            }
            catch { }
        }

        // ✅ ±0.5s（XAMLで参照されているやつ）
        private void Minus05_Click(object sender, RoutedEventArgs e)
        {
            NudgeSeconds(-0.5);
        }

        // ✅ ±0.5s（XAMLで参照されているやつ）
        private void Plus05_Click(object sender, RoutedEventArgs e)
        {
            NudgeSeconds(+0.5);
        }

        private void NudgeSeconds(double delta)
        {
            try
            {
                if (SeekSlider == null) return;

                var next = SeekSlider.Value + delta;
                if (next < SeekSlider.Minimum) next = SeekSlider.Minimum;
                if (next > SeekSlider.Maximum) next = SeekSlider.Maximum;

                SeekSlider.Value = next; // ValueChanged が走る（ドラッグ中でなければ即シーク）
                TrySetAppState("PlaybackSeconds", next);
            }
            catch { }
        }

        // -------------------------
        // AppState (safe reflection)
        // -------------------------
        private void TrySetAppState(string prop, object value)
        {
            try
            {
                var appState = DataContext;
                if (appState == null) return;

                var pi = appState.GetType().GetProperty(prop);
                if (pi == null || !pi.CanWrite) return;

                // 型変換が必要なケースだけ軽く吸収
                if (pi.PropertyType == typeof(double) && value is not double)
                {
                    if (double.TryParse(value?.ToString(), out var d))
                        pi.SetValue(appState, d);
                    return;
                }

                pi.SetValue(appState, value);
            }
            catch { }
        }
    }
}
