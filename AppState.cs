using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    // ------------------------------------------------------------
    // App-wide shared state (simple MVVM-ish singleton)
    // ------------------------------------------------------------
    public sealed class AppState : INotifyPropertyChanged
    {
        // ✅ singleton
        public static AppState Instance { get; } = new AppState();

        // ✅ compatibility alias
        public static AppState Current => Instance;

        private AppState()
        {
            ImportedVideos = new ObservableCollection<VideoItem>();
            Tags = new ObservableCollection<string>();
            TagEntries = new ObservableCollection<TagEntry>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        // ------------------------------------------------------------
        // Status
        // ------------------------------------------------------------
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        // ------------------------------------------------------------
        // Imported videos
        // ------------------------------------------------------------
        public ObservableCollection<VideoItem> ImportedVideos { get; }

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                if (Set(ref _selectedVideo, value))
                {
                    SelectedVideoPath = _selectedVideo?.Path ?? "";
                    SelectedVideoName = _selectedVideo?.Name ?? "";
                }
            }
        }

        private string _selectedVideoPath = "";
        public string SelectedVideoPath
        {
            get => _selectedVideoPath;
            set => Set(ref _selectedVideoPath, value);
        }

        private string _selectedVideoName = "";
        public string SelectedVideoName
        {
            get => _selectedVideoName;
            set => Set(ref _selectedVideoName, value);
        }

        // 互換：古いコードが呼んでも落ちないように
        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            var item = new VideoItem(path);
            if (!ImportedVideos.Any(v => string.Equals(v.Path, item.Path, StringComparison.OrdinalIgnoreCase)))
            {
                ImportedVideos.Add(item);
                StatusMessage = $"Imported: {item.Name}";
            }

            if (SelectedVideo == null)
                SelectedVideo = ImportedVideos.LastOrDefault();
        }

        // 互換：Views 側から呼ばれがち
        public void SetSelected(VideoItem? item)
        {
            SelectedVideo = item;
            if (item != null)
                StatusMessage = $"Selected: {item.Name}";
        }

        public void SetSelected(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SelectedVideo = null;
                return;
            }

            var item = ImportedVideos.FirstOrDefault(v =>
                string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));

            if (item == null)
            {
                item = new VideoItem(path);
                ImportedVideos.Add(item);
            }

            SelectedVideo = item;
            StatusMessage = $"Selected: {item.Name}";
        }

        // ------------------------------------------------------------
        // Playback info (ClipsView の player 表示用)
        // ------------------------------------------------------------
        private TimeSpan _playbackPosition = TimeSpan.Zero;
        public TimeSpan PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (Set(ref _playbackPosition, value))
                    OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        private TimeSpan _playbackDuration = TimeSpan.Zero;
        public TimeSpan PlaybackDuration
        {
            get => _playbackDuration;
            set
            {
                if (Set(ref _playbackDuration, value))
                    OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        // 互換：秒で扱うコード用
        public double PlaybackSeconds
        {
            get => PlaybackPosition.TotalSeconds;
            set => PlaybackPosition = TimeSpan.FromSeconds(Math.Max(0, value));
        }

        public string PlaybackPositionText
            => $"{FormatTime(PlaybackPosition)} / {FormatTime(PlaybackDuration)}";

        public static string FormatTime(TimeSpan t)
        {
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
            return $"{t.Minutes:00}:{t.Seconds:00}";
        }

        // ------------------------------------------------------------
        // Tags (TagsView 用)
        // ------------------------------------------------------------
        public ObservableCollection<string> Tags { get; }

        public void AddTag(string? tag)
        {
            tag = (tag ?? "").Trim();
            if (tag.Length == 0) return;

            if (!Tags.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                Tags.Add(tag);
                StatusMessage = $"Tag added: {tag}";
            }
        }

        public void ClearTags()
        {
            Tags.Clear();
            StatusMessage = "Tags cleared";
        }

        // ------------------------------------------------------------
        // Tag entries (ExportsView / TagsView 互換)
        // ------------------------------------------------------------
        public ObservableCollection<TagEntry> TagEntries { get; }

        // 互換：この名前で呼ばれてもOKにする（将来拡張用）
        public void AddTagEntry(TagEntry entry)
        {
            if (entry == null) return;
            TagEntries.Add(entry);
            StatusMessage = $"TagEntry added: {entry.Tag}";
        }
    }

    // ------------------------------------------------------------
    // Simple model used by Clips/Exports list
    // ------------------------------------------------------------
    public sealed class VideoItem
    {
        public VideoItem() { }

        public VideoItem(string path)
        {
            Path = path ?? "";
            Name = System.IO.Path.GetFileName(Path);
            Ext = System.IO.Path.GetExtension(Path).TrimStart('.').ToLowerInvariant();
            CreatedAt = DateTime.Now;
        }

        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public string Ext { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public string DisplayName => Name;
    }

    // ------------------------------------------------------------
    // Tag model (ExportsView.xaml.cs が参照する想定の型)
    // ------------------------------------------------------------
    public sealed class TagEntry
    {
        public string VideoPath { get; set; } = "";
        public string Tag { get; set; } = "";

        // クリップ範囲（秒）
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }

        public string Note { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // 表示/CSV用
        public string RangeText
        {
            get
            {
                var s = AppState.FormatTime(TimeSpan.FromSeconds(Math.Max(0, StartSeconds)));
                var e = AppState.FormatTime(TimeSpan.FromSeconds(Math.Max(0, EndSeconds)));
                return $"{s} - {e}";
            }
        }
    }
}
