using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace PlayCutWin
{
    // 取り回ししやすい最小モデル（ClipsView側でも使える）
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public sealed class AppState : INotifyPropertyChanged
    {
        // ✅ 互換: AppState.Instance / AppState.Current 両方を使えるようにする
        public static AppState Instance { get; } = new AppState();
        public static AppState Current => Instance;

        private AppState()
        {
            ImportedVideos = new ObservableCollection<VideoItem>();
            ImportedVideos.CollectionChanged += (_, __) => RaiseImportedSummary();

            // 初期状態（選択なし）
            _tagsForSelectedVideo = new ObservableCollection<string>();
            HookTagsCollection(_tagsForSelectedVideo);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        // ----------------------------
        // Imported videos
        // ----------------------------
        public ObservableCollection<VideoItem> ImportedVideos { get; }

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                if (_selectedVideo == value) return;
                _selectedVideo = value;
                OnPropertyChanged(nameof(SelectedVideo));
                OnPropertyChanged(nameof(SelectedVideoPath));
                OnPropertyChanged(nameof(SelectedVideoName));

                // 選択が変わったら、その動画のタグ一覧へ切り替える
                SwitchTagsCollectionForSelectedVideo();

                StatusMessage = _selectedVideo == null
                    ? "Ready"
                    : $"Selected: {SelectedVideoName}";
            }
        }

        public string SelectedVideoPath => SelectedVideo?.Path ?? "";
        public string SelectedVideoName => SelectedVideo?.Name ?? "";

        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            // 同一パスは重複登録しない（必要ならここを変更）
            if (ImportedVideos.Any(v => string.Equals(v.Path, fullPath, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = $"Already imported: {System.IO.Path.GetFileName(fullPath)}";
                return;
            }

            var item = new VideoItem
            {
                Path = fullPath,
                Name = System.IO.Path.GetFileName(fullPath)
            };

            ImportedVideos.Add(item);

            // 追加した動画を選択
            SelectedVideo = item;

            StatusMessage = $"Imported: {item.Name}";
        }

        // ----------------------------
        // Status
        // ----------------------------
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        private void RaiseImportedSummary()
        {
            OnPropertyChanged(nameof(ImportedCountText));
        }

        public string ImportedCountText => $"Count: {ImportedVideos.Count}";

        // ----------------------------
        // Tags (per-video)
        // ----------------------------
        // Pathごとにタグ一覧を持つ
        private readonly Dictionary<string, ObservableCollection<string>> _tagsByVideoPath
            = new Dictionary<string, ObservableCollection<string>>(StringComparer.OrdinalIgnoreCase);

        private ObservableCollection<string> _tagsForSelectedVideo;

        /// <summary>
        /// TagsView / Dashboard / Exports が参照する「選択中動画のタグ一覧」
        /// </summary>
        public ObservableCollection<string> TagsForSelectedVideo
        {
            get => _tagsForSelectedVideo;
            private set
            {
                if (ReferenceEquals(_tagsForSelectedVideo, value)) return;

                UnhookTagsCollection(_tagsForSelectedVideo);
                _tagsForSelectedVideo = value;
                HookTagsCollection(_tagsForSelectedVideo);

                OnPropertyChanged(nameof(TagsForSelectedVideo));
                RaiseTagSummary();
            }
        }

        public string TagCountText => SelectedVideo == null
            ? "Tags: 0"
            : $"Tags: {TagsForSelectedVideo.Count}";

        public string LatestTagText => (TagsForSelectedVideo.Count == 0)
            ? "Latest: -"
            : $"Latest: {TagsForSelectedVideo.Last()}";

        public void AddTag(string tag)
        {
            if (SelectedVideo == null)
            {
                StatusMessage = "Select a video first.";
                return;
            }

            tag = (tag ?? "").Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                StatusMessage = "Tag is empty.";
                return;
            }

            // 同一タグは重複させない（必要ならここを変更）
            if (TagsForSelectedVideo.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)))
            {
                StatusMessage = $"Tag already exists: {tag}";
                return;
            }

            TagsForSelectedVideo.Add(tag);
            StatusMessage = $"Added tag: {tag}";
        }

        public void RemoveTag(string tag)
        {
            if (SelectedVideo == null) return;
            if (string.IsNullOrWhiteSpace(tag)) return;

            var hit = TagsForSelectedVideo.FirstOrDefault(t =>
                string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));

            if (hit != null)
            {
                TagsForSelectedVideo.Remove(hit);
                StatusMessage = $"Removed tag: {hit}";
            }
        }

        private void SwitchTagsCollectionForSelectedVideo()
        {
            if (SelectedVideo == null || string.IsNullOrWhiteSpace(SelectedVideo.Path))
            {
                TagsForSelectedVideo = new ObservableCollection<string>();
                return;
            }

            if (!_tagsByVideoPath.TryGetValue(SelectedVideo.Path, out var list))
            {
                list = new ObservableCollection<string>();
                _tagsByVideoPath[SelectedVideo.Path] = list;
            }

            TagsForSelectedVideo = list;
        }

        private void HookTagsCollection(ObservableCollection<string> col)
        {
            col.CollectionChanged += Tags_CollectionChanged;
        }

        private void UnhookTagsCollection(ObservableCollection<string> col)
        {
            col.CollectionChanged -= Tags_CollectionChanged;
        }

        private void Tags_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RaiseTagSummary();
        }

        private void RaiseTagSummary()
        {
            OnPropertyChanged(nameof(TagCountText));
            OnPropertyChanged(nameof(LatestTagText));
        }

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
