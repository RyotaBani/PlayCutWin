using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // ---- Singleton (両方の名前でアクセス可能にする) ----
        private static readonly Lazy<AppState> _instance = new(() => new AppState());

        public static AppState Instance => _instance.Value;
        public static AppState Current => _instance.Value;

        private AppState() { }

        // ---- Data ----
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new();

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            private set
            {
                if (!Equals(_selectedVideo, value))
                {
                    _selectedVideo = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(SelectedVideoDisplay));
                }
            }
        }

        public string SelectedVideoDisplay =>
            SelectedVideo == null ? "(none)" : $"{SelectedVideo.Name}";

        // ---- Operations ----
        public void AddImportedVideo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            var item = new VideoItem(filePath);
            Application.Current.Dispatcher.Invoke(() =>
            {
                ImportedVideos.Add(item);
            });

            SetSelected(item);
        }

        // 互換用：どっちの名前で呼んでもOK
        public void SetSelected(VideoItem? item) => SelectedVideo = item;
        public void SetSelectedVideo(VideoItem? item) => SelectedVideo = item;

        // ---- INotifyPropertyChanged ----
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public sealed class VideoItem
    {
        public string Path { get; }
        public string Name { get; }

        public VideoItem(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }
    }
}
