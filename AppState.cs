using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // 互換：旧コードが Current を参照
        public static AppState Current => Instance;

        // 現行：Instance でも参照可
        public static AppState Instance { get; } = new AppState();

        private AppState() { }

        public event PropertyChangedEventHandler? PropertyChanged;

        private string? _selectedVideoPath;

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
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return "(none)";
                return Path.GetFileName(SelectedVideoPath);
            }
        }

        // Importした動画一覧（フルパス）
        public ObservableCollection<string> ImportedVideos { get; } = new ObservableCollection<string>();

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (!ImportedVideos.Contains(path))
                ImportedVideos.Add(path);

            SelectedVideoPath = path;
        }

        // 現行API
        public void SelectVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            SelectedVideoPath = path;
        }

        // ✅ 互換API：ClipsViewが呼んでいる可能性が高い
        public void SetSelected(string path) => SelectVideo(path);

        // ✅ 互換API：将来別名が出ても困らないように（あっても害なし）
        public void SetSelectedVideo(string path) => SelectVideo(path);

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
