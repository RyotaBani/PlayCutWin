using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    // Imported video row model
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    // Simple tag model (A block)
    public class TagItem
    {
        public string Text { get; set; } = "";
        public string Time { get; set; } = ""; // placeholder (e.g. "00:12")
    }

    // App-wide shared state
    public class AppState : INotifyPropertyChanged
    {
        private static readonly AppState _instance = new AppState();
        public static AppState Instance => _instance;

        // Compatibility aliases (if old code references these)
        public static AppState Current => _instance;

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string _selectedVideoPath = "";
        public string SelectedVideoPath
        {
            get => _selectedVideoPath;
            set { _selectedVideoPath = value; OnPropertyChanged(); OnPropertyChanged(nameof(SelectedVideoName)); }
        }

        public string SelectedVideoName
            => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(none)" : System.IO.Path.GetFileName(SelectedVideoPath);

        public ObservableCollection<VideoItem> ImportedVideos { get; } = new ObservableCollection<VideoItem>();
        public ObservableCollection<TagItem> Tags { get; } = new ObservableCollection<TagItem>();

        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            ImportedVideos.Add(new VideoItem
            {
                Name = System.IO.Path.GetFileName(fullPath),
                Path = fullPath
            });

            StatusMessage = $"Imported: {System.IO.Path.GetFileName(fullPath)}";
        }

        public void SetSelected(string fullPath)
        {
            SelectedVideoPath = fullPath ?? "";
            StatusMessage = string.IsNullOrWhiteSpace(SelectedVideoPath)
                ? "Selected: (none)"
                : $"Selected: {System.IO.Path.GetFileName(SelectedVideoPath)}";
        }

        public void AddTag(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            // A block: time is placeholder (later connect to playback time)
            Tags.Add(new TagItem { Text = text.Trim(), Time = "--:--" });
            StatusMessage = $"Tag added: {text.Trim()}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
