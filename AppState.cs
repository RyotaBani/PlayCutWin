using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public sealed class AppState : INotifyPropertyChanged
    {
        // Singleton
        private static readonly AppState _instance = new AppState();

        // どっちで呼ばれても動くように alias を用意
        public static AppState Instance => _instance;
        public static AppState Current => _instance;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<VideoItem> ImportedVideos { get; } = new ObservableCollection<VideoItem>();

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            private set
            {
                _selectedVideo = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoName));
                OnPropertyChanged(nameof(SelectedVideoPath));
            }
        }

        public string SelectedVideoName => SelectedVideo?.Name ?? "(none)";
        public string SelectedVideoPath => SelectedVideo?.Path ?? "";

        private AppState() { }

        public void AddImportedVideo(string filePath)
        {
            var item = new VideoItem
            {
                Name = System.IO.Path.GetFileName(filePath),
                Path = filePath
            };

            ImportedVideos.Add(item);

            // 追加したらそれを選択にする（使いやすい）
            SetSelected(item);
            OnPropertyChanged(nameof(ImportedCount));
        }

        public int ImportedCount => ImportedVideos.Count;

        public void SetSelected(VideoItem? item)
        {
            SelectedVideo = item;
        }

        public void SetSelectedByPath(string filePath)
        {
            foreach (var v in ImportedVideos)
            {
                if (string.Equals(v.Path, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    SetSelected(v);
                    return;
                }
            }
        }

        public void ClearSelection()
        {
            SelectedVideo = null;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
