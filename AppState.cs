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
        // ========= Tag =========
        public sealed class TagEntry
        {
            public string Name { get; set; } = "";
            public string Category { get; set; } = "Offense";
            public override string ToString() => Name;

            // XAML/コード側で TagEntry を string として扱われても落ちないようにする
            public static implicit operator string(TagEntry e) => e?.Name ?? "";
        }

        // ========= Singleton =========
        public static AppState Instance { get; } = new AppState();

        private AppState()
        {
            Tags = new ObservableCollection<TagEntry>();
            SelectedTags = new ObservableCollection<string>();
            Clips = new ObservableCollection<ClipItem>();

            TeamAName = "Home / Our Team";
            TeamBName = "Away / Opponent";

            StatusMessage = "Ready";
            PlaybackSpeed = 1.0;
        }

        // ========= INotifyPropertyChanged =========
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ========= UI Text =========
        public string VideoHeaderText => "Video (16:9)";

        // ========= Teams =========
        private string _teamAName = "";
        public string TeamAName
        {
            get => _teamAName;
            set { _teamAName = value ?? ""; OnPropertyChanged(); }
        }

        private string _teamBName = "";
        public string TeamBName
        {
            get => _teamBName;
            set { _teamBName = value ?? ""; OnPropertyChanged(); }
        }

        // ========= Video =========
        private string _videoPath = "";
        public string VideoPath
        {
            get => _videoPath;
            set
            {
                _videoPath = value ?? "";
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasVideo));
            }
        }

        public bool HasVideo => !string.IsNullOrWhiteSpace(VideoPath) && File.Exists(VideoPath);

        // ========= Playback =========
        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayPauseGlyph));
            }
        }

        // 再生中=⏸ / 停止中=▶（要望どおり）
        public string PlayPauseGlyph => IsPlaying ? "⏸" : "▶";

        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set
            {
                _playbackSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackTimeText));
            }
        }

        private double _durationSeconds;
        public double DurationSeconds
        {
            get => _durationSeconds;
            set
            {
                _durationSeconds = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationTimeText));
                OnPropertyChanged(nameof(PlaybackDuration));
            }
        }

        public string PlaybackTimeText => FormatTime(PlaybackSeconds);
        public string DurationTimeText => FormatTime(DurationSeconds);

        private double _playbackSpeed = 1.0;
        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set { _playbackSpeed = value; OnPropertyChanged(); }
        }

        // DashboardView が set してくることがあるので set 可能にして吸収
        public double PlaybackDuration
        {
            get => DurationSeconds;
            set => DurationSeconds = value;
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value ?? ""; OnPropertyChanged(); }
        }

        // ========= Clip Range =========
        private double? _clipStart;
        public double? ClipStart
        {
            get => _clipStart;
            set
            {
                _clipStart = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClipStartText));
            }
        }

        private double? _clipEnd;
        public double? ClipEnd
        {
            get => _clipEnd;
            set
            {
                _clipEnd = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClipEndText));
            }
        }

        public string ClipStartText => ClipStart.HasValue ? FormatTime(ClipStart.Value) : "--:--";
        public string ClipEndText => ClipEnd.HasValue ? FormatTime(ClipEnd.Value) : "--:--";

        // ========= Collections =========
        public ObservableCollection<TagEntry> Tags { get; }
        public ObservableCollection<string> SelectedTags { get; }

        public sealed class ClipItem
        {
            public string Team { get; set; } = "A"; // "A" or "B"
            public double Start { get; set; }
            public double End { get; set; }
            public ObservableCollection<string> Tags { get; set; } = new ObservableCollection<string>();
            public string SetPlay { get; set; } = "";
            public string Note { get; set; } = "";
        }

        public ObservableCollection<ClipItem> Clips { get; }

        // ========= Player Commands (PlayerViewが購読して実行する) =========
        public event EventHandler? RequestPlayPause;
        public event EventHandler? RequestStop;
        public event EventHandler<double>? RequestSeekRelative;
        public event EventHandler<double>? RequestRate;

        // ========= Methods expected by Views =========

        // 動画読み込み後の初期化（DashboardViewなどから呼ばれる想定）
        public void AddImportedVideo(string path)
        {
            VideoPath = path ?? "";
            PlaybackSeconds = 0;
            DurationSeconds = 0;
            IsPlaying = false;
            ClipStart = null;
            ClipEnd = null;
            StatusMessage = HasVideo ? "Media opened" : "No video loaded";
        }

        // PlayerControlsView から呼ばれる
        public void SendPlayPause() => RequestPlayPause?.Invoke(this, EventArgs.Empty);
        public void SendStop() => RequestStop?.Invoke(this, EventArgs.Empty);
        public void SendSeekRelative(double deltaSeconds) => RequestSeekRelative?.Invoke(this, deltaSeconds);

        public void SendRate(double rate)
        {
            PlaybackSpeed = rate;
            RequestRate?.Invoke(this, rate);
        }

        // Clip START/END（Mac名に寄せる）
        public void SetClipStart()
        {
            ClipStart = PlaybackSeconds;
            StatusMessage = $"Clip START: {ClipStartText}";
        }

        public void SetClipEnd()
        {
            ClipEnd = PlaybackSeconds;
            StatusMessage = $"Clip END: {ClipEndText}";
        }

        public void ResetClipRange()
        {
            ClipStart = null;
            ClipEnd = null;
            StatusMessage = "Clip range reset";
        }

        // Save Team A/B
        public void SaveClip(string team)
        {
            var t = string.IsNullOrWhiteSpace(team) ? "A" : team.Trim().ToUpperInvariant();
            if (t != "A" && t != "B") t = "A";

            if (!ClipStart.HasValue || !ClipEnd.HasValue)
            {
                StatusMessage = "Clip START/END is not set";
                return;
            }

            var s = ClipStart.Value;
            var e = ClipEnd.Value;
            if (e < s) { var tmp = s; s = e; e = tmp; }

            Clips.Add(new ClipItem
            {
                Team = t,
                Start = s,
                End = e,
                Tags = new ObservableCollection<string>(SelectedTags.ToList())
            });

            StatusMessage = $"Saved clip ({t}) {FormatTime(s)} - {FormatTime(e)}";
        }

        // ========= Tag operations =========
        public void AddTagToSelected(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;
            if (!SelectedTags.Contains(tagName))
                SelectedTags.Add(tagName);
            StatusMessage = $"Tag selected: {tagName}";
        }

        public void AddTagToSelected(TagEntry entry)
        {
            if (entry == null) return;
            AddTagToSelected(entry.Name);
        }

        public void RemoveSelectedTag(string tagName)
        {
            if (SelectedTags.Contains(tagName))
                SelectedTags.Remove(tagName);
            StatusMessage = $"Tag removed: {tagName}";
        }

        public void ClearTagsForSelected()
        {
            SelectedTags.Clear();
            StatusMessage = "Tags cleared";
        }

        // ========= CSV (後で本実装) =========
        public void ImportCsvFromDialog() => StatusMessage = "Import CSV: (todo)";
        public void ExportCsvToDialog() => StatusMessage = "Export CSV: (todo)";

        // ========= util =========
        private static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            return ts.Hours > 0
                ? $"{ts.Hours}:{ts.Minutes:00}:{ts.Seconds:00}"
                : $"{ts.Minutes:00}:{ts.Seconds:00}";
        }
    }
}
