using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // ✅ singleton（どっちで呼んでもOKにしておく）
        public static AppState Current { get; } = new AppState();
        public static AppState Instance => Current;

        private AppState() { }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

        // ---- Imported videos ----
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new ObservableCollection<VideoItem>();

        // ---- Selected video ----
        private string? _selectedVideoPath;
        public string? SelectedVideoPath
        {
            get => _selectedVideoPath;
            set
            {
                if (_selectedVideoPath == value) return;
                _selectedVideoPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoName));
            }
        }

        public string SelectedVideoName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return "";
                return Path.GetFileName(SelectedVideoPath);
            }
        }

        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            var item = new VideoItem
            {
                Name = Path.GetFileName(fullPath),
                Path = fullPath
            };

            ImportedVideos.Add(item);

            // 追加したらそれを選択にする（UXよし）
            SetSelected(fullPath);

            StatusMessage = $"Imported: {item.Name}";
        }

        public void SetSelected(string? fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;
            SelectedVideoPath = fullPath;
            StatusMessage = $"Selected: {Path.GetFileName(fullPath)}";
        }
    }

    public sealed class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }
}
