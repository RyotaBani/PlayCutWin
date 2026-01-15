using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    // Imported video row model
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    // Simple tag model (A block)
    public class TagItem
    {
        public string Text { get; set; } = "";
        public string Time { get; set; } = ""; // placeholder (e.g. "00:12") -> Cブロックで本物にする
    }

    // App-wide shared state
    public class AppState : INotifyPropertyChanged
    {
        private static readonly AppState _instance = new AppState();
        public static AppState Instance => _instance;

        // Compatibility alias
        public static AppState Current => _instance;

        // ---- Status ----
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // ---- Selected video ----
        private string _selectedVideoPath = "";
        public string SelectedVideoPath
        {
            get => _selectedVideoPath;
            set
            {
                if (_selectedVideoPath == value) return;
                _selectedVideoPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoName));
            }
        }

        public string SelectedVideoName
            => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(none)" : System.IO.Path.GetFileName(SelectedVideoPath);

        // ---- Collections ----
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new ObservableCollection<VideoItem>();
        public ObservableCollection<TagItem> Tags { get; } = new ObservableCollection<TagItem>();

        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            ImportedVideos.Add(new VideoItem
            {
                Name = System.IO.Path.GetFileName(fullPath),
                Path = fullPath
            });

            StatusMessage = $"Imported: {System.IO.Path.GetFileName(fullPath)}";
        }

        public void SetSelected(string fullPath)
        {
            SelectedVideoPath = fullPath ?? "";
            StatusMessage = string.IsNullOrWhiteSpace(SelectedVideoPath)
                ? "Selected: (none)"
                : $"Selected: {System.IO.Path.GetFileName(SelectedVideoPath)}";
        }

        public void AddTag(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            Tags.Add(new TagItem { Text = text.Trim(), Time = "--:--" });
            StatusMessage = $"Tag added: {text.Trim()}";
        }

        // ---- Playback shared state (B block) ----
        private TimeSpan _playbackPosition = TimeSpan.Zero;
        public TimeSpan PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (_playbackPosition == value) return;
                _playbackPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        private TimeSpan _playbackDuration = TimeSpan.Zero;
        public TimeSpan PlaybackDuration
        {
            get => _playbackDuration;
            set
            {
                if (_playbackDuration == value) return;
                _playbackDuration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        public string PlaybackPositionText => $"{Fmt(PlaybackPosition)} / {FmtOrDash(PlaybackDuration)}";

        private static string Fmt(TimeSpan t)
        {
            int total = (int)Math.Max(0, t.TotalSeconds);
            int mm = total / 60;
            int ss = total % 60;
            return $"{mm:00}:{ss:00}";
        }

        private static string FmtOrDash(TimeSpan t)
        {
            if (t.TotalSeconds <= 0) return "--:--";
            return Fmt(t);
        }

        // ---- INotifyPropertyChanged ----
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
