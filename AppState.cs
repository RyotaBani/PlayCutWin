using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    // TagsView が参照している型
    public sealed class TagEntry
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "Offense"; // "Offense" / "Defense" など
        public override string ToString() => Name;
    }

    // App 全体の状態 + Views から呼ばれる「互換API」を全部持たせる
    public sealed class AppState : INotifyPropertyChanged
    {
        // ===== Singleton =====
        public static AppState Instance { get; } = new AppState();
        private AppState()
        {
            // 初期タグ（空でもOK。UIが死なないように最低限用意）
            Tags = new ObservableCollection<TagEntry>();
            SelectedTags = new ObservableCollection<string>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ===== 基本プロパティ（UI表示のため） =====
        private string _teamAName = "Home / Our Team";
        public string TeamAName
        {
            get => _teamAName;
            set { _teamAName = value; OnPropertyChanged(); }
        }

        private string _teamBName = "Away / Opponent";
        public string TeamBName
        {
            get => _teamBName;
            set { _teamBName = value; OnPropertyChanged(); }
        }

        private string _videoPath = "";
        public string VideoPath
        {
            get => _videoPath;
            set { _videoPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasVideo)); }
        }

        public bool HasVideo => !string.IsNullOrWhiteSpace(VideoPath) && File.Exists(VideoPath);

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayPauseGlyph)); }
        }

        // UIで使う ▶ / ⏸ 切り替え（ボタンの Content に使える）
        public string PlayPauseGlyph => IsPlaying ? "⏸" : "▶";

        // 再生位置（秒）
        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set { _playbackSeconds = value; OnPropertyChanged(); }
        }

        // 総尺（秒）— MediaOpened などで更新
        private double _durationSeconds;
        public double DurationSeconds
        {
            get => _durationSeconds;
            set { _durationSeconds = value; OnPropertyChanged(); }
        }

        // 再生速度
        private double _playbackSpeed = 1.0;
        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set { _playbackSpeed = value; OnPropertyChanged(); }
        }

        // ステータス表示
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // ===== タグ（Views/TagsView が参照している） =====
        public ObservableCollection<TagEntry> Tags { get; private set; }

        // いま選択されてるタグ（簡易：文字列で保持）
        public ObservableCollection<string> SelectedTags { get; private set; }

        // ===== “命令イベント”（Player側が購読して実処理する） =====
        public event EventHandler? RequestPlayPause;
        public event EventHandler? RequestStop;
        public event EventHandler<double>? RequestSeek;      // 秒を加算（+なら進む、-なら戻る）
        public event EventHandler<double>? RequestRate;      // 速度

        // ===== Views が呼ぶ互換API（ここが無いと今のエラーになる） =====

        // DashboardView から呼ばれてる：動画を登録
        public void AddImportedVideo(string path)
        {
            VideoPath = path ?? "";
            PlaybackSeconds = 0;
            DurationSeconds = 0;
            IsPlaying = false;
            StatusMessage = HasVideo ? "Media opened" : "No video loaded";

            // PlayerView 側が VideoPath 変更を見て Source を差し替える想定
        }

        // Controls：▶/⏸
        public void TogglePlayPause()
        {
            RequestPlayPause?.Invoke(this, EventArgs.Empty);
        }

        public void StopPlayback()
        {
            RequestStop?.Invoke(this, EventArgs.Empty);
        }

        public void SeekBySeconds(double deltaSeconds)
        {
            RequestSeek?.Invoke(this, deltaSeconds);
        }

        public void SetPlaybackSpeed(double rate)
        {
            PlaybackSpeed = rate;
            RequestRate?.Invoke(this, rate);
        }

        // TagsView から呼ばれてる：選択タグの追加/削除/クリア
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

        // ExportsView から呼ばれてる：CSV入出力（今は“最低限ビルド優先”で空実装）
        // 後で Clips 実装が固まったら本実装にする
        public void ImportCsvFromDialog()
        {
            // TODO: 後で実装（OpenFileDialog → クリップ読み込み）
            StatusMessage = "Import CSV: (todo)";
        }

        public void ExportCsvToDialog()
        {
            // TODO: 後で実装（SaveFileDialog → クリップ書き出し）
            StatusMessage = "Export CSV: (todo)";
        }
    }
}
