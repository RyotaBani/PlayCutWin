using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // 互換のため Instance/Current 両方用意
        public static AppState Instance { get; } = new AppState();
        public static AppState Current => Instance;

        // ---- Imported videos ----
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
                OnPropertyChanged(nameof(SelectedPath));
                OnPropertyChanged(nameof(SelectedVideoPath));

                StatusMessage = _selectedVideo == null
                    ? "Ready"
                    : $"Selected: {_selectedVideo.Name}";
            }
        }

        // 既存互換
        public string SelectedPath => SelectedVideo?.Path ?? "";

        // ★ TagsView.xaml.cs が要求している名前（互換）
        public string SelectedVideoPath => SelectedVideo?.Path ?? "";

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

        // ---- Playback position (placeholder) ----
        private TimeSpan _playbackPosition = TimeSpan.Zero;
        public TimeSpan PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (_playbackPosition == value) return;
                _playbackPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        // ★ TagsView.xaml.cs が要求している名前（互換）
        public string PlaybackPositionText => FormatTime(PlaybackPosition);

        // ---- Tags (placeholder) ----
        // ★ TagsView.xaml.cs が要求している名前（互換）
        public ObservableCollection<TagItem> Tags { get; } = new();

        private AppState() { }

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            var item = new VideoItem(path);
            ImportedVideos.Add(item);

            // 追加したら自動選択
            SelectedVideo = item;

            StatusMessage = $"Imported: {item.Name}";
        }

        // 互換用（既存コードが呼んでても落ちないように）
        public void SetSelected(VideoItem? item) => SelectedVideo = item;

        // ★ TagsView.xaml.cs が要求しているメソッド名（互換）
        public void AddTag(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0)
            {
                StatusMessage = "タグを入れてね（仮）";
                return;
            }

            // とりあえず同名重複は防ぐ（仮）
            if (Tags.Any(t => string.Equals(t.Text, text, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = $"Tag already exists: {text}";
                return;
            }

            Tags.Add(new TagItem
            {
                Text = text,
                Time = PlaybackPositionText
            });

            StatusMessage = $"Tag added: {text} @ {PlaybackPositionText}";
        }

        public void ClearTags()
        {
            Tags.Clear();
            StatusMessage = "Tags cleared";
        }

        // ---- INotifyPropertyChanged ----
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private static string FormatTime(TimeSpan t)
        {
            if (t.TotalHours >= 1) return t.ToString(@"hh\:mm\:ss");
            return t.ToString(@"mm\:ss");
        }
    }

    public sealed class VideoItem
    {
        public string Name { get; }
        public string Path { get; }

        public VideoItem(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }
    }

    public sealed class TagItem
    {
        public string Text { get; set; } = "";
        public string Time { get; set; } = "00:00";
    }
}
