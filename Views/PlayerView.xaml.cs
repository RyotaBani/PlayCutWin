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
        private bool _updatingFromPlayer; // Player -> AppState の更新中フラグ（ループ防止）

        public PlayerView()
        {
            InitializeComponent();
            DataContext = S;

            // Player -> AppState に再生位置を定期反映
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _timer.Tick += (_, __) => PullPositionFromPlayer();

            Loaded += (_, __) =>
            {
                if (_isLoaded) return;
                _isLoaded = true;

                S.PropertyChanged += OnAppStateChanged;
                RefreshSourceFromState(); // 起動時
                _timer.Start();
            };

            Unloaded += (_, __) =>
            {
                _timer.Stop();
                S.PropertyChanged -= OnAppStateChanged;

                try { Player.Stop(); } catch { /* ignore */ }
            };
        }

        // -----------------------------
        // AppState -> Player
        // -----------------------------
        private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SelectedVideo) ||
                e.PropertyName == nameof(AppState.SelectedVideoPath))
            {
                RefreshSourceFromState();
                return;
            }

            // Controls の ±0.5s などで PlaybackSeconds が変わったらシーク
            if (e.PropertyName == nameof(AppState.PlaybackSeconds))
            {
                if (_updatingFromPlayer) return; // 自分が更新した分で戻すのを防ぐ
                SeekToStateSeconds();
                return;
            }
        }

        private void RefreshSourceFromState()
        {
            var path = S.SelectedVideoPath;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                EmptyText.Visibility = System.Windows.Visibility.Visible;
                try
                {
                    Player.Stop();
                    Player.Source = null;
                }
                catch { /* ignore */ }

                S.PlaybackDuration = 0;
                S.PlaybackSeconds = 0;
                return;
            }

            EmptyText.Visibility = System.Windows.Visibility.Collapsed;

            try
            {
                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);

                // まずは「読み込みだけ」して止める（勝手に再生しない）
                // ※ 将来 Play/Pause ボタン実装でここを制御する
                Player.Play();
                Player.Pause();
            }
            catch
            {
                EmptyText.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void SeekToStateSeconds()
        {
            try
            {
                if (Player.Source == null) return;

                var sec = Math.Max(0, S.PlaybackSeconds);
                Player.Position = TimeSpan.FromSeconds(sec);
            }
            catch { /* ignore */ }
        }

        // -----------------------------
        // Player events
        // -----------------------------
        private void Player_MediaOpened(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    S.PlaybackDuration = Player.NaturalDuration.TimeSpan.TotalSeconds;
                }
                else
                {
                    S.PlaybackDuration = 0;
                }

                // 読み込み後、Stateの秒に合わせてシーク（初期0でもOK）
                SeekToStateSeconds();
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
        }

        // -----------------------------
        // Player -> AppState（再生中の位置反映）
        // -----------------------------
        private void PullPositionFromPlayer()
        {
            try
            {
                if (Player.Source == null) return;

                // 再生していなくても Position は取れる（PauseでもOK）
                var sec = Player.Position.TotalSeconds;

                // ループ防止：ここで State を更新したことを示す
                _updatingFromPlayer = true;
                S.PlaybackSeconds = sec;
            }
            catch
            {
                // ignore
            }
            finally
            {
                _updatingFromPlayer = false;
            }
        }
    }
}
