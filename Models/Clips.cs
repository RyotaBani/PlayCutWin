using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin.Models
{
    public sealed class Clip : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        Guid _id = Guid.NewGuid();
        public Guid Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        string _team = "A"; // "A" or "B"
        public string Team
        {
            get => _team;
            set { _team = value; OnPropertyChanged(); }
        }

        double _startSeconds;
        public double StartSeconds
        {
            get => _startSeconds;
            set { _startSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(StartDisplay)); }
        }

        double _endSeconds;
        public double EndSeconds
        {
            get => _endSeconds;
            set { _endSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(EndDisplay)); }
        }

        string _tags = "";
        public string Tags
        {
            get => _tags;
            set { _tags = value; OnPropertyChanged(); }
        }

        string _clipNote = "";
        public string ClipNote
        {
            get => _clipNote;
            set { _clipNote = value; OnPropertyChanged(); }
        }

        string _setPlay = "";
        public string SetPlay
        {
            get => _setPlay;
            set { _setPlay = value; OnPropertyChanged(); }
        }

        public string StartDisplay => FormatTime(StartSeconds);
        public string EndDisplay => FormatTime(EndSeconds);

        public static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}:{ts.Minutes:00}:{ts.Seconds:00}";
            return $"{ts.Minutes}:{ts.Seconds:00}";
        }

        void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
