using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;

        private bool _isDraggingTimeline = false;
        private bool _ignoreSliderChange = false;

        private double? _pendingJumpSeconds = null;

        public MainWindowViewModel VM { get; } = new MainWindowViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = VM;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _timer.Tick += (_, __) => Tick();
            _timer.Start();

            VM.StatusText = "Ready";
        }

        // ----------------------------
        // Video
        // ----------------------------
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select video",
                Filter = "Video Files (*.mp4;*.mov;*.m4v)|*.mp4;*.mov;*.m4v|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                VM.LoadedVideoPath = dlg.FileName;
                VM.LoadedVideoName = Path.GetFileName(dlg.FileName);
                VM.StatusText = "Loading video…";

                if (VideoHint != null) VideoHint.Visibility = Visibility.Collapsed;

                Player.Stop();
                Player.Source = new Uri(dlg.FileName, UriKind.Absolute);

                // Load only (do not auto play)
                VM.IsPlaying = false;
                VM.StatusText = "Video loaded.";
            }
            catch (Exception ex)
            {
                VM.StatusText = $"Load failed: {ex.Message}";
                MessageBox.Show(ex.Message, "Load Video Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Play();
            VM.IsPlaying = true;
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            Player.Pause();
            VM.IsPlaying = false;
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                VM.DurationSeconds = Player.NaturalDuration.TimeSpan.TotalSeconds;
                TimelineSlider.Maximum = VM.DurationSeconds;

                // seek pending jump if exists
                if (_pendingJumpSeconds.HasValue)
                {
                    var s = _pendingJumpSeconds.Value;
                    _pendingJumpSeconds = null;
                    SeekToSeconds(s);
                }

                VM.StatusText = "Ready";
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            VM.StatusText = "Media failed.";
            MessageBox.Show(e.ErrorException?.Message ?? "Unknown media error", "Media Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Tick()
        {
            if (Player.Source == null) return;
            if (!Player.NaturalDuration.HasTimeSpan) return;

            if (!_isDraggingTimeline)
            {
                VM.CurrentSeconds = Player.Position.TotalSeconds;

                _ignoreSliderChange = true;
                TimelineSlider.Value = VM.CurrentSeconds;
                _ignoreSliderChange = false;
            }

            VM.TimeDisplay = $"{FormatTime(Player.Position.TotalSeconds)} / {FormatTime(VM.DurationSeconds)}";
            if (TxtTime != null) TxtTime.Text = VM.TimeDisplay;
        }

        // ----------------------------
        // Timeline (match XAML handlers)
        // ----------------------------
        private void Timeline_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingTimeline = true;
        }

        private void Timeline_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingTimeline = false;
            SeekToSeconds(TimelineSlider.Value);
        }

        private void Timeline_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ignoreSliderChange) return;

            if (_isDraggingTimeline)
            {
                VM.CurrentSeconds = TimelineSlider.Value;
                VM.TimeDisplay = $"{FormatTime(VM.CurrentSeconds)} / {FormatTime(VM.DurationSeconds)}";
                if (TxtTime != null) TxtTime.Text = VM.TimeDisplay;
            }
        }

        // ----------------------------
        // Safe seek (Jump only)
        // ----------------------------
        private void SeekToSeconds(double seconds)
        {
            if (Player.Source == null) return;

            // duration not ready -> defer
            if (!Player.NaturalDuration.HasTimeSpan)
            {
                _pendingJumpSeconds = seconds;
                return;
            }

            var max = Player.NaturalDuration.TimeSpan.TotalSeconds;
            if (double.IsNaN(max) || max <= 0) max = seconds;

            if (seconds < 0) seconds = 0;
            if (seconds > max) seconds = max;

            // Important: seek only. Do NOT call Play/Stop here.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Player.Position = TimeSpan.FromSeconds(seconds);
                    VM.CurrentSeconds = seconds;

                    _ignoreSliderChange = true;
                    TimelineSlider.Value = seconds;
                    _ignoreSliderChange = false;
                }
                catch
                {
                    // ignore seek failure to avoid crash
                }
            }), DispatcherPriority.Background);
        }

        // ----------------------------
        // Clip list: double click -> Jump only
        // ----------------------------
        private void ClipList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ClipList.SelectedItem is ClipRow row)
            {
                SeekToSeconds(row.Start);
            }
        }

        private void ClipList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // optional: keep as no-op
        }

        // ----------------------------
        // Clip actions (optional)
        // ----------------------------
        private void AddClip_Click(object sender, RoutedEventArgs e)
        {
            // Minimal: add 5s clip around current time for test
            if (Player.Source == null) return;

            var start = Math.Max(0, Player.Position.TotalSeconds);
            var end = Math.Min(VM.DurationSeconds, start + 5.0);

            var item = new ClipRow
            {
                Team = "A",
                Start = start,
                End = end,
                Tags = new List<string>()
            };

            VM.AllClips.Add(item);
            VM.StatusText = "Clip added.";
        }

        private void DeleteSelectedClip_Click(object sender, RoutedEventArgs e)
        {
            if (ClipList.SelectedItem is ClipRow row)
            {
                VM.AllClips.Remove(row);
                VM.StatusText = "Clip deleted.";
            }
        }

        // ----------------------------
        // Utils
        // ----------------------------
        private static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "--:--";
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        private static string EscapeCsv(string s)
        {
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static List<string> SplitCsv(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        sb.Append('\"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString());
            return result;
        }

        private static double ParseTimeToSeconds(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var secNum))
                return secNum;
            if (double.TryParse(s, out secNum))
                return secNum;

            var parts = s.Split(':');
            try
            {
                if (parts.Length == 2)
                {
                    int minutes = int.Parse(parts[0]);
                    double seconds = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    return minutes * 60 + seconds;
                }
                if (parts.Length == 3)
                {
                    int hours = int.Parse(parts[0]);
                    int minutes = int.Parse(parts[1]);
                    double seconds = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    return hours * 3600 + minutes * 60 + seconds;
                }
            }
            catch { }
            return 0;
        }
    }

    // ============================
    // ViewModel / Models (minimal & consistent)
    // ============================
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _loadedVideoName = "";
        private string _loadedVideoPath = "";
        private string _statusText = "";
        private bool _isPlaying = false;

        private double _durationSeconds = 0;
        private double _currentSeconds = 0;
        private string _timeDisplay = "--:-- / --:--";

        public ObservableCollection<ClipRow> AllClips { get; } = new ObservableCollection<ClipRow>();

        public string LoadedVideoName { get => _loadedVideoName; set { _loadedVideoName = value; OnPropertyChanged(); } }
        public string LoadedVideoPath { get => _loadedVideoPath; set { _loadedVideoPath = value; OnPropertyChanged(); } }

        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

        public bool IsPlaying { get => _isPlaying; set { _isPlaying = value; OnPropertyChanged(); } }

        public double DurationSeconds { get => _durationSeconds; set { _durationSeconds = value; OnPropertyChanged(); } }
        public double CurrentSeconds { get => _currentSeconds; set { _currentSeconds = value; OnPropertyChanged(); } }

        public string TimeDisplay { get => _timeDisplay; set { _timeDisplay = value; OnPropertyChanged(); } }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ClipRow
    {
        public string Team { get; set; } = "A";
        public double Start { get; set; }
        public double End { get; set; }
        public List<string> Tags { get; set; } = new List<string>();

        public string StartText => FormatTime(Start);
        public string EndText => FormatTime(End);
        public string TagsText => Tags == null || Tags.Count == 0 ? "" : string.Join(", ", Tags);

        private static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }
    }
}
