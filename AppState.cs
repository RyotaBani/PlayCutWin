using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public class ImportedVideo
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public class TagEntry
    {
        public string Time { get; set; } = "00:00";   // mm:ss
        public string Text { get; set; } = "";
        public string VideoPath { get; set; } = "";
    }

    public class AppState : INotifyPropertyChanged
    {
        // Singleton
        private static readonly AppState _instance = new AppState();
        public static AppState Instance => _instance;

        // 互換用（以前のコードが AppState.Current / AppState.Instance 両方参照しても落ちないように）
        public static AppState Current => _instance;

        private AppState()
        {
            ImportedVideos = new ObservableCollection<ImportedVideo>();
            Tags = new ObservableCollection<TagEntry>();
        }

        public ObservableCollection<ImportedVideo> ImportedVideos { get; }
        public ObservableCollection<TagEntry> Tags { get; }

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
                if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return "(none)";
                return System.IO.Path.GetFileName(SelectedVideoPath);
            }
        }

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

        public string PlaybackPositionText => FormatTime(PlaybackPosition);

        public void SetSelected(string? videoPath)
        {
            SelectedVideoPath = videoPath;
        }

        public void AddTag(string tagText)
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;

            Tags.Add(new TagEntry
            {
                Time = PlaybackPositionText,
                Text = tagText.Trim(),
                VideoPath = SelectedVideoPath!
            });
        }

        public static string FormatTime(TimeSpan ts)
        {
            var totalSeconds = (int)Math.Max(0, ts.TotalSeconds);
            var mm = totalSeconds / 60;
            var ss = totalSeconds % 60;
            return $"{mm:00}:{ss:00}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
