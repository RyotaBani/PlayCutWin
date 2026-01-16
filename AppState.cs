using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    // App-wide shared state (Singleton)
    public sealed class AppState : INotifyPropertyChanged
    {
        // ---- Singleton ------------------------------------------------------
        private static readonly AppState _current = new AppState();
        public static AppState Current => _current;

        // 以前のコードが Instance を参照してても動くように互換エイリアス
        public static AppState Instance => _current;

        private AppState()
        {
            ImportedVideos = new ObservableCollection<VideoItem>();
            Tags = new ObservableCollection<TagEntry>();
        }

        // ---- INotifyPropertyChanged ----------------------------------------
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

        // ---- Status / Selected Video ---------------------------------------
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        private string _selectedVideoPath = "";
        public string SelectedVideoPath
        {
            get => _selectedVideoPath;
            set => Set(ref _selectedVideoPath, value);
        }

        // ClipsView 等が SetSelected を呼んでも動くように
        public void SetSelected(string? path)
        {
            SelectedVideoPath = path ?? "";
            StatusMessage = string.IsNullOrWhiteSpace(SelectedVideoPath)
                ? "Selected: (none)"
                : $"Selected: {SelectedVideoPath}";
        }

        // ---- Imported Videos (Clips list uses this) ------------------------
        public ObservableCollection<VideoItem> ImportedVideos { get; }

        public void AddImportedVideo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            // 重複防止（同じ Path があれば追加しない）
            if (ImportedVideos.Any(v => string.Equals(v.Path, filePath, StringComparison.OrdinalIgnoreCase)))
            {
                SetSelected(filePath);
                StatusMessage = $"Already imported: {filePath}";
                return;
            }

            var item = new VideoItem(filePath);
            ImportedVideos.Add(item);

            // 追加したら選択状態も更新
            SetSelected(filePath);
            StatusMessage = $"Imported: {item.Name}";
            OnPropertyChanged(nameof(ImportedVideos));
        }

        // ---- Playback info (Player UI binds these) -------------------------
        private double _playbackPosition; // seconds
        public double PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (Set(ref _playbackPosition, value))
                {
                    OnPropertyChanged(nameof(PlaybackPositionText));
                }
            }
        }

        private double _playbackDuration; // seconds
        public double PlaybackDuration
        {
            get => _playbackDuration;
            set
            {
                if (Set(ref _playbackDuration, value))
                {
                    OnPropertyChanged(nameof(PlaybackDurationText));
                }
            }
        }

        // 既存コード互換：PlaybackSeconds を参照してても Position と同義で動く
        public double PlaybackSeconds
        {
            get => PlaybackPosition;
            set => PlaybackPosition = value;
        }

        public string PlaybackPositionText => FormatTime(PlaybackPosition);
        public string PlaybackDurationText => FormatTime(PlaybackDuration);

        // ---- Tags -----------------------------------------------------------
        public ObservableCollection<TagEntry> Tags { get; }

        // 現状の TagsView は Enter / ボタンで AddTag を呼ぶ想定
        public void AddTag(string tagText)
        {
            if (string.IsNullOrWhiteSpace(tagText))
            {
                StatusMessage = "Tag is empty.";
                return;
            }

            // どの動画に対するタグか最低限持たせる（今は SelectedVideoPath だけ）
            var t = new TagEntry
            {
                Seconds = PlaybackPosition,
                Time = FormatTime(PlaybackPosition),
                Text = tagText.Trim(),
                VideoPath = SelectedVideoPath
            };

            Tags.Add(t);
            StatusMessage = $"Added tag: {t.Time} {t.Text}";
            OnPropertyChanged(nameof(Tags));
        }

        public void ClearTagsForSelected()
        {
            if (Tags.Count == 0) return;

            var sel = SelectedVideoPath ?? "";
            if (string.IsNullOrWhiteSpace(sel))
            {
                Tags.Clear();
                StatusMessage = "Cleared all tags.";
                return;
            }

            // 選択中の動画に紐づくタグだけ消す
            var toRemove = Tags.Where(x => string.Equals(x.VideoPath, sel, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var x in toRemove) Tags.Remove(x);

            StatusMessage = $"Cleared tags for selected video. ({toRemove.Count})";
            OnPropertyChanged(nameof(Tags));
        }

        // ---- Helper ---------------------------------------------------------
        public static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            // mm:ss 形式（必要なら小数も足せる）
            return $"{(int)ts.TotalMinutes:00}:{ts.Seconds:00}";
        }
    }

    // ---- Models -------------------------------------------------------------
    public sealed class VideoItem
    {
        public VideoItem(string path)
        {
            Path = path ?? "";
            Name = System.IO.Path.GetFileName(Path);
        }

        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public sealed class TagEntry
    {
        // DataGrid が Binding している列に合わせる
        public string Time { get; set; } = "";  // "mm:ss"
        public string Text { get; set; } = "";

        // 内部用（秒 / 紐づく動画）
        public double Seconds { get; set; }
        public string VideoPath { get; set; } = "";
    }
}
