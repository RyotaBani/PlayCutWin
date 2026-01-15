using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private readonly DispatcherTimer _timer;
        private bool _isDragging = false;
        private bool _ignoreSeek = false;
        private bool _mediaReady = false;

        public ClipsView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;

            RefreshCount();
            RefreshSelectedTitle();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (_, __) => Tick();
            _timer.Start();
        }

        // -------------------------
        // UI helpers
        // -------------------------
        private void RefreshCount()
        {
            try
            {
                CountText.Text = $"Count: {PlayCutWin.AppState.Instance.ImportedVideos.Count}";
            }
            catch
            {
                CountText.Text = "Count: 0";
            }
        }

        private void RefreshSelectedTitle()
        {
            var path = PlayCutWin.AppState.Instance.SelectedVideoPath ?? "";
            if (string.IsNullOrWhiteSpace(path))
            {
                SelectedTitle.Text = "Selected: (none)";
            }
            else
            {
                SelectedTitle.Text = $"Selected: {System.IO.Path.GetFileName(path)}";
            }
        }

        private static string Fmt(TimeSpan t)
        {
            int total = (int)Math.Max(0, t.TotalSeconds);
            int mm = total / 60;
            int ss = total % 60;
            return $"{mm:00}:{ss:00}";
        }

        // -------------------------
        // Selection
        // -------------------------
        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshCount();

            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                PlayCutWin.AppState.Instance.SetSelected(item.Path);
                RefreshSelectedTitle();

                LoadVideo(item.Path, autoPlay: false);
            }
        }

        private void VideosGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                LoadVideo(item.Path, autoPlay: true);
            }
        }

        private void LoadVideo(string path, bool autoPlay)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                _mediaReady = false;

                _timer.Stop();
                Player.Stop();

                Player.Source = new Uri(path, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;

                _ignoreSeek = true;
                SeekSlider.Value = 0;
                SeekSlider.Maximum = 1;
                SeekSlider.IsEnabled = false;
                _ignoreSeek = false;

                TimeText.Text = "00:00 / 00:00";

                PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                PlayCutWin.AppState.Instance.PlaybackDuration = TimeSpan.Zero;

                if (autoPlay)
                {
                    Player.Play();
                    PlayCutWin.AppState.Instance.StatusMessage = $"Playing: {System.IO.Path.GetFileName(path)}";
                }
                else
                {
                    PlayCutWin.AppState.Instance.StatusMessage = $"Loaded: {System.IO.Path.GetFileName(path)}";
                }

                _timer.Start();
            }
            catch (Exception ex)
            {
                PlayCutWin.AppState.Instance.StatusMessage = $"Load failed: {ex.Message}";
                MessageBox.Show(ex.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------
        // Player events
        // -------------------------
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    _mediaReady = true;

                    var dur = Player.NaturalDuration.TimeSpan;

                    PlayCutWin.AppState.Instance.PlaybackDuration = dur;
                    PlayCutWin.AppState.Instance.PlaybackPosition = Player.Position;

                    _ignoreSeek = true;
                    SeekSlider.Maximum = Math.Max(1, dur.TotalSeconds);
                    SeekSlider.Value = 0;
                    SeekSlider.IsEnabled = true;
                    _ignoreSeek = false;

                    UpdateTimeText();
                }
                else
                {
                    _mediaReady = false;
                    SeekSlider.IsEnabled = false;
                }
            }
            catch
            {
                _mediaReady = false;
                SeekSlider.IsEnabled = false;
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Stop();
                Player.Position = TimeSpan.Zero;

                _ignoreSeek = true;
                SeekSlider.Value = 0;
                _ignoreSeek = false;

                PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                PlayCutWin.AppState.Instance.StatusMessage = "Ended";

                UpdateTimeText();
            }
            catch { }
        }

        // -------------------------
        // Controls
        // -------------------------
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.Source == null)
                {
                    PlayCutWin.AppState.Instance.StatusMessage = "No video loaded.";
                    return;
                }

                Player.Play();
                PlayCutWin.AppState.Instance.StatusMessage = "Play";
            }
            catch { }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Pause();
                PlayCutWin.AppState.Instance.StatusMessage = "Pause";
            }
            catch { }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.Stop();
                Player.Position = TimeSpan.Zero;

                _ignoreSeek = true;
                SeekSlider.Value = 0;
                _ignoreSeek = false;

                PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                PlayCutWin.AppState.Instance.StatusMessage = "Stop";

                UpdateTimeText();
            }
            catch { }
        }

        // -------------------------
        // Seek
        // -------------------------
        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            SeekTo(SeekSlider.Value);
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ignoreSeek) return;

            if (_isDragging)
            {
                var preview = TimeSpan.FromSeconds(SeekSlider.Value);
                PlayCutWin.AppState.Instance.PlaybackPosition = preview;
                UpdateTimeText(preview, PlayCutWin.AppState.Instance.PlaybackDuration);
            }
        }

        private void SeekTo(double seconds)
        {
            if (!_mediaReady) return;

            try
            {
                seconds = Math.Max(0, seconds);

                Player.Position = TimeSpan.FromSeconds(seconds);
                PlayCutWin.AppState.Instance.PlaybackPosition = Player.Position;

                UpdateTimeText();
            }
            catch { }
        }

        // -------------------------
        // Timer tick
        // -------------------------
        private void Tick()
        {
            RefreshCount();
            RefreshSelectedTitle();

            if (!_mediaReady) return;
            if (_isDragging) return;

            try
            {
                var pos = Player.Position;
                var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;

                PlayCutWin.AppState.Instance.PlaybackPosition = pos;
                PlayCutWin.AppState.Instance.PlaybackDuration = dur;

                _ignoreSeek = true;
                if (dur.TotalSeconds > 0)
                {
                    SeekSlider.Maximum = dur.TotalSeconds;
                    SeekSlider.Value = Math.Min(dur.TotalSeconds, pos.TotalSeconds);
                }
                _ignoreSeek = false;

                UpdateTimeText(pos, dur);
            }
            catch { }
        }

        private void UpdateTimeText()
        {
            var pos = PlayCutWin.AppState.Instance.PlaybackPosition;
            var dur = PlayCutWin.AppState.Instance.PlaybackDuration;
            UpdateTimeText(pos, dur);
        }

        private void UpdateTimeText(TimeSpan pos, TimeSpan dur)
        {
            var left = Fmt(pos);
            var right = dur.TotalSeconds > 0 ? Fmt(dur) : "--:--";
            TimeText.Text = $"{left} / {right}";
        }

        // -------------------------
        // NEW: Range controls (START/END/Add Clip)
        // -------------------------
        private void MarkStart_Click(object sender, RoutedEventArgs e)
        {
            PlayCutWin.AppState.Instance.MarkClipStart();
        }

        private void MarkEnd_Click(object sender, RoutedEventArgs e)
        {
            PlayCutWin.AppState.Instance.MarkClipEnd();
        }

        private void AddClip_Click(object sender, RoutedEventArgs e)
        {
            if (!PlayCutWin.AppState.Instance.CanCreateClip())
            {
                MessageBox.Show("STARTとENDを設定してね（END > START）", "Clip", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // とりあえず tagsText は空（次で Tags と連動させる）
            PlayCutWin.AppState.Instance.CreateClipFromRange(tagsText: "", team: "Team A", note: "");
        }
    }
}
