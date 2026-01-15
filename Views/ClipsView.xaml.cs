using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;

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

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += (_, __) => UpdateTime();
        }

        private void RefreshCount()
        {
            CountText.Text = $"Count: {PlayCutWin.AppState.Instance.ImportedVideos.Count}";
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                PlayCutWin.AppState.Instance.SetSelected(item.Path);
                SelectedTitle.Text = $"Selected: {item.Name}";

                // 選択しただけでは自動再生しない（ダブルクリックで再生）
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
            try
            {
                _mediaReady = false;
                _timer.Stop();

                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;

                // seek 初期化
                _ignoreSeek = true;
                SeekSlider.Value = 0;
                SeekSlider.Maximum = 1;
                SeekSlider.IsEnabled = false;
                _ignoreSeek = false;

                // AppStateへ共有
                PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                PlayCutWin.AppState.Instance.PlaybackDuration = TimeSpan.Zero;

                TimeText.Text = "00:00 / 00:00";

                if (autoPlay)
                {
                    Player.Play();
                    PlayCutWin.AppState.Instance.StatusMessage = $"Playing: {System.IO.Path.GetFileName(path)}";
                }
                else
                {
                    PlayCutWin.AppState.Instance.StatusMessage = $"Selected: {System.IO.Path.GetFileName(path)}";
                }
            }
            catch (Exception ex)
            {
                PlayCutWin.AppState.Instance.StatusMessage = $"Load failed: {ex.Message}";
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            _mediaReady = Player.NaturalDuration.HasTimeSpan;
            if (_mediaReady)
            {
                var dur = Player.NaturalDuration.TimeSpan;

                PlayCutWin.AppState.Instance.PlaybackDuration = dur;
                PlayCutWin.AppState.Instance.PlaybackPosition = Player.Position;

                _ignoreSeek = true;
                SeekSlider.Maximum = Math.Max(1, dur.TotalSeconds);
                SeekSlider.Value = 0;
                SeekSlider.IsEnabled = true;
                _ignoreSeek = false;

                UpdateTime();
                _timer.Start();
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Player.Stop();
            Player.Position = TimeSpan.Zero;

            PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
            PlayCutWin.AppState.Instance.StatusMessage = "Ended";
            UpdateTime();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                PlayCutWin.AppState.Instance.StatusMessage = "No video loaded.";
                return;
            }
            Player.Play();
            _timer.Start();
            PlayCutWin.AppState.Instance.StatusMessage = "Play";
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            Player.Pause();
            PlayCutWin.AppState.Instance.StatusMessage = "Pause";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Player.Stop();
            Player.Position = TimeSpan.Zero;

            PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
            PlayCutWin.AppState.Instance.StatusMessage = "Stop";
            UpdateTime();
        }

        private void UpdateTime()
        {
            if (!_mediaReady) return;

            var pos = Player.Position;
            var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;

            // AppState共有（Cブロックの時刻タグで使う）
            PlayCutWin.AppState.Instance.PlaybackPosition = pos;
            PlayCutWin.AppState.Instance.PlaybackDuration = dur;

            TimeText.Text = PlayCutWin.AppState.Instance.PlaybackPositionText;

            if (_isDragging) return;

            if (dur.TotalSeconds > 0)
            {
                _ignoreSeek = true;
                SeekSlider.Maximum = dur.TotalSeconds;
                SeekSlider.Value = Math.Min(dur.TotalSeconds, pos.TotalSeconds);
                _ignoreSeek = false;
            }
        }

        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_mediaReady) { _isDragging = false; return; }

            _isDragging = false;
            Player.Position = TimeSpan.FromSeconds(SeekSlider.Value);
            PlayCutWin.AppState.Instance.PlaybackPosition = Player.Position;
            UpdateTime();
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ignoreSeek) return;
            if (!_isDragging) return;

            // ドラッグ中のプレビュー時間も共有（Tagsに表示したい時に使える）
            PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.FromSeconds(SeekSlider.Value);
            TimeText.Text = PlayCutWin.AppState.Instance.PlaybackPositionText;
        }
    }
}
