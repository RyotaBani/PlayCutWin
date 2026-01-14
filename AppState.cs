using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // 互換のため Instance/Current 両方用意
        public static AppState Instance { get; } = new AppState();
        public static AppState Current => Instance;

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
                StatusMessage = _selectedVideo == null
                    ? "Ready"
                    : $"Selected: {_selectedVideo.Name}";
            }
        }

        public string SelectedPath => SelectedVideo?.Path ?? "";

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

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
}
