using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class PlayerView : UserControl
    {
        private AppState S => AppState.Instance;

        private readonly DispatcherTimer _timer;
        private bool _isLoaded;
        private bool _updatingFromPlayer;

        public PlayerView()
        {
            InitializeComponent();
            DataContext = S;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _timer.Tick += (_, __) => PullPositionFromPlayer();

            Loaded += (_, __) =>
            {
                if (_isLoaded) return;
                _isLoaded = true;

                S.PropertyChanged += OnAppStateChanged;
                RefreshSourceFromState();
                _timer.Start();
            };

            Unloaded += (_, __) =>
            {
                _timer.Stop();
                S.PropertyChanged -= OnAppStateChanged;
                try { Player.Stop(); } catch { }
            };
        }

        private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SelectedVideo) ||
                e.PropertyName == nameof(AppState.SelectedVideoPath))
            {
                RefreshSourceFromState();
                return;
            }

            if (e.PropertyName == nameof(AppState.PlaybackSeconds))
            {
                if (_updatingFromPlayer) return;
                SeekToStateSeconds();
                return;
            }

            if (e.PropertyName == nameof(AppState.IsPlaying))
            {
                ApplyPlayState();
                return;
            }
        }

        private void RefreshSourceFromState()
        {
            var path = S.SelectedVideoPath;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                EmptyText.Visibility = System.Windows.Visibility.Visible;
                try { Player.Stop(); Player.Source = null; } catch { }

                S.PlaybackDuration = 0;
                S.PlaybackSeconds = 0;
                S.IsPlaying = false;
                return;
            }

            EmptyText.Visibility = System.Windows.Visibility.Collapsed;

            try
            {
                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);

                // 読み込みのため一瞬 Play→Pause（勝手に再生しない）
                Player.Play();
                Player.Pause();

                ApplyPlayState();
            }
            catch
            {
                EmptyText.Visibility = System.Windows.Visibility.Visible;
                S.IsPlaying = false;
            }
        }

        private void ApplyPlayState()
        {
            try
            {
                if (Player.Source == null) return;

                if (S.IsPlaying)
                    Player.Play();
                else
                    Player.Pause();
            }
            catch { }
        }

        private void SeekToStateSeconds()
        {
            try
            {
                if (Player.Source == null) return;
                Player.Position = TimeSpan.FromSeconds(Math.Max(0, S.PlaybackSeconds));
            }
            catch { }
        }

        private void Player_MediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                S.PlaybackDuration = Player.NaturalDuration.HasTimeSpan
                    ? Player.NaturalDuration.TimeSpan.TotalSeconds
                    : 0;

                SeekToStateSeconds();
                ApplyPlayState();
            }
            catch
            {
                S.PlaybackDuration = 0;
            }
        }

        private void Player_MediaFailed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {
            EmptyText.Visibility = System.Windows.Visibility.Visible;
            S.StatusMessage = $"Media failed: {e.ErrorException?.Message}";
            S.PlaybackDuration = 0;
            S.PlaybackSeconds = 0;
            S.IsPlaying = false;
        }

        private void PullPositionFromPlayer()
        {
            try
            {
                if (Player.Source == null) return;

                var sec = Player.Position.TotalSeconds;

                _updatingFromPlayer = true;
                S.PlaybackSeconds = sec;
            }
            catch { }
            finally
            {
                _updatingFromPlayer = false;
            }
        }
    }
}
