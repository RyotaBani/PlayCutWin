using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private readonly DispatcherTimer _timer;
        private bool _isDraggingSeek = false;
        private bool _isMediaOpened = false;

        public ClipsView()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += (_, __) => TickUpdate();
            Loaded += (_, __) => _timer.Start();
            Unloaded += (_, __) => _timer.Stop();
        }

        // -------------------------
        // DataGrid selection -> load video
        // -------------------------
        private void ClipsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ClipsGrid.SelectedItem;
            if (item == null) return;

            // item has { Name, Path } (your VideoItem)
            var nameProp = item.GetType().GetProperty("Name");
            var pathProp = item.GetType().GetProperty("Path");

            var name = nameProp?.GetValue(item)?.ToString() ?? "(unknown)";
            var path = pathProp?.GetValue(item)?.ToString() ?? "";

            SelectedTitle.Text = $"Selected: {name}";
            LoadVideo(path);

            // optional: keep AppState in sync if it exists
            TrySetAppStateSelected(path, name);
        }

        private void LoadVideo(string path)
        {
            _isMediaOpened = false;

            if (string.IsNullOrWhiteSpace(path))
            {
                Player.Source = null;
                NoVideoText.Visibility = Visibility.Visible;
                TimeText.Text = "00:00 / 00:00";
                SeekSlider.Value = 0;
                SeekSlider.Maximum = 1;
                return;
            }

            try
            {
                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);
                NoVideoText.Visibility = Visibility.Collapsed;

                // autoplay is optional; for now keep it manual (user presses Play)
                // Player.Play();
            }
            catch
            {
                Player.Source = null;
                NoVideoText.Visibility = Visibility.Visible;
            }
        }

        // -------------------------
        // Player events
        // -------------------------
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            _isMediaOpened = true;

            if (Player.NaturalDuration.HasTimeSpan)
            {
                var dur = Player.NaturalDuration.TimeSpan.TotalSeconds;
                SeekSlider.Maximum = Math.Max(1, dur);
            }
            else
            {
                SeekSlider.Maximum = 1;
            }

            UpdateTimeText();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            Player.Stop();
            SeekSlider.Value = 0;
            UpdateTimeText();
        }

        // -------------------------
        // Controls
        // -------------------------
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Play();
            TrySetStatus("Playing");
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Pause();
            TrySetStatus("Paused");
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Stop();
            SeekSlider.Value = 0;
            UpdateTimeText();
            TrySetStatus("Stopped");
        }
        // -------------------------
        // Seek slider (drag to seek)
        // -------------------------
        private void SeekSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDraggingSeek = true;
        }

        private void SeekSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDraggingSeek = false;
            SeekToSlider();
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingSeek)
            {
                // while dragging, update time text preview
                UpdateTimeText(previewSeconds: SeekSlider.Value);
            }
        }

        private void SeekToSlider()
        {
            if (Player.Source == null) return;
            if (!_isMediaOpened) return;

            var sec = SeekSlider.Value;
            try
            {
                Player.Position = TimeSpan.FromSeconds(sec);
                UpdateTimeText();
            }
            catch
            {
                // ignore
            }
        }

        // -------------------------
        // +/- 0.5s buttons
        // -------------------------
        private void Back05_Click(object sender, RoutedEventArgs e)
        {
            Nudge(-0.5);
        }

        private void Fwd05_Click(object sender, RoutedEventArgs e)
        {
            Nudge(+0.5);
        }

        private void Nudge(double deltaSeconds)
        {
            if (Player.Source == null) return;
            if (!_isMediaOpened) return;

            try
            {
                var now = Player.Position.TotalSeconds;
                var target = Math.Max(0, now + deltaSeconds);

                // clamp to duration if available
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    var max = Player.NaturalDuration.TimeSpan.TotalSeconds;
                    target = Math.Min(max, target);
                }

                Player.Position = TimeSpan.FromSeconds(target);
                SeekSlider.Value = target;
                UpdateTimeText();
            }
            catch
            {
                // ignore
            }
        }

        // -------------------------
        // Tick update (sync slider/time)
        // -------------------------
        private void TickUpdate()
        {
            if (Player.Source == null) return;
            if (!_isMediaOpened) return;

            if (_isDraggingSeek) return;

            try
            {
                var pos = Player.Position.TotalSeconds;
                if (pos < 0) pos = 0;

                // update slider safely
                if (SeekSlider.Maximum <= 0) SeekSlider.Maximum = 1;
                if (pos <= SeekSlider.Maximum)
                    SeekSlider.Value = pos;

                UpdateTimeText();
            }
            catch
            {
                // ignore
            }
        }

        private void UpdateTimeText(double? previewSeconds = null)
        {
            double cur = previewSeconds ?? (Player.Source != null ? Player.Position.TotalSeconds : 0);
            double total = 0;

            if (Player.Source != null && Player.NaturalDuration.HasTimeSpan)
                total = Player.NaturalDuration.TimeSpan.TotalSeconds;

            TimeText.Text = $"{FormatTime(cur)} / {FormatTime(total)}";
        }

        private static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);

            // 1h以上も一応対応
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";

            return $"{ts.Minutes:00}:{ts.Seconds:00}";
        }

        // -------------------------
        // AppState helpers (optional)
        // -------------------------
        private void TrySetAppStateSelected(string path, string name)
        {
            try
            {
                // If your AppState has SetSelected(...) compatibility
                var t = Type.GetType("PlayCutWin.AppState");
                if (t == null) return;

                // try AppState.Current or AppState.Instance
                object? appStateObj = null;

                var curProp = t.GetProperty("Current");
                if (curProp != null) appStateObj = curProp.GetValue(null);

                if (appStateObj == null)
                {
                    var instProp = t.GetProperty("Instance");
                    if (instProp != null) appStateObj = instProp.GetValue(null);
                }

                if (appStateObj == null) return;

                // SetSelected(string path, string name) or SetSelected(string path)
                var m2 = t.GetMethod("SetSelected", new[] { typeof(string), typeof(string) });
                if (m2 != null)
                {
                    m2.Invoke(appStateObj, new object[] { path, name });
                    return;
                }

                var m1 = t.GetMethod("SetSelected", new[] { typeof(string) });
                if (m1 != null)
                {
                    m1.Invoke(appStateObj, new object[] { path });
                    return;
                }
            }
            catch
            {
                // ignore (AppState shape differs)
            }
        }

        private void TrySetStatus(string message)
        {
            try
            {
                // If AppState.StatusMessage exists
                var t = Type.GetType("PlayCutWin.AppState");
                if (t == null) return;

                object? appStateObj = null;

                var curProp = t.GetProperty("Current");
                if (curProp != null) appStateObj = curProp.GetValue(null);

                if (appStateObj == null)
                {
                    var instProp = t.GetProperty("Instance");
                    if (instProp != null) appStateObj = instProp.GetValue(null);
                }

                if (appStateObj == null) return;

                var statusProp = t.GetProperty("StatusMessage");
                if (statusProp != null && statusProp.CanWrite)
                    statusProp.SetValue(appStateObj, message);
            }
            catch
            {
                // ignore
            }
        }
    }
}
