using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // 既存参照互換：AppState.Instance
        public static AppState Instance { get; } = new AppState();

        // 新参照互換：AppState.Current
        public static AppState Current => Instance;

        private AppState()
        {
            ImportedVideos = new ObservableCollection<VideoItem>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(name);
        }

        // -----------------------------
        // Status
        // -----------------------------
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        // -----------------------------
        // Imported videos
        // -----------------------------
        public ObservableCollection<VideoItem> ImportedVideos { get; }

        // -----------------------------
        // Selected video (互換: SelectedVideo / SelectedVideoPath / SelectedVideoText)
        // -----------------------------
        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                if (ReferenceEquals(_selectedVideo, value)) return;
                _selectedVideo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoPath));
                OnPropertyChanged(nameof(SelectedVideoName));
                OnPropertyChanged(nameof(SelectedVideoText));
                OnPropertyChanged(nameof(TagsForSelected));
                OnPropertyChanged(nameof(Tags));
            }
        }

        public string? SelectedVideoPath => SelectedVideo?.Path;

        public string SelectedVideoName
            => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(none)" : Path.GetFileName(SelectedVideoPath);

        public string SelectedVideoText
            => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(no video selected)" : SelectedVideoPath!;

        // 互換: SetSelected(string) / SetSelected(VideoItem)
        public void SetSelected(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                SelectedVideo = null;
                StatusMessage = "Selected: (none)";
                return;
            }

            var hit = ImportedVideos.FirstOrDefault(v =>
                string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));

            if (hit == null)
            {
                // pathだけ来た場合でも選択できるようにする（落とさない）
                hit = new VideoItem { Name = Path.GetFileName(path), Path = path };
            }

            SelectedVideo = hit;
            StatusMessage = $"Selected: {SelectedVideoName}";
        }

        public void SetSelected(VideoItem? item)
        {
            SelectedVideo = item;
            StatusMessage = item == null ? "Selected: (none)" : $"Selected: {SelectedVideoName}";
        }

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            var existing = ImportedVideos.FirstOrDefault(v =>
                string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                ImportedVideos.Remove(existing);
                ImportedVideos.Insert(0, existing);
                SetSelected(existing);
                return;
            }

            var item = new VideoItem
            {
                Name = Path.GetFileName(path),
                Path = path
            };

            ImportedVideos.Insert(0, item);
            SetSelected(item);
        }

        // -----------------------------
        // Playback (互換: PlaybackSeconds / PlaybackDuration / PlaybackPositionText)
        // -----------------------------
        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set
            {
                if (Math.Abs(_playbackSeconds - value) < 0.0001) return;
                _playbackSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        private double _playbackDuration;
        public double PlaybackDuration
        {
            get => _playbackDuration;
            set
            {
                if (Math.Abs(_playbackDuration - value) < 0.0001) return;
                _playbackDuration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        public string PlaybackPositionText
            => $"{FormatTime(PlaybackSeconds)} / {FormatTime(PlaybackDuration)}";

        public static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            // 01:23:45 か 12:34 を出し分け
            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}";
            return $"{ts.Minutes:00}:{ts.Seconds:00}";
        }

        // -----------------------------
        // Tags (互換: Tags / TagsForSelected / AddTagForSelected / ClearTagsForSelected / GetTags)
        // -----------------------------
        private readonly Dictionary<string, ObservableCollection<string>> _tagsByVideo = new();

        public ObservableCollection<string> TagsForSelected
        {
            get
            {
                var key = SelectedVideoPath ?? "";
                if (string.IsNullOrWhiteSpace(key))
                    return new ObservableCollection<string>(); // 選択なしでも落とさない

                if (!_tagsByVideo.TryGetValue(key, out var list))
                {
                    list = new ObservableCollection<string>();
                    _tagsByVideo[key] = list;
                }
                return list;
            }
        }

        // 互換用：selected の tags を List<string> で返すプロパティ
        public List<string> Tags => TagsForSelected.ToList();

        public void AddTagForSelected(string tagText)
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No selected video.";
                return;
            }

            tagText = (tagText ?? "").Trim();
            if (tagText.Length == 0) return;

            TagsForSelected.Add(tagText);

            StatusMessage = $"Tag added: {tagText}";
            OnPropertyChanged(nameof(TagsForSelected));
            OnPropertyChanged(nameof(Tags));
        }

        public void ClearTagsForSelected()
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No selected video.";
                return;
            }

            TagsForSelected.Clear();

            StatusMessage = "Tags cleared.";
            OnPropertyChanged(nameof(TagsForSelected));
            OnPropertyChanged(nameof(Tags));
        }

        public IReadOnlyList<string> GetTags(string? videoPath)
        {
            if (string.IsNullOrWhiteSpace(videoPath)) return Array.Empty<string>();
            if (_tagsByVideo.TryGetValue(videoPath, out var list)) return list.ToList();
            return Array.Empty<string>();
        }
    }

    // 既存で参照される型
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }
}
