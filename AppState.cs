using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public class TagEntry
    {
        public string VideoPath { get; set; } = "";
        public double Seconds { get; set; }
        public string Tag { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string TimeText => AppState.FormatTime(Seconds);
    }

    public class AppState : INotifyPropertyChanged
    {
        // --- singleton (互換: Instance / Current 両方対応) ---
        public static AppState Instance { get; } = new AppState();
        public static AppState Current => Instance;

        private AppState() { }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // --- UI status ---
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // --- videos ---
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new ObservableCollection<VideoItem>();

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                _selectedVideo = value;
                OnPropertyChanged();
                SelectedVideoPath = _selectedVideo?.Path ?? "";
                OnPropertyChanged(nameof(SelectedVideoName));
            }
        }

        public string SelectedVideoName => SelectedVideo?.Name ?? "";

        private string _selectedVideoPath = "";
        public string SelectedVideoPath
        {
            get => _selectedVideoPath;
            set { _selectedVideoPath = value; OnPropertyChanged(); }
        }

        // --- playback info (ClipsView が更新) ---
        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set { _playbackSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlaybackPositionText)); }
        }

        private double _playbackDuration;
        public double PlaybackDuration
        {
            get => _playbackDuration;
            set { _playbackDuration = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlaybackDurationText)); }
        }

        public string PlaybackPositionText => FormatTime(PlaybackSeconds);
        public string PlaybackDurationText => FormatTime(PlaybackDuration);

        // --- tags ---
        public ObservableCollection<TagEntry> Tags { get; } = new ObservableCollection<TagEntry>();

        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            var exists = ImportedVideos.Any(v => string.Equals(v.Path, fullPath, StringComparison.OrdinalIgnoreCase));
            if (exists) return;

            ImportedVideos.Add(new VideoItem
            {
                Path = fullPath,
                Name = System.IO.Path.GetFileName(fullPath)
            });

            StatusMessage = $"Imported: {System.IO.Path.GetFileName(fullPath)}";
        }

        public void SetSelected(VideoItem? item)
        {
            SelectedVideo = item;
        }

        public void AddTag(string videoPath, double seconds, string tag)
        {
            if (string.IsNullOrWhiteSpace(videoPath)) return;
            if (string.IsNullOrWhiteSpace(tag)) return;

            Tags.Add(new TagEntry
            {
                VideoPath = videoPath,
                Seconds = seconds,
                Tag = tag.Trim(),
                CreatedAt = DateTime.Now
            });

            StatusMessage = $"Tag added: {tag.Trim()} @ {FormatTime(seconds)}";
        }

        public void ClearTagsForSelected()
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;

            var toRemove = Tags.Where(t => string.Equals(t.VideoPath, SelectedVideoPath, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var t in toRemove) Tags.Remove(t);

            StatusMessage = "Cleared tags for selected video.";
        }

        public static string FormatTime(double sec)
        {
            if (sec < 0) sec = 0;
            var ts = TimeSpan.FromSeconds(sec);
            if (ts.TotalHours >= 1) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        // --- export dummy helper ---
        public void ExportDummy(string targetFilePath, string content)
        {
            File.WriteAllText(targetFilePath, content ?? "");
            StatusMessage = $"Exported: {System.IO.Path.GetFileName(targetFilePath)}";
        }
    }
}
