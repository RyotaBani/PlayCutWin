using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    /// <summary>
    /// 画面間で共有する状態（最小版）
    /// </summary>
    public sealed class AppState : INotifyPropertyChanged
    {
        public static AppState Current { get; } = new AppState();

        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _selectedVideoPath;

        /// <summary>Import された動画一覧</summary>
        public ObservableCollection<string> ImportedVideos { get; } = new();

        /// <summary>現在選択中の動画パス（画面間共有）</summary>
        public string? SelectedVideoPath
        {
            get => _selectedVideoPath;
            set
            {
                if (_selectedVideoPath == value) return;
                _selectedVideoPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoFileName));
            }
        }

        public string SelectedVideoFileName
            => string.IsNullOrWhiteSpace(SelectedVideoPath)
                ? "(none)"
                : System.IO.Path.GetFileName(SelectedVideoPath);

        private readonly Dictionary<string, ObservableCollection<string>> _tagsByVideo = new();

        /// <summary>選択中動画のタグ（Tags画面で表示する用）</summary>
        public ObservableCollection<string> CurrentTags
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedVideoPath))
                    return _emptyTags;

                if (!_tagsByVideo.TryGetValue(SelectedVideoPath!, out var tags))
                {
                    tags = new ObservableCollection<string>();
                    _tagsByVideo[SelectedVideoPath!] = tags;
                }
                return tags;
            }
        }

        private readonly ObservableCollection<string> _emptyTags = new();

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (!ImportedVideos.Contains(path))
                ImportedVideos.Add(path);

            SelectedVideoPath = path;
            OnPropertyChanged(nameof(CurrentTags));
        }

        public void SetSelected(string? path)
        {
            SelectedVideoPath = path;
            OnPropertyChanged(nameof(CurrentTags));
        }

        public void AddTagToSelected(string tag)
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;
            tag = tag.Trim();
            if (tag.Length == 0) return;

            var tags = CurrentTags;
            if (!tags.Contains(tag))
                tags.Add(tag);

            OnPropertyChanged(nameof(CurrentTags));
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
