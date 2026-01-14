using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        public static AppState Instance { get; } = new AppState();

        public ObservableCollection<string> ImportedVideos { get; } = new ObservableCollection<string>();

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
            => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(none)" : System.IO.Path.GetFileName(SelectedVideoPath);

        public event PropertyChangedEventHandler? PropertyChanged;

        private AppState() { }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
