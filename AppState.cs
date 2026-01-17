using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // 互換：Instance / Current どっちで呼ばれても同じ
        private static readonly AppState _instance = new AppState();
        public static AppState Instance => _instance;
        public static AppState Current => _instance;

        private AppState()
        {
            // 初期値（Macの見た目に近いデフォルト）
            TeamAName = "Home / Our Team";
            TeamBName = "Away / Opponent";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return;
            field = value;
            OnPropertyChanged(name);
        }

        // ----------------------------
        // Videos
        // ----------------------------
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new();

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                if (_selectedVideo == value) return;
                _selectedVideo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoPath));
                OnPropertyChanged(nameof(SelectedVideoName));
                OnPropertyChanged(nameof(VideoHeaderText));
                OnPropertyChanged(nameof(SelectedVideoPathText));
            }
        }

        public string? SelectedVideoPath => SelectedVideo?.Path;
        public string SelectedVideoName => SelectedVideo?.Name ?? "";

        // 左上ヘッダ（未選択なら "Video (16:9)"）
        public string VideoHeaderText => string.IsNullOrWhiteSpace(SelectedVideoName) ? "Video (16:9)" : SelectedVideoName;

        // Tagsエリアに表示する用（未選択なら空でOK）
        public string SelectedVideoPathText => SelectedVideoPath ?? "";

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            // 重複防止
            if (ImportedVideos.Any(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase)))
                return;

            ImportedVideos.Add(new VideoItem(path));

            // 最初の1本は自動選択
            if (SelectedVideo == null)
                SelectedVideo = ImportedVideos.FirstOrDefault();
        }

        public void SetSelected(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            var found = ImportedVideos.FirstOrDefault(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));
            if (found != null) SelectedVideo = found;
        }

        // ----------------------------
        // Team Names
        // ----------------------------
        private string _teamAName = "Home / Our Team";
        public string TeamAName { get => _teamAName; set => Set(ref _teamAName, value); }

        private string _teamBName = "Away / Opponent";
        public string TeamBName { get => _teamBName; set => Set(ref _teamBName, value); }

        // ----------------------------
        // Playback
        // ----------------------------
        private TimeSpan _playbackPosition = TimeSpan.Zero;
        public TimeSpan PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (_playbackPosition == value) return;
                _playbackPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackSeconds));
                OnPropertyChanged(nameof(PlaybackPositionText));
                OnPropertyChanged(nameof(TimeText));
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
                OnPropertyChanged(nameof(PlaybackDurationText));
                OnPropertyChanged(nameof(TimeText));
            }
        }

        public double PlaybackSeconds
        {
            get => PlaybackPosition.TotalSeconds;
            set
            {
                var s = Math.Max(0, value);
                PlaybackPosition = TimeSpan.FromSeconds(s);
            }
        }

        public string PlaybackPositionText => FormatTime(PlaybackPosition);
        public string PlaybackDurationText => FormatTime(PlaybackDuration);

        // 左下 Controls の "00:00 / 00:00"
        public string TimeText => $"{PlaybackPositionText} / {PlaybackDurationText}";

        private static string FormatTime(TimeSpan t)
        {
            // 1時間超は H:MM:SS
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
            return $"{t.Minutes:00}:{t.Seconds:00}";
        }

        // ----------------------------
        // Clip Start / End (秒)
        // ----------------------------
        private double _clipStartSeconds = 0;
        public double ClipStartSeconds { get => _clipStartSeconds; set { Set(ref _clipStartSeconds, Math.Max(0, value)); OnPropertyChanged(nameof(ClipStartText)); } }

        private double _clipEndSeconds = 0;
        public double ClipEndSeconds { get => _clipEndSeconds; set { Set(ref _clipEndSeconds, Math.Max(0, value)); OnPropertyChanged(nameof(ClipEndText)); } }

        public string ClipStartText => $"Start {FormatTime(TimeSpan.FromSeconds(ClipStartSeconds))}";
        public string ClipEndText => $"End {FormatTime(TimeSpan.FromSeconds(ClipEndSeconds))}";

        // ----------------------------
        // Tags
        // ----------------------------
        public ObservableCollection<string> Tags { get; } = new();

        public void AddTag(string tag)
        {
            tag = (tag ?? "").Trim();
            if (tag.Length == 0) return;
            if (!Tags.Contains(tag)) Tags.Add(tag);
        }

        public void ClearTagsForSelected()
        {
            Tags.Clear();
        }

        // ----------------------------
        // UI Status
        // ----------------------------
        private string _statusMessage = "Ready";
        public string StatusMessage { get => _statusMessage; set => Set(ref _statusMessage, value); }

        // ----------------------------
        // Clips (今は数だけ)
        // ----------------------------
        private int _clipsTotal = 0;
        public int ClipsTotal { get => _clipsTotal; set { Set(ref _clipsTotal, value); OnPropertyChanged(nameof(ClipsHeaderText)); } }

        public string ClipsHeaderText => $"Clips (Total {ClipsTotal})";
    }
}
