using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        private static readonly Lazy<AppState> _lazy = new(() => new AppState());
        public static AppState Instance => _lazy.Value;

        private AppState()
        {
            TeamAName = "";
            TeamBName = "";
            CurrentVideoPath = "";
            CurrentVideoFileName = "";
            PlaybackSeconds = 0;
            DurationSeconds = 0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ---- Video / Team ----
        private string _currentVideoPath = "";
        public string CurrentVideoPath
        {
            get => _currentVideoPath;
            set { _currentVideoPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasVideo)); }
        }

        private string _currentVideoFileName = "";
        public string CurrentVideoFileName
        {
            get => _currentVideoFileName;
            set { _currentVideoFileName = value; OnPropertyChanged(); OnPropertyChanged(nameof(VideoTitleText)); }
        }

        public bool HasVideo => !string.IsNullOrWhiteSpace(CurrentVideoPath);

        public string VideoTitleText
            => HasVideo ? CurrentVideoFileName : "Video (16:9)";

        private string _teamAName = "";
        public string TeamAName
        {
            get => _teamAName;
            set { _teamAName = value; OnPropertyChanged(); }
        }

        private string _teamBName = "";
        public string TeamBName
        {
            get => _teamBName;
            set { _teamBName = value; OnPropertyChanged(); }
        }

        // ---- Playback ----
        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set { _playbackSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlaybackPositionText)); }
        }

        private double _durationSeconds;
        public double DurationSeconds
        {
            get => _durationSeconds;
            set { _durationSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlaybackDurationText)); }
        }

        public string PlaybackPositionText => FormatTime(PlaybackSeconds);
        public string PlaybackDurationText => DurationSeconds > 0 ? FormatTime(DurationSeconds) : "00:00";

        public void SetVideo(string fullPath, string fileName)
        {
            CurrentVideoPath = fullPath ?? "";
            CurrentVideoFileName = fileName ?? "";
            PlaybackSeconds = 0;
            DurationSeconds = 0;
        }

        private static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            // 1時間超も見えるように
            return ts.TotalHours >= 1
                ? $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}"
                : $"{ts.Minutes:00}:{ts.Seconds:00}";
        }
    }
}
