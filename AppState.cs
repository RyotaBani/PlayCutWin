using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    // 1本の動画行（Clips/Exportsで使い回す）
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public class AppState : INotifyPropertyChanged
    {
        // -------- Singleton (互換: Instance / Current どっちでも取れる) --------
        private static readonly AppState _instance = new AppState();
        public static AppState Instance => _instance;
        public static AppState Current => _instance;

        // -------- 共通状態 --------
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new ObservableCollection<VideoItem>();

        private string _selectedVideoPath = "";
        public string SelectedVideoPath
        {
            get => _selectedVideoPath;
            set
            {
                if (_selectedVideoPath == value) return;
                _selectedVideoPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoName));
                OnPropertyChanged(nameof(SelectedVideoDisplay));
                SyncPendingTagsFromSelected();
            }
        }

        public string SelectedVideoName
        {
            get
            {
                var item = ImportedVideos.FirstOrDefault(v => v.Path == SelectedVideoPath);
                return item?.Name ?? "";
            }
        }

        public string SelectedVideoDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return "(none)";
                return $"{SelectedVideoName}";
            }
        }

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

        // -------- Tags（選択中動画ごとに保持） --------
        // 画面が直接バインドしやすい「表示用の現行タグ」
        public ObservableCollection<string> Tags { get; } = new ObservableCollection<string>();

        // 動画Path -> Tags の保存領域
        private readonly Dictionary<string, List<string>> _tagsByVideoPath = new Dictionary<string, List<string>>();

        // 互換用: 旧コードが SetSelected を呼んでも動くように
        public void SetSelected(string? path)
        {
            SelectedVideoPath = path ?? "";
            StatusMessage = string.IsNullOrWhiteSpace(SelectedVideoPath)
                ? "Selected cleared"
                : $"Selected: {SelectedVideoName}";
        }

        // 互換用: 旧コードが SetSelected(VideoItem) を呼んでも動くように
        public void SetSelected(VideoItem? item)
        {
            SetSelected(item?.Path);
        }

        // 互換用: 旧コードが AddImportedVideo を呼んでも動くように
        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            var name = System.IO.Path.GetFileName(fullPath);
            if (ImportedVideos.Any(v => v.Path == fullPath)) return;

            ImportedVideos.Add(new VideoItem { Name = name, Path = fullPath });
            StatusMessage = $"Imported: {name}";

            // 初回は自動選択
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
                SetSelected(fullPath);
        }

        // 互換用: 旧コードが AddTag を呼んでも動くように
        public void AddTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            tag = tag.Trim();

            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No video selected";
                return;
            }

            var list = GetOrCreateTagList(SelectedVideoPath);
            if (!list.Contains(tag))
                list.Add(tag);

            SyncPendingTagsFromSelected();
            StatusMessage = $"Tag added: {tag}";
        }

        public void RemoveTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;

            var list = GetOrCreateTagList(SelectedVideoPath);
            list.Remove(tag);

            SyncPendingTagsFromSelected();
            StatusMessage = $"Tag removed: {tag}";
        }

        public void ClearTags()
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;
            _tagsByVideoPath[SelectedVideoPath] = new List<string>();
            SyncPendingTagsFromSelected();
            StatusMessage = "Tags cleared";
        }

        public IReadOnlyList<string> GetTagsForSelected()
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return Array.Empty<string>();
            return GetOrCreateTagList(SelectedVideoPath);
        }

        // -------- internal helpers --------
        private List<string> GetOrCreateTagList(string path)
        {
            if (!_tagsByVideoPath.TryGetValue(path, out var list))
            {
                list = new List<string>();
                _tagsByVideoPath[path] = list;
            }
            return list;
        }

        private void SyncPendingTagsFromSelected()
        {
            Tags.Clear();
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return;

            var list = GetOrCreateTagList(SelectedVideoPath);
            foreach (var t in list)
                Tags.Add(t);
        }

        // -------- INotifyPropertyChanged --------
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
