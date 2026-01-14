using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // ✅ これが無いせいで Instance エラーになってた
        public static AppState Instance { get; } = new AppState();

        private AppState() { }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _selectedVideoPath;

        /// <summary>
        /// 今選択されている動画のフルパス
        /// </summary>
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

        /// <summary>
        /// 今選択されている動画のファイル名（表示用）
        /// </summary>
        public string SelectedVideoFileName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return "(none)";
                return Path.GetFileName(SelectedVideoPath);
            }
        }

        /// <summary>
        /// Importされた動画（フルパス）一覧
        /// </summary>
        public ObservableCollection<string> ImportedVideos { get; } = new ObservableCollection<string>();

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (!ImportedVideos.Contains(path))
                ImportedVideos.Add(path);

            // 追加したら自動的にそれを選択にしておく
            SelectedVideoPath = path;
        }

        public void SelectVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            SelectedVideoPath = path;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
