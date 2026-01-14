using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        public static AppState Current { get; } = new AppState();
        public static AppState Instance => Current;

        private AppState() { }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

        // ---- Imported videos ----
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new ObservableCollection<VideoItem>();

        // ---- Selected video ----
        private string? _selectedVideoPath;
        public string? SelectedVideoPath
        {
            get => _selectedVideoPath;
            set
            {
                if (_selectedVideoPath == value) return;
                _selectedVideoPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoName));

                // 動画を切り替えたら、その動画のタグ一覧に切り替え
                SyncTagsForSelectedVideo();
            }
        }

        public string SelectedVideoName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return "";
                return Path.GetFileName(SelectedVideoPath);
            }
        }

        // ---- Playback position shared ----
        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set
            {
                if (Math.Abs(_playbackSeconds - value) < 0.001) return;
                _playbackSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackTimeText));
            }
        }

        public string PlaybackTimeText
        {
            get
            {
                var t = TimeSpan.FromSeconds(Math.Max(0, PlaybackSeconds));
                if (t.TotalHours >= 1) return t.ToString(@"hh\:mm\:ss");
                return t.ToString(@"mm\:ss");
            }
        }

        // ---- Tags ----
        // 選択中動画のタグ（表示用）
        public ObservableCollection<TagItem> Tags { get; } = new ObservableCollection<TagItem>();

        // 全動画ぶんのタグを保持（キー：動画パス）
        private readonly Dictionary<string, ObservableCollection<TagItem>> _tagsByVideoPath
            = new Dictionary<string, ObservableCollection<TagItem>>(StringComparer.OrdinalIgnoreCase);

        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            var item = new VideoItem
            {
                Name = Path.GetFileName(fullPath),
                Path = fullPath
            };

            ImportedVideos.Add(item);

            // 追加したらそれを選択にする
            SetSelected(fullPath);

            StatusMessage = $"Imported: {item.Name}";
        }

        public void SetSelected(string? fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;
            SelectedVideoPath = fullPath;
            StatusMessage = $"Selected: {Path.GetFileName(fullPath)}";
        }

        private void SyncTagsForSelectedVideo()
        {
            Tags.Clear();

            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;

            if (!_tagsByVideoPath.TryGetValue(SelectedVideoPath, out var list))
            {
                list = new ObservableCollection<TagItem>();
                _tagsByVideoPath[SelectedVideoPath] = list;
            }

            foreach (var t in list) Tags.Add(t);
        }

        public void AddTagToSelectedVideo(string tagText)
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;
            tagText = tagText.Trim();
            if (string.IsNullOrWhiteSpace(tagText)) return;

            if (!_tagsByVideoPath.TryGetValue(SelectedVideoPath, out var list))
            {
                list = new ObservableCollection<TagItem>();
                _tagsByVideoPath[SelectedVideoPath] = list;
            }

            var item = new TagItem
            {
                TimeSeconds = PlaybackSeconds,
                TimeText = PlaybackTimeText,
                Tag = tagText
            };

            list.Add(item);
            Tags.Add(item);

            StatusMessage = $"Tag added: {tagText} @ {item.TimeText}";
        }
    }

    public sealed class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public sealed class TagItem
    {
        public double TimeSeconds { get; set; }
        public string TimeText { get; set; } = "";
        public string Tag { get; set; } = "";
    }
}
