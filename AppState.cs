using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // ---- Singleton ----
        public static AppState Instance { get; } = new AppState();
        private AppState() { }

        // ---- Events (UI -> Playerへ命令を投げる) ----
        public event EventHandler? RequestPlayPause;
        public event EventHandler? RequestStop;
        public event EventHandler<double>? RequestSeekRelative; // seconds (+/-)
        public event EventHandler<double>? RequestRate;         // 0.25 / 0.5 / 1 / 2 etc

        // ---- Properties (UI表示用) ----
        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set { if (_isPlaying != value) { _isPlaying = value; OnPropertyChanged(); } }
        }

        private double _playbackRate = 1.0;
        public double PlaybackRate
        {
            get => _playbackRate;
            set { if (Math.Abs(_playbackRate - value) > 0.0001) { _playbackRate = value; OnPropertyChanged(); } }
        }

        private TimeSpan _position = TimeSpan.Zero;
        public TimeSpan Position
        {
            get => _position;
            set { if (_position != value) { _position = value; OnPropertyChanged(); OnPropertyChanged(nameof(PositionText)); } }
        }

        private TimeSpan _duration = TimeSpan.Zero;
        public TimeSpan Duration
        {
            get => _duration;
            set { if (_duration != value) { _duration = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationText)); } }
        }

        public string PositionText => $"{(int)Position.TotalMinutes:00}:{Position.Seconds:00}";
        public string DurationText => $"{(int)Duration.TotalMinutes:00}:{Duration.Seconds:00}";

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        // ---- Methods (Controlsから呼ぶ) ----
        public void SendPlayPause()
            => RequestPlayPause?.Invoke(this, EventArgs.Empty);

        public void SendStop()
            => RequestStop?.Invoke(this, EventArgs.Empty);

        public void SendSeekRelative(double seconds)
            => RequestSeekRelative?.Invoke(this, seconds);

        public void SendRate(double rate)
        {
            PlaybackRate = rate; // UI側も更新しておく
            RequestRate?.Invoke(this, rate);
        }

        // ---- INotifyPropertyChanged ----
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
