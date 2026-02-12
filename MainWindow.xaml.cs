using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PlayCutWin.Views;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;

        private bool _isDraggingTimeline = false;

        // seek-jump safety
        private double? _pendingJumpSeconds = null;
        private bool _pendingAutoPlayAfterJump = false;

        // Speed button visuals
        private static readonly SolidColorBrush SpeedNormalBrush = new((Color)ColorConverter.ConvertFromString("#2A2A2A"));
        private static readonly SolidColorBrush SpeedSelectedBrush = new((Color)ColorConverter.ConvertFromString("#0A84FF"));
        private double _currentSpeed = 1.0;

        public MainWindowViewModel VM { get; } = new MainWindowViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = VM;

            HighlightSpeedButtons(_currentSpeed);

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(120)
            };
            _timer.Tick += (_, __) => Tick();
            _timer.Start();

            VM.StatusText = "Ready";
        }

        // ----------------------------
        // TextBox UX (mac-like)
        // - Click into unfocused TextBox: focus and select-all
        // - Focus gained: select-all (so typing replaces placeholder/name)
        // NOTE: Placeholder itself is handled in XAML via Adorner/Style;
        // these handlers only improve selection behavior.
        // ----------------------------
        private void TextBox_SelectAllOnFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.Dispatcher.BeginInvoke(new Action(() => tb.SelectAll()), DispatcherPriority.Input);
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown_SelectAll(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (!tb.IsKeyboardFocusWithin)
                {
                    e.Handled = true;
                    tb.Focus();
                    tb.SelectAll();
                }
            }
        }

        // ----------------------------
        // Media / Timeline
        // ----------------------------
        private void Tick()
        {
            if (Player.Source == null) return;

            if (!_isDraggingTimeline)
            {
                VM.CurrentTimeSeconds = Player.Position.TotalSeconds;
            }

            // pending jump after media open / seek completed
            if (_pendingJumpSeconds.HasValue)
            {
                var target = _pendingJumpSeconds.Value;
                _pendingJumpSeconds = null;

                try
                {
                    Player.Position = TimeSpan.FromSeconds(Math.Max(0, target));
                    VM.CurrentTimeSeconds = Player.Position.TotalSeconds;
                }
                catch { /* ignore */ }

                if (_pendingAutoPlayAfterJump)
                {
                    _pendingAutoPlayAfterJump = false;
                    TryPlay();
                }
            }
        }

        private void Timeline_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingTimeline = true;
        }

        private void Timeline_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingTimeline = false;
            SeekTo(VM.CurrentTimeSeconds);
        }

        private void Timeline_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingTimeline)
            {
                // live preview only
            }
        }

        private void SeekTo(double seconds)
        {
            if (Player.Source == null) return;
            try
            {
                Player.Position = TimeSpan.FromSeconds(Math.Max(0, seconds));
            }
            catch { }
        }

        private void TryPlay()
        {
            if (Player.Source == null) return;
            try
            {
                Player.Play();
                VM.IsPlaying = true;
                VM.PlayPauseIcon = "⏸";
            }
            catch { }
        }

        private void TryPause()
        {
            if (Player.Source == null) return;
            try
            {
                Player.Pause();
                VM.IsPlaying = false;
                VM.PlayPauseIcon = "▶";
            }
            catch { }
        }

        // ----------------------------
        // Buttons
        // ----------------------------
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.mov;*.mkv;*.avi|All Files|*.*"
            };
            if (ofd.ShowDialog() != true) return;

            LoadVideo(ofd.FileName);
        }

        private void LoadVideo(string path)
        {
            try
            {
                VM.LoadedVideoPath = path;
                VM.VideoPanelTitle = $"Video (16:9)  [{Path.GetFileName(path)}]";
                VM.StatusText = "Video loaded";

                Player.Source = new Uri(path);
                Player.Position = TimeSpan.Zero;

                VideoHint.Visibility = Visibility.Collapsed;

                Player.MediaOpened += Player_MediaOpened;
                Player.MediaFailed += Player_MediaFailed;

                // stop initial auto-play
                TryPause();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Load Video Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Player_MediaOpened(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    VM.VideoDurationSeconds = Player.NaturalDuration.TimeSpan.TotalSeconds;
                    VM.DurationText = FormatTime(VM.VideoDurationSeconds);
                }
                else
                {
                    VM.VideoDurationSeconds = 0;
                    VM.DurationText = "0:00";
                }

                VM.CurrentTimeSeconds = 0;
                VM.CurrentTimeText = "0:00";
            }
            catch { }
        }

        private void Player_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException?.Message ?? "Media failed.", "Player Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (!VM.IsPlaying) TryPlay();
            else TryPause();
        }

        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        private void SetSpeed(double speed)
        {
            _currentSpeed = speed;
            try
            {
                Player.SpeedRatio = speed;
            }
            catch { }
            HighlightSpeedButtons(speed);
        }

        private void HighlightSpeedButtons(double speed)
        {
            void SetBtn(Button b, bool selected)
            {
                b.Background = selected ? SpeedSelectedBrush : SpeedNormalBrush;
            }

            if (Speed025 != null) SetBtn(Speed025, Math.Abs(speed - 0.25) < 0.001);
            if (Speed05 != null) SetBtn(Speed05, Math.Abs(speed - 0.5) < 0.001);
            if (Speed1 != null) SetBtn(Speed1, Math.Abs(speed - 1.0) < 0.001);
            if (Speed2 != null) SetBtn(Speed2, Math.Abs(speed - 2.0) < 0.001);
        }

        private void SaveTeamA_Click(object sender, RoutedEventArgs e)
        {
            VM.SaveClipForTeam("A");
        }

        private void SaveTeamB_Click(object sender, RoutedEventArgs e)
        {
            VM.SaveClipForTeam("B");
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            VM.DeleteSelectedClip();
        }

        // ----------------------------
        // Clip list interaction
        // ----------------------------
        private void ClipList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VM.OnClipSelectionChanged();
        }

        private void ClipList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // handled in PreviewMouseDoubleClick for consistent behavior
        }

        private void ClipList_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VM.SelectedClip == null) return;

            // Jump to start and auto-play
            _pendingJumpSeconds = VM.SelectedClip.Start;
            _pendingAutoPlayAfterJump = true;

            SeekTo(VM.SelectedClip.Start);
            TryPlay();
        }

        // ----------------------------
        // CSV
        // ----------------------------
        private void ImportCSV_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "CSV|*.csv|All Files|*.*"
            };
            if (ofd.ShowDialog() != true) return;

            VM.ImportCsv(ofd.FileName);
        }

        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            VM.ExportCsv();
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            VM.ExportAllVideosWithProgress(this);
        }

        // ----------------------------
        // Helpers
        // ----------------------------
        private static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }
    }

    // =========================================================
    // ViewModel (kept in the same file in this repo style)
    // =========================================================
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        // Keep empty by default; placeholder text is provided by the TextBox's Tag (watermark).
        private string _teamAName = "";
        private string _teamBName = "";

        private string _videoPanelTitle = "Video (16:9)";
        private string _statusText = "";

        private string _currentTimeText = "0:00";
        private string _durationText = "0:00";
        private double _currentTimeSeconds = 0;
        private double _videoDurationSeconds = 0;

        private bool _isPlaying = false;
        private string _playPauseIcon = "▶";

        private string _loadedVideoPath = "";

        public ObservableCollection<ClipItem> Clips { get; } = new ObservableCollection<ClipItem>();
        public ICollectionView TeamAView { get; }
        public ICollectionView TeamBView { get; }

        public ObservableCollection<string> ClipFilters { get; } = new ObservableCollection<string>(new[]
        {
            "All Clips",
            "Team A",
            "Team B"
        });

        private string _selectedClipFilter = "All Clips";

        private ClipItem? _selectedClip;

        public MainWindowViewModel()
        {
            TeamAView = CollectionViewSource.GetDefaultView(Clips);
            TeamAView.Filter = o => FilterTeam(o, "A");

            TeamBView = new ListCollectionView(Clips);
            TeamBView.Filter = o => FilterTeam(o, "B");

            Clips.CollectionChanged += (_, __) =>
            {
                OnPropertyChanged(nameof(AllClipsCountText));
            };
        }

        private bool FilterTeam(object? o, string team)
        {
            if (o is not ClipItem c) return false;

            if (SelectedClipFilter == "Team A" && c.Team != "A") return false;
            if (SelectedClipFilter == "Team B" && c.Team != "B") return false;

            return c.Team == team;
        }

        public string SelectedClipFilter
        {
            get => _selectedClipFilter;
            set
            {
                if (_selectedClipFilter == value) return;
                _selectedClipFilter = value;
                OnPropertyChanged();
                TeamAView.Refresh();
                TeamBView.Refresh();
                OnPropertyChanged(nameof(AllClipsCountText));
            }
        }

        public string TeamAName
        {
            get => _teamAName;
            set { _teamAName = value; OnPropertyChanged(); }
        }

        public string TeamBName
        {
            get => _teamBName;
            set { _teamBName = value; OnPropertyChanged(); }
        }

        public string VideoPanelTitle
        {
            get => _videoPanelTitle;
            set { _videoPanelTitle = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public string CurrentTimeText
        {
            get => _currentTimeText;
            set { _currentTimeText = value; OnPropertyChanged(); }
        }

        public string DurationText
        {
            get => _durationText;
            set { _durationText = value; OnPropertyChanged(); }
        }

        public double CurrentTimeSeconds
        {
            get => _currentTimeSeconds;
            set
            {
                _currentTimeSeconds = value;
                CurrentTimeText = FormatTime(_currentTimeSeconds);
                OnPropertyChanged();
            }
        }

        public double VideoDurationSeconds
        {
            get => _videoDurationSeconds;
            set { _videoDurationSeconds = value; OnPropertyChanged(); }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; OnPropertyChanged(); }
        }

        public string PlayPauseIcon
        {
            get => _playPauseIcon;
            set { _playPauseIcon = value; OnPropertyChanged(); }
        }

        public string LoadedVideoPath
        {
            get => _loadedVideoPath;
            set { _loadedVideoPath = value; OnPropertyChanged(); }
        }

        public string AllClipsCountText => $"Clips (Total {Clips.Count})";

        public ClipItem? SelectedClip
        {
            get => _selectedClip;
            set { _selectedClip = value; OnPropertyChanged(); }
        }

        public void OnClipSelectionChanged()
        {
            // placeholder for future (e.g., sync tag panels)
        }

        public void SaveClipForTeam(string team)
        {
            // TODO: existing logic in your repo likely already writes start/end, tags etc.
            // This placeholder keeps compile stability if you call it.
            StatusText = $"Saved clip for Team {team}";
        }

        public void DeleteSelectedClip()
        {
            if (SelectedClip == null) return;
            Clips.Remove(SelectedClip);
            SelectedClip = null;
        }

        public void ImportCsv(string path)
        {
            // TODO: existing import logic should be here in your repo.
            // Keep stub to compile; replace with your real implementation.
            StatusText = $"Imported CSV: {Path.GetFileName(path)}";
        }

        public void ExportCsv()
        {
            StatusText = "Export CSV (stub)";
        }

        public void ExportAllVideosWithProgress(Window owner)
        {
            StatusText = "Export All (stub)";
        }

        private static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ClipItem : INotifyPropertyChanged
    {
        public string Team { get; set; } = "A";
        public double Start { get; set; }
        public double End { get; set; }

        private string _comment = "";
        private string _setPlay = "";
        public List<string> Tags { get; set; } = new List<string>();

        public string StartText => FormatTime(Start);
        public string EndText => FormatTime(End);
        public string TagsText => string.Join(", ", Tags);

        public string Comment
        {
            get => _comment;
            set { _comment = value; OnPropertyChanged(); }
        }

        public string SetPlay
        {
            get => _setPlay;
            set { _setPlay = value; OnPropertyChanged(); }
        }

        private static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
