using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        // ✅ 互換用：いまのコードが AppState.Current を参照してるので必須
        public static AppState Current => Instance;

        // ✅ こちらでも参照できるように残す
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

        public void SelectVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            SelectedVideoPath = path;
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
