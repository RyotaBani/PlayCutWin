using System;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private bool _isDragging = false;
        private TimeSpan _duration = TimeSpan.Zero;

        public ClipsView()
        {
            InitializeComponent();
            DataContext = AppState.Current;

            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += (_, __) => UpdateTime();
            _timer.Start();
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideosGrid.SelectedItem is not PlayCutWin.VideoItem item) return;

            AppState.Current.SetSelected(item.Path);
            LoadAndPlay(item.Path);
        }

        private void LoadAndPlay(string path)
        {
            try
            {
                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);
                Player.Play();
                AppState.Current.StatusMessage = $"Playing: {System.IO.Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to play.\n{ex.Message}", "PlayCut", MessageBoxButton.OK, MessageBoxImage.Error);
                AppState.Current.StatusMessage = "Play failed";
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                _duration = Player.NaturalDuration.TimeSpan;
                Seek.Maximum = Math.Max(1, _duration.TotalSeconds);
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            Player.Stop();
        }

        private void Play_Click(object sender, RoutedEventArgs e) => Player.Play();
        private void Pause_Click(object sender, RoutedEventArgs e) => Player.Pause();
        private void Stop_Click(object sender, RoutedEventArgs e) => Player.Stop();

        private void Seek_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void Seek_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_duration.Equals(TimeSpan.Zero))
            {
                Player.Position = TimeSpan.FromSeconds(Seek.Value);
            }
            _isDragging = false;
        }

        private void UpdateTime()
        {
            if (_isDragging) return;

            if (_duration.TotalSeconds > 0)
            {
                Seek.Value = Player.Position.TotalSeconds;
                TimeText.Text = $"{Fmt(Player.Position)} / {Fmt(_duration)}";
            }
            else
            {
                TimeText.Text = "00:00 / 00:00";
            }
        }

        private static string Fmt(TimeSpan t)
        {
            if (t.TotalHours >= 1) return t.ToString(@"hh\:mm\:ss");
            return t.ToString(@"mm\:ss");
        }
    }
}
