using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public class AppState : INotifyPropertyChanged
    {
        // =========================
        // Singleton
        // =========================
        public static AppState Instance { get; } = new AppState();

        // ★ これが足りなかった1行（超重要）
        public static AppState Current => Instance;

        private AppState() { }

        // =========================
        // INotifyPropertyChanged
        // =========================
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // =========================
        // Videos
        // =========================
        public ObservableCollection<VideoItem> Videos { get; }
            = new ObservableCollection<VideoItem>();

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                if (_selectedVideo == value) return;
                _selectedVideo = value;
                Notify();
                Notify(nameof(SelectedVideoPath));
                Notify(nameof(CurrentTags));
            }
        }

        public string SelectedVideoPath
            => SelectedVideo?.Path ?? "(no clip selected)";

        // =========================
        // Playback
        // =========================
        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set
            {
                if (Math.Abs(_playbackSeconds - value) < 0.0001) return;
                _playbackSeconds = Math.Max(0, value);
                Notify();
                Notify(nameof(PlaybackPositionText));
            }
        }

        private double _playbackDuration;
        public double PlaybackDuration
        {
            get => _playbackDuration;
            set
            {
                if (Math.Abs(_playbackDuration - value) < 0.0001) return;
                _playbackDuration = Math.Max(0, value);
                Notify();
                Notify(nameof(PlaybackDurationText));
            }
        }

        public string PlaybackPositionText => FormatTime(PlaybackSeconds);
        public string PlaybackDurationText => FormatTime(PlaybackDuration);

        private static string FormatTime(double sec)
        {
            var ts = TimeSpan.FromSeconds(sec);
            return ts.TotalHours >= 1
                ? ts.ToString(@"hh\:mm\:ss")
                : ts.ToString(@"mm\:ss");
        }

        // =========================
        // Clip Range
        // =========================
        private double _clipStart;
        public double ClipStart
        {
            get => _clipStart;
            set { _clipStart = Math.Max(0, value); Notify(); }
        }

        private double _clipEnd;
        public double ClipEnd
        {
            get => _clipEnd;
            set { _clipEnd = Math.Max(0, value); Notify(); }
        }

        // =========================
        // Tags
        // =========================
        public Dictionary<VideoItem, List<string>> TagsByVideo { get; }
            = new Dictionary<VideoItem, List<string>>();

        public List<string> CurrentTags
        {
            get
            {
                if (SelectedVideo == null) return new List<string>();
                if (!TagsByVideo.ContainsKey(SelectedVideo))
                    return new List<string>();
                return TagsByVideo[SelectedVideo];
            }
        }

        public void AddTag(string tag)
        {
            if (SelectedVideo == null) return;
            if (string.IsNullOrWhiteSpace(tag)) return;

            if (!TagsByVideo.ContainsKey(SelectedVideo))
                TagsByVideo[SelectedVideo] = new List<string>();

            if (!TagsByVideo[SelectedVideo].Contains(tag))
                TagsByVideo[SelectedVideo].Add(tag);

            Notify(nameof(CurrentTags));
        }

        public void ClearTags()
        {
            if (SelectedVideo == null) return;
            TagsByVideo.Remove(SelectedVideo);
            Notify(nameof(CurrentTags));
        }
    }

    public class VideoItem
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
    }
}
