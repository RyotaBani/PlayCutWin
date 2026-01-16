using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
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
            if (string.IsNullOrEmpty(path)) return;

            TrySetAppState("SelectedVideoPath", path);
            TrySetAppState("SelectedVideoText", path);

            try
            {
                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);
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

                var p = item.GetType().GetProperty("Path")?.GetValue(item)?.ToString();
                if (!string.IsNullOrEmpty(p)) return p;

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
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    var sec = Player.NaturalDuration.TimeSpan.TotalSeconds;
                    SeekSlider.Minimum = 0;
                    SeekSlider.Maximum = sec;
                    SeekSlider.Value = 0;

                    TrySetAppState("PlaybackDurationSeconds", sec);
                }
            }
            catch { }
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
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
                SeekSlider.Value = 0;
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

                pi.SetValue(appState, value);
            }
            catch { }
        }
    }
}
