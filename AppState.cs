using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // Singleton
        private static readonly AppState _current = new AppState();
        public static AppState Current => _current;

        // 互換：以前のコードが Instance を参照しても通るように
        public static AppState Instance => _current;

        private AppState()
        {
            ImportedVideos = new ObservableCollection<VideoItem>();
            Tags = new ObservableCollection<TagEntry>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ---------------------------
        // Shared: Videos
        // ---------------------------
        public ObservableCollection<VideoItem> ImportedVideos { get; }

        private string? _selectedVideoPath;
        public string? SelectedVideoPath
        {
            get => _selectedVideoPath;
            set
            {
                if (_selectedVideoPath == value) return;
                _selectedVideoPath = value;
                OnPropertyChanged(nameof(SelectedVideoPath));
                OnPropertyChanged(nameof(SelectedVideoName));
                RefreshTagsForSelected();
            }
        }

        public string SelectedVideoName =>
            string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(none)" : Path.GetFileName(SelectedVideoPath);

        // 互換：呼び出し側が AppState.SetSelected(...) を使っても動く
        public void SetSelected(string? path)
        {
            SelectedVideoPath = path;
            StatusMessage = string.IsNullOrWhiteSpace(path)
                ? "Selected: (none)"
                : $"Selected: {Path.GetFileName(path)}";
        }

        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;
            if (ImportedVideos.Any(v => string.Equals(v.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
                return;

            ImportedVideos.Add(new VideoItem
            {
                Name = Path.GetFileName(fullPath),
                Path = fullPath
            });

            StatusMessage = $"Imported: {Path.GetFileName(fullPath)}";

            // まだ未選択なら自動選択
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                SetSelected(fullPath);
            }
        }

        // ---------------------------
        // Shared: Status
        // ---------------------------
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        // ---------------------------
        // Shared: Playback Position (ClipsView が更新する想定)
        // ---------------------------
        private double _playbackSeconds = 0;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set
            {
                if (Math.Abs(_playbackSeconds - value) < 0.001) return;
                _playbackSeconds = value;
                OnPropertyChanged(nameof(PlaybackSeconds));
                OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        public string PlaybackPositionText => FormatTime(PlaybackSeconds);

        // ---------------------------
        // Tags
        // ---------------------------
        // videoPath -> tags
        private readonly Dictionary<string, List<TagEntry>> _tagsByVideo =
            new Dictionary<string, List<TagEntry>>(StringComparer.OrdinalIgnoreCase);

        // UI がそのまま ItemsSource にできる “選択中動画のタグ一覧”
        public ObservableCollection<TagEntry> Tags { get; }

        public void AddTag(string tagText)
        {
            tagText = (tagText ?? "").Trim();
            if (string.IsNullOrWhiteSpace(tagText))
            {
                StatusMessage = "Tag is empty.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No video selected.";
                return;
            }

            var entry = new TagEntry
            {
                VideoPath = SelectedVideoPath!,
                Seconds = PlaybackSeconds,
                Tag = tagText,
                CreatedAt = DateTime.Now
            };

            if (!_tagsByVideo.TryGetValue(SelectedVideoPath!, out var list))
            {
                list = new List<TagEntry>();
                _tagsByVideo[SelectedVideoPath!] = list;
            }

            list.Add(entry);
            list.Sort((a, b) => a.Seconds.CompareTo(b.Seconds)); // 時刻順

            RefreshTagsForSelected();
            StatusMessage = $"Tag added: [{FormatTime(entry.Seconds)}] {entry.Tag}";
        }

        public void ClearTagsForSelected()
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No video selected.";
                return;
            }

            _tagsByVideo.Remove(SelectedVideoPath!);
            RefreshTagsForSelected();
            StatusMessage = "Tags cleared.";
        }

        private void RefreshTagsForSelected()
        {
            Tags.Clear();

            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;

            if (_tagsByVideo.TryGetValue(SelectedVideoPath!, out var list))
            {
                foreach (var t in list)
                    Tags.Add(t);
            }

            OnPropertyChanged(nameof(Tags));
        }

        // ✅ Export All 用：全動画分のタグをフラットにして返す
        public List<TagEntry> GetAllTagsSnapshot()
        {
            var all = new List<TagEntry>();

            foreach (var kv in _tagsByVideo)
                all.AddRange(kv.Value);

            all.Sort((a, b) =>
            {
                var c = string.Compare(a.VideoPath, b.VideoPath, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                return a.Seconds.CompareTo(b.Seconds);
            });

            return all;
        }

        // ---------------------------
        // Helpers
        // ---------------------------
        public static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
        }
    }

    public sealed class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public sealed class TagEntry
    {
        public string VideoPath { get; set; } = "";
        public double Seconds { get; set; }
        public string Tag { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public string TimeText => AppState.FormatTime(Seconds);
    }
}
