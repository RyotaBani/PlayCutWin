using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private readonly DispatcherTimer _timer;
        private bool _isDraggingSeek = false;
        private bool _ignoreSeekEvent = false;

        public ClipsView()
        {
            InitializeComponent();

            VideoGrid.ItemsSource = AppState.Current.ImportedVideos;
            RefreshCount();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += (_, __) => UpdateTimeUI();
            _timer.Start();
        }

        private void RefreshCount()
        {
            CountText.Text = $"Count: {AppState.Current.ImportedVideos.Count}";
        }

        private void VideoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = VideoGrid.SelectedItem as VideoItem;
            AppState.Current.SelectedVideo = item;

            if (item == null)
            {
                SelectedTitle.Text = "Selected: (none)";
                StopPlayer();
                return;
            }

            SelectedTitle.Text = $"Selected: {item.Name}";
            LoadAndPlay(item.Path, autoPlay: false);
        }

        private void LoadAndPlay(string path, bool autoPlay)
        {
            try
            {
                StopPlayer();

                Player.Source = new Uri(path);
                Player.Position = TimeSpan.Zero;

                // すぐ Duration が取れないことがあるので、一旦UI更新
                _ignoreSeekEvent = true;
                SeekBar.Value = 0;
                SeekBar.Maximum = 1;
                _ignoreSeekEvent = false;

                UpdateTimeUI();

                if (autoPlay) Player.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Player error");
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                // 未ロードなら選択中を再ロード
                var item = AppState.Current.SelectedVideo;
                if (item != null) LoadAndPlay(item.Path, autoPlay: true);
                return;
            }

            Player.Play();
            AppState.Current.StatusMessage = $"Playing: {AppState.Current.SelectedVideo?.Name ?? ""}";
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            Player.Pause();
            AppState.Current.StatusMessage = "Paused";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            StopPlayer();
            AppState.Current.StatusMessage = "Stopped";
        }

        private void StopPlayer()
        {
            try
            {
                Player.Stop();
                Player.Source = null;

                _ignoreSeekEvent = true;
                SeekBar.Value = 0;
                SeekBar.Maximum = 1;
                _ignoreSeekEvent = false;

                TimeText.Text = "00:00 / 00:00";
            }
            catch { }
        }

        private void SeekBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ignoreSeekEvent) return;
            if (Player.Source == null) return;

            // WPFは ValueChanged が連発されるので、ドラッグ中扱いに寄せる
            if (_isDraggingSeek) return;

            Seek(e.NewValue);
        }

        private void Seek(double seconds)
        {
            try
            {
                Player.Position = TimeSpan.FromSeconds(seconds);
            }
            catch { }
        }

        private void UpdateTimeUI()
        {
            if (Player.Source == null) return;

            var pos = Player.Position;
            var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;

            if (dur.TotalSeconds > 0)
            {
                _ignoreSeekEvent = true;
                SeekBar.Maximum = dur.TotalSeconds;
                SeekBar.Value = Math.Min(pos.TotalSeconds, dur.TotalSeconds);
                _ignoreSeekEvent = false;
            }

            TimeText.Text = $"{Format(pos)} / {Format(dur)}";
        }

        private static string Format(TimeSpan t)
        {
            if (t.TotalHours >= 1) return t.ToString(@"hh\:mm\:ss");
            return t.ToString(@"mm\:ss");
        }
    }
}
