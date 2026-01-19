using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    /// <summary>
    /// Viewが参照する「アプリ状態」 + Playerへ命令を投げる「イベント」 + 互換メソッド群
    /// まずは「ビルドを通す」ことを最優先に、Viewが期待する名前を全部ここに揃える。
    /// </summary>
    public sealed class AppState : INotifyPropertyChanged
    {
        // ===== TagsViewが参照している想定：AppState.TagEntry =====
        public sealed class TagEntry
        {
            public string Name { get; set; } = "";
            public string Category { get; set; } = "Offense";
            public override string ToString() => Name;
        }

        // ===== Singleton =====
        public static AppState Instance { get; } = new AppState();

        private AppState()
        {
            Tags = new ObservableCollection<TagEntry>();
            SelectedTags = new ObservableCollection<string>();

            TeamAName = "Home / Our Team";
            TeamBName = "Away / Opponent";

            StatusMessage = "Ready";
            PlaybackSpeed = 1.0;
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ===== Core State =====
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

        private string _videoPath = "";
        public string VideoPath
        {
            get => _videoPath;
            set
            {
                _videoPath = value ?? "";
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasVideo));
                OnPropertyChanged(nameof(VideoHeaderText));
            }
        }

        public bool HasVideo => !string.IsNullOrWhiteSpace(VideoPath) && File.Exists(VideoPath);

        // Mac版にある「Video (16:9)」表記をWindows側も揃える用（無くても動くがUI合わせに便利）
        public string VideoHeaderText => "Video (16:9)";

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayPauseGlyph)); }
        }

        // ▶ / ⏸
        public string PlayPauseGlyph => IsPlaying ? "⏸" : "▶";

        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set { _playbackSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlaybackTimeText)); }
        }

        private double _durationSeconds;
        public double DurationSeconds
        {
            get => _durationSeconds;
            set { _durationSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(DurationTimeText)); }
        }

        public string PlaybackTimeText => FormatTime(PlaybackSeconds);
        public string DurationTimeText => FormatTime(DurationSeconds);

        private double _playbackSpeed = 1.0;
        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set { _playbackSpeed = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value ?? ""; OnPropertyChanged(); }
        }

        // ===== Clip Range (DashboardViewが参照しているやつ) =====
        private double? _clipStart;
        public double? ClipStart
        {
            get => _clipStart;
            set { _clipStart = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClipStartText)); }
        }

        private double? _clipEnd;
        public double? ClipEnd
        {
            get => _clipEnd;
            set { _clipEnd = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClipEndText)); }
        }

        public string ClipStartText => ClipStart.HasValue ? FormatTime(ClipStart.Value) : "--:--";
        public string ClipEndText => ClipEnd.HasValue ? FormatTime(ClipEnd.Value) : "--:--";

        // DashboardViewが期待：PlaybackDuration（TimeSpan or double）
        // ここでは double秒を返す（表示用は DurationSeconds/DurationTimeText を使う）
        public double PlaybackDuration => DurationSeconds;

        // ===== Tags =====
        public ObservableCollection<TagEntry> Tags { get; }
        public ObservableCollection<string> SelectedTags { get; }

        // ===== Clips（まずは“保存されたことにする”プレースホルダ） =====
        public sealed class ClipItem
        {
            public string Team { get; set; } = "A";     // "A" or "B"
            public double Start { get; set; }
            public double End { get; set; }
            public ObservableCollection<string> Tags { get; set; } = new ObservableCollection<string>();
            public string Note { get; set; } = "";
        }

        public ObservableCollection<ClipItem> Clips { get; } = new ObservableCollection<ClipItem>();

        // ===== Playerへ命令を投げるイベント（PlayerView側が購読して実処理） =====
        public event EventHandler? RequestPlayPause;
        public event EventHandler? RequestStop;
        public event EventHandler<double>? RequestSeekRelative;
        public event EventHandler<double>? RequestRate;

        // ============================================================
        // 互換メソッド群：Views/*.xaml.cs が呼んでる名前を全部ここで受ける
        // ============================================================

        // DashboardView：動画を読み込んだら呼ぶ想定
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

        // PlayerControlsView：再生/停止/シーク/速度
        public void SendPlayPause() => RequestPlayPause?.Invoke(this, EventArgs.Empty);
        public void SendStop() => RequestStop?.Invoke(this, EventArgs.Empty);
        public void SendSeekRelative(double deltaSeconds) => RequestSeekRelative?.Invoke(this, deltaSeconds);

        public void SendRate(double rate)
        {
            PlaybackSpeed = rate;
            RequestRate?.Invoke(this, rate);
        }

        // DashboardView：Clip START / END（今の再生位置でセット）
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

        // DashboardView：SaveClip（まずはクリップ一覧に追加するだけ）
        public void SaveClip(string team)
        {
            // teamが空ならAに寄せる
            var t = string.IsNullOrWhiteSpace(team) ? "A" : team.Trim().ToUpperInvariant();
            if (t != "A" && t != "B") t = "A";

            if (!ClipStart.HasValue || !ClipEnd.HasValue)
            {
                StatusMessage = "Clip START/END is not set";
                return;
            }

            var s = ClipStart.Value;
            var e = ClipEnd.Value;
            if (e < s)
            {
                // 入れ替え
                var tmp = s; s = e; e = tmp;
            }

            var clip = new ClipItem
            {
                Team = t,
                Start = s,
                End = e,
                Tags = new ObservableCollection<string>(SelectedTags.ToList())
            };

            Clips.Add(clip);
            StatusMessage = $"Saved clip ({t}) {FormatTime(s)} - {FormatTime(e)}";
        }

        // TagsView：タグを選択に追加（ここが “TagEntry→string” 変換エラーの解決ポイント）
        // TagsViewが TagEntry を渡してくるならこのオーバーロードで受ける。
        public void AddTagToSelected(TagEntry entry)
        {
            if (entry == null) return;
            AddTagToSelected(entry.Name);
        }

        public void AddTagToSelected(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;
            if (!SelectedTags.Contains(tagName))
                SelectedTags.Add(tagName);
            StatusMessage = $"Tag selected: {tagName}";
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

        // CSV（まずはビルド用のダミー。後で本実装）
        public void ImportCsvFromDialog() => StatusMessage = "Import CSV: (todo)";
        public void ExportCsvToDialog() => StatusMessage = "Export CSV: (todo)";

        // ============================================================
        // Helpers
        // ============================================================
        private static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            // 00:00 形式（時間が長い場合は 1:02:03 にしたければ後で拡張）
            return ts.Hours > 0
                ? $"{ts.Hours}:{ts.Minutes:00}:{ts.Seconds:00}"
                : $"{ts.Minutes:00}:{ts.Seconds:00}";
        }
    }
}
