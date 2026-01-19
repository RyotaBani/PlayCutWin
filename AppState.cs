using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    /// <summary>
    /// App 全体の状態（Viewが参照する） + Playerへ命令を投げる（イベント）
    /// いまは「View側が期待している古いAPI名」を全部揃えてビルドを安定させる。
    /// </summary>
    public sealed class AppState : INotifyPropertyChanged
    {
        // ===== TagsView が期待している：AppState.TagEntry =====
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

            // 初期値（Mac版に寄せるならここを後で整える）
            TeamAName = "Home / Our Team";
            TeamBName = "Away / Opponent";
            StatusMessage = "Ready";
            PlaybackSpeed = 1.0;
        }

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ===== State =====
        private string _teamAName = "";
        public string TeamAName
        {
            get => _teamAName;
            set { _teamAName = value; OnPropertyChanged(); }
        }

        private string _teamBName = "";
        public string TeamBName
        {
            get => _teamBName;
            set { _teamBName = value; OnPropertyChanged(); }
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
            }
        }

        public bool HasVideo => !string.IsNullOrWhiteSpace(VideoPath) && File.Exists(VideoPath);

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

        // ▶ / ⏸ の見え分け（PlayerControlsボタンのContentに使える）
        public string PlayPauseGlyph => IsPlaying ? "⏸" : "▶";

        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set { _playbackSeconds = value; OnPropertyChanged(); }
        }

        private double _durationSeconds;
        public double DurationSeconds
        {
            get => _durationSeconds;
            set { _durationSeconds = value; OnPropertyChanged(); }
        }

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
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ===== Tags =====
        public ObservableCollection<TagEntry> Tags { get; }
        public ObservableCollection<string> SelectedTags { get; }

        // ===== Playerへ命令を投げるイベント（PlayerView側が購読して実処理する） =====
        public event EventHandler? RequestPlayPause;
        public event EventHandler? RequestStop;
        public event EventHandler<double>? RequestSeekRelative; // +秒 / -秒
        public event EventHandler<double>? RequestRate;         // 速度

        // ============================================================
        // 互換API（View側が期待している名前を「そのまま」用意する）
        // ============================================================

        // DashboardView が呼ぶ想定：動画登録
        public void AddImportedVideo(string path)
        {
            VideoPath = path ?? "";
            PlaybackSeconds = 0;
            DurationSeconds = 0;
            IsPlaying = false;
            StatusMessage = HasVideo ? "Media opened" : "No video loaded";
        }

        // PlayerControlsView が呼んでる想定（スクショのエラー群）
        public void SendPlayPause()
        {
            RequestPlayPause?.Invoke(this, EventArgs.Empty);
        }

        public void SendStop()
        {
            RequestStop?.Invoke(this, EventArgs.Empty);
        }

        public void SendSeekRelative(double deltaSeconds)
        {
            RequestSeekRelative?.Invoke(this, deltaSeconds);
        }

        public void SendRate(double rate)
        {
            PlaybackSpeed = rate;
            RequestRate?.Invoke(this, rate);
        }

        // TagsView が呼ぶ想定
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

        // ExportsView が呼ぶ想定（いまは“ビルド安定”優先でプレースホルダ）
        public void ImportCsvFromDialog()
        {
            StatusMessage = "Import CSV: (todo)";
        }

        public void ExportCsvToDialog()
        {
            StatusMessage = "Export CSV: (todo)";
        }
    }
}
