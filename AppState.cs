using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    // いまの段階は「共有状態のハブ」
    // Clips / Tags / Exports から共通で参照する。
    public sealed class AppState : INotifyPropertyChanged
    {
        public static AppState Current { get; } = new AppState();

        private AppState()
        {
            ImportedVideos = new ObservableCollection<VideoItem>();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(name);
        }

        // -----------------------------
        // 共通：ステータス/選択動画
        // -----------------------------
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

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
                OnPropertyChanged(nameof(SelectedVideoText));
                OnPropertyChanged(nameof(TagsForSelected));
            }
        }

        public string SelectedVideoName
            => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(none)" : Path.GetFileName(SelectedVideoPath);

        public string SelectedVideoText
            => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(no video selected)" : SelectedVideoPath!;

        // Clips/Tags/Exports で共通の「選択変更」用
        public void SetSelected(string? path)
        {
            SelectedVideoPath = path;
            if (!string.IsNullOrWhiteSpace(path))
                StatusMessage = $"Selected: {Path.GetFileName(path)}";
        }

        // -----------------------------
        // Clips：読み込んだ動画一覧
        // -----------------------------
        public ObservableCollection<VideoItem> ImportedVideos { get; }

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            // 既にあるなら先頭へ(見つけやすく)
            var existing = ImportedVideos.FirstOrDefault(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                ImportedVideos.Remove(existing);
                ImportedVideos.Insert(0, existing);
                SetSelected(existing.Path);
                return;
            }

            var item = new VideoItem
            {
                Name = Path.GetFileName(path),
                Path = path
            };

            ImportedVideos.Insert(0, item);
            SetSelected(path);
        }

        // -----------------------------
        // Tags：動画ごとのタグ一覧（Aブロック本体）
        // -----------------------------
        private readonly Dictionary<string, ObservableCollection<TagEntry>> _tagsByVideo = new();

        // XAMLから ItemsSource で直接バインドする用（SelectedVideoPathに追従）
        public ObservableCollection<TagEntry> TagsForSelected
        {
            get
            {
                var key = SelectedVideoPath ?? "";
                if (string.IsNullOrWhiteSpace(key))
                    return new ObservableCollection<TagEntry>();

                if (!_tagsByVideo.TryGetValue(key, out var list))
                {
                    list = new ObservableCollection<TagEntry>();
                    _tagsByVideo[key] = list;
                }
                return list;
            }
        }

        public void AddTagForSelected(string tagText)
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No selected video.";
                return;
            }

            tagText = (tagText ?? "").Trim();
            if (tagText.Length == 0) return;

            var list = TagsForSelected; // ここで辞書に作られる
            list.Add(new TagEntry { Text = tagText, CreatedAt = DateTime.Now });

            StatusMessage = $"Tag added: {tagText}";
            OnPropertyChanged(nameof(TagsForSelected));
        }

        public void ClearTagsForSelected()
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath))
            {
                StatusMessage = "No selected video.";
                return;
            }

            var list = TagsForSelected;
            list.Clear();

            StatusMessage = "Tags cleared.";
            OnPropertyChanged(nameof(TagsForSelected));
        }

        // もし他Viewで使いたい時のため（安全）
        public IReadOnlyList<TagEntry> GetTags(string? videoPath)
        {
            if (string.IsNullOrWhiteSpace(videoPath)) return Array.Empty<TagEntry>();
            if (_tagsByVideo.TryGetValue(videoPath, out var list)) return list.ToList();
            return Array.Empty<TagEntry>();
        }
    }

    // -----------------------------
    // 最小モデル（なければここで定義）
    // -----------------------------
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    public class TagEntry
    {
        public string Text { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string TimeText => CreatedAt.ToString("HH:mm:ss");
    }
}
