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
        // 互換：Instance / Current の両対応
        public static AppState Instance { get; } = new AppState();
        public static AppState Current => Instance;

        private AppState() { }

        // -------------------------
        // INotifyPropertyChanged
        // -------------------------
        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // -------------------------
        // Videos
        // -------------------------
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new();

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                if (Equals(_selectedVideo, value)) return;
                _selectedVideo = value;
                Notify();
                Notify(nameof(SelectedVideoPath));
                Notify(nameof(SelectedVideoName));
                Notify(nameof(SelectedVideoText));
                Notify(nameof(CurrentTags));
            }
        }

        public string SelectedVideoPath => SelectedVideo?.Path ?? "";
        public string SelectedVideoName => SelectedVideo?.Name ?? "";
        public string SelectedVideoText => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(no clip selected)" : SelectedVideoPath;

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = path.Trim();

            if (ImportedVideos.Any(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase)))
            {
                // 既にあるなら選択だけ合わせる
                SetSelected(path);
                return;
            }

            var item = new VideoItem
            {
                Path = path,
                Name = Path.GetFileName(path)
            };

            ImportedVideos.Add(item);

            if (SelectedVideo == null)
                SelectedVideo = item;

            StatusMessage = $"Imported: {item.Name}";
        }

        public void SetSelected(string path)
        {
            var v = ImportedVideos.FirstOrDefault(x =>
                string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));

            if (v != null)
            {
                SelectedVideo = v;
                StatusMessage = $"Selected: {v.Name}";
            }
        }

        // -------------------------
        // Playback（今は“値だけ”を扱う。Player実装は後でOK）
        // -------------------------
        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set
            {
                var next = Math.Max(0, value);
                if (Math.Abs(_playbackSeconds - next) < 0.0001) return;
                _playbackSeconds = next;
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
                var next = Math.Max(0, value);
                if (Math.Abs(_playbackDuration - next) < 0.0001) return;
                _playbackDuration = next;
                Notify();
                Notify(nameof(PlaybackDurationText));
            }
        }

        public string PlaybackPositionText => FormatTime(PlaybackSeconds);
        public string PlaybackDurationText => FormatTime(PlaybackDuration);

        private static string FormatTime(double sec)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, sec));
            return ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
        }

        // -------------------------
        // Clip Range（左下Controlsで使う）
        // -------------------------
        private double _clipStart;
        public double ClipStart
        {
            get => _clipStart;
            set
            {
                var next = Math.Max(0, value);
                if (Math.Abs(_clipStart - next) < 0.0001) return;
                _clipStart = next;
                Notify();
            }
        }

        private double _clipEnd;
        public double ClipEnd
        {
            get => _clipEnd;
            set
            {
                var next = Math.Max(0, value);
                if (Math.Abs(_clipEnd - next) < 0.0001) return;
                _clipEnd = next;
                Notify();
            }
        }

        public void ResetRange()
        {
            ClipStart = 0;
            ClipEnd = 0;
            StatusMessage = "Range reset";
        }

        // -------------------------
        // Tags（動画ごと）
        // -------------------------
        private readonly Dictionary<string, ObservableCollection<TagEntry>> _tagsByVideoPath = new(StringComparer.OrdinalIgnoreCase);

        public ObservableCollection<TagEntry> CurrentTags
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedVideoPath))
                    return new ObservableCollection<TagEntry>();

                if (!_tagsByVideoPath.TryGetValue(SelectedVideoPath, out var list))
                {
                    list = new ObservableCollection<TagEntry>();
                    _tagsByVideoPath[SelectedVideoPath] = list;
                }
                return list;
            }
        }

        public void AddTagToSelected(string text)
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No clip selected";
                return;
            }

            text = (text ?? "").Trim();
            if (text.Length == 0) return;

            var entry = new TagEntry
            {
                VideoPath = SelectedVideoPath,
                Seconds = PlaybackSeconds,
                Text = text,
                CreatedAt = DateTime.Now
            };

            CurrentTags.Add(entry);
            StatusMessage = $"Tag added: {text}";
            Notify(nameof(CurrentTags));
        }

        public void RemoveSelectedTag(TagEntry entry)
        {
            if (entry == null) return;
            CurrentTags.Remove(entry);
            StatusMessage = "Tag removed";
            Notify(nameof(CurrentTags));
        }

        public void ClearTagsForSelected()
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No clip selected";
                return;
            }

            CurrentTags.Clear();
            StatusMessage = "Tags cleared";
            Notify(nameof(CurrentTags));
        }

        public IEnumerable<(VideoItem video, TagEntry tag)> EnumerateAllTags()
        {
            foreach (var v in ImportedVideos)
            {
                if (string.IsNullOrWhiteSpace(v.Path)) continue;
                if (_tagsByVideoPath.TryGetValue(v.Path, out var list))
                {
                    foreach (var t in list)
                        yield return (v, t);
                }
            }
        }

        // -------------------------
        // Status
        // -------------------------
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                Notify();
            }
        }
    }

    public sealed class VideoItem
    {
        public string Path { get; set; } = "";
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }

    public sealed class TagEntry
    {
        public string VideoPath { get; set; } = "";
        public double Seconds { get; set; }
        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; }

        public string TimeText
        {
            get
            {
                var ts = TimeSpan.FromSeconds(Math.Max(0, Seconds));
                return ts.TotalHours >= 1 ? ts.ToString(@"hh\:mm\:ss") : ts.ToString(@"mm\:ss");
            }
        }
    }
}
