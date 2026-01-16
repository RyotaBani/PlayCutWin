using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private readonly AppState _state = AppState.Current;
        private readonly DispatcherTimer _timer;
        private bool _isDraggingSeek;

        public ClipsView()
        {
            InitializeComponent();
            DataContext = _state;

            VideosGrid.ItemsSource = _state.ImportedVideos;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            Player.MediaOpened += Player_MediaOpened;
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideosGrid.SelectedItem is not VideoItem v) return;

            _state.SetSelected(v.Path);

            try
            {
                Player.Source = new Uri(v.Path);
                Player.Position = TimeSpan.Zero;
                Player.Play();
                _state.StatusMessage = $"Playing: {v.Name}";
            }
            catch (Exception ex)
            {
                _state.StatusMessage = $"Failed to play: {ex.Message}";
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                var dur = Player.NaturalDuration.TimeSpan.TotalSeconds;
                _state.PlaybackDuration = dur;
                SeekSlider.Maximum = Math.Max(1, dur);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_isDraggingSeek) return;

            try
            {
                var pos = Player.Position.TotalSeconds;
                _state.PlaybackPosition = pos;

                if (SeekSlider.Maximum > 1)
                {
                    SeekSlider.Value = Math.Max(0, Math.Min(SeekSlider.Maximum, pos));
                }
            }
            catch { /* ignore */ }
        }

        // ---- Buttons --------------------------------------------------------
        private void Play_Click(object sender, RoutedEventArgs e) => Player.Play();
        private void Pause_Click(object sender, RoutedEventArgs e) => Player.Pause();

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Player.Stop();
            _state.PlaybackPosition = 0;
            if (SeekSlider.Maximum > 1) SeekSlider.Value = 0;
        }

        private void Minus05_Click(object sender, RoutedEventArgs e) => SeekBy(-0.5);
        private void Plus05_Click(object sender, RoutedEventArgs e) => SeekBy(+0.5);

        private void SeekBy(double deltaSeconds)
        {
            var target = Player.Position.TotalSeconds + deltaSeconds;
            SeekToSeconds(target);
        }

        private void SeekToSeconds(double seconds)
        {
            if (seconds < 0) seconds = 0;

            try
            {
                Player.Position = TimeSpan.FromSeconds(seconds);
                _state.PlaybackPosition = seconds;
                if (SeekSlider.Maximum > 1)
                    SeekSlider.Value = Math.Max(0, Math.Min(SeekSlider.Maximum, seconds));
            }
            catch { /* ignore */ }
        }

        // ---- Slider ---------------------------------------------------------
        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDraggingSeek = true;
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDraggingSeek = false;
            SeekToSeconds(SeekSlider.Value);
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // ドラッグ中は表示だけ追従（離したら Seek 実行）
            if (_isDraggingSeek)
            {
                _state.PlaybackPosition = SeekSlider.Value;
            }
        }
    }
}
