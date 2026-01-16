using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private bool _isDraggingSlider = false;
        private bool _isUpdatingFromTimer = false;
        private readonly DispatcherTimer _timer;

        public ClipsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            UpdateCount();
            UpdateSelectedTitle();
        }

        private void UpdateCount()
        {
            CountText.Text = $"Count: {AppState.Instance.ImportedVideos.Count}";
        }

        private void UpdateSelectedTitle()
        {
            var app = AppState.Instance;
            SelectedTitle.Text = string.IsNullOrWhiteSpace(app.SelectedVideoName)
                ? "Selected: (none)"
                : $"Selected: {app.SelectedVideoName}";
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var app = AppState.Instance;

            if (VideosGrid.SelectedItem is VideoItem item)
            {
                app.SetSelected(item);
                UpdateSelectedTitle();

                try
                {
                    Player.Stop();
                    Player.Source = new Uri(item.Path);
                    Player.Position = TimeSpan.Zero;
                    Player.Play(); // 選択したら即再生（mac版の気持ちよさ寄せ）
                    app.StatusMessage = $"Playing: {item.Name}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Player", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try { Player.Play(); }
            catch { }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            try { Player.Pause(); }
            catch { }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try { Player.Stop(); }
            catch { }
        }

        private void Minus05_Click(object sender, RoutedEventArgs e) => SeekRelative(-0.5);
        private void Plus05_Click(object sender, RoutedEventArgs e) => SeekRelative(+0.5);

        private void SeekRelative(double deltaSeconds)
        {
            try
            {
                var cur = Player.Position.TotalSeconds;
                var next = Math.Max(0, cur + deltaSeconds);
                Player.Position = TimeSpan.FromSeconds(next);
                AppState.Instance.PlaybackSeconds = next;
            }
            catch { }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (Player.Source == null) return;

            if (_isDraggingSlider) return;

            try
            {
                var app = AppState.Instance;

                // Duration
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    var dur = Player.NaturalDuration.TimeSpan.TotalSeconds;
                    if (dur > 0)
                    {
                        app.PlaybackDuration = dur;

                        _isUpdatingFromTimer = true;
                        SeekSlider.Maximum = dur;
                        SeekSlider.Value = Player.Position.TotalSeconds;
                        _isUpdatingFromTimer = false;
                    }
                }

                // Position
                app.PlaybackSeconds = Player.Position.TotalSeconds;

                // UI time text
                TimeText.Text = $"{app.PlaybackPositionText} / {app.PlaybackDurationText}";
            }
            catch
            {
                // ignore
            }
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isUpdatingFromTimer) return;
            if (!_isDraggingSlider) return;

            // dragging中だけ Position に反映（ガタガタ防止）
            try
            {
                var sec = SeekSlider.Value;
                AppState.Instance.PlaybackSeconds = sec;
                TimeText.Text = $"{AppState.Instance.PlaybackPositionText} / {AppState.Instance.PlaybackDurationText}";
            }
            catch { }
        }

        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = true;
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSlider = false;

            try
            {
                var sec = SeekSlider.Value;
                Player.Position = TimeSpan.FromSeconds(sec);
                AppState.Instance.PlaybackSeconds = sec;
            }
            catch { }
        }
    }
}
