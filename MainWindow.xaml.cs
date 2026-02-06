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

                VideoHint.Visibility = Visibility.Collapsed;

                Player.Stop();
                Player.Source = new Uri(dlg.FileName, UriKind.Absolute);

                // auto-play? keep paused by default
                Player.LoadedBehavior = MediaState.Manual;
                Player.UnloadedBehavior = MediaState.Manual;

                Player.MediaOpened += (_, __) =>
                {
                    VM.VideoDurationSeconds = Player.NaturalDuration.HasTimeSpan
                        ? Player.NaturalDuration.TimeSpan.TotalSeconds
                        : 0;

                    VM.StatusText = "Video loaded";
                    UpdateTimeTexts();
                };
            }
            catch (Exception ex)
            {
                VM.StatusText = "Load failed";
                MessageBox.Show(ex.ToString(), "Load Video Error");
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            TogglePlayPause();
        }

        private void TogglePlayPause()
        {
            if (Player.Source == null) return;

            if (VM.IsPlaying)
            {
                Player.Pause();
                VM.IsPlaying = false;
            }
            else
            {
                Player.Play();
                VM.IsPlaying = true;
            }
        }

        private void SeekRelative(double deltaSeconds)
        {
            if (Player.Source == null) return;

            var newPos = Math.Max(0, Player.Position.TotalSeconds + deltaSeconds);
            newPos = Math.Min(VM.VideoDurationSeconds, newPos);

            Player.Position = TimeSpan.FromSeconds(newPos);
            VM.CurrentTimeSeconds = newPos;
            UpdateTimeTexts();
        }

        private void JumpToSeconds(double seconds, bool autoPlayAfter = false)
        {
            if (Player.Source == null) return;

            seconds = Math.Max(0, Math.Min(VM.VideoDurationSeconds, seconds));

            _pendingJumpSeconds = seconds;
            _pendingAutoPlayAfterJump = autoPlayAfter;

            Player.Position = TimeSpan.FromSeconds(seconds);
            VM.CurrentTimeSeconds = seconds;
            UpdateTimeTexts();

            // MediaElement sometimes needs a tick to “settle” after jump
        }

        private void Tick()
        {
            if (Player.Source == null) return;

            if (_pendingJumpSeconds.HasValue)
            {
                // settle
                var target = _pendingJumpSeconds.Value;
                var cur = Player.Position.TotalSeconds;

                if (Math.Abs(cur - target) < 0.2)
                {
                    _pendingJumpSeconds = null;

                    if (_pendingAutoPlayAfterJump)
                    {
                        _pendingAutoPlayAfterJump = false;
                        if (!VM.IsPlaying)
                        {
                            Player.Play();
                            VM.IsPlaying = true;
                        }
                    }
                }
            }

            if (!_isDraggingTimeline)
            {
                VM.CurrentTimeSeconds = Player.Position.TotalSeconds;
                UpdateTimeTexts();
            }
        }

        private void Timeline_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingTimeline = true;
        }

        private void Timeline_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingTimeline = false;
            Player.Position = TimeSpan.FromSeconds(VM.CurrentTimeSeconds);
            UpdateTimeTexts();
        }

        private void Timeline_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingTimeline)
            {
                UpdateTimeTexts();
            }
        }

        private void UpdateTimeTexts()
        {
            VM.CurrentTimeText = FormatTime(VM.CurrentTimeSeconds);
            VM.DurationText = FormatTime(VM.VideoDurationSeconds);
        }

        private static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        // ----------------------------
        // Speed UI
        // ----------------------------
        private void SetSpeed(double speed)
        {
            _currentSpeed = speed;
            Player.SpeedRatio = speed;
            HighlightSpeedButtons(speed);
        }

        private void HighlightSpeedButtons(double speed)
        {
            // this relies on XAML buttons named Speed025/Speed05/Speed1/Speed2
            // if missing, ignore
            void Mark(Button? b, bool selected)
            {
                if (b == null) return;
                b.Background = selected ? SpeedSelectedBrush : SpeedNormalBrush;
            }

            Mark(FindName("Speed025") as Button, Math.Abs(speed - 0.25) < 0.001);
            Mark(FindName("Speed05") as Button, Math.Abs(speed - 0.5) < 0.001);
            Mark(FindName("Speed1") as Button, Math.Abs(speed - 1.0) < 0.001);
            Mark(FindName("Speed2") as Button, Math.Abs(speed - 2.0) < 0.001);
        }

        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        // ----------------------------
        // Keyboard shortcuts
        // ----------------------------
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);

            if (e.Key == Key.Space)
            {
                TogglePlayPause();
                e.Handled = true;
                return;
            }

            // frame step
            if (e.Key == Key.Left)
            {
                SeekRelative(-0.1);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Right)
            {
                SeekRelative(+0.1);
                e.Handled = true;
                return;
            }
        }

        // ----------------------------
        // Clips / Save / Tagging
        // ----------------------------
        private void SaveTeamA_Click(object sender, RoutedEventArgs e)
        {
            VM.SaveClip("A");
        }

        private void SaveTeamB_Click(object sender, RoutedEventArgs e)
        {
            VM.SaveClip("B");
        }

        private void ClipList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView lv && lv.SelectedItem is ClipRow clip)
            {
                JumpToSeconds(clip.Start, autoPlayAfter: true);
            }
        }

        // XAML互換保険：PreviewMouseDoubleClickを張っててもビルドが通るようにする
        private void ClipList_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ClipList_DoubleClick(sender, e);
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            VM.DeleteSelectedClip();
        }

        // ----------------------------
        // CSV Export/Import
        // ----------------------------
        private void ExportCSV_Click(object sender, RoutedEventArgs e)
        {
            var clips = VM.GetAllClipsForExport();
            if (clips.Count == 0)
            {
                MessageBox.Show("No clips to export.");
                return;
            }

            var dlg = new SaveFileDialog
            {
                Title = "Export CSV",
                Filter = "CSV (*.csv)|*.csv",
                FileName = "play_by_play.csv"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var sb = new StringBuilder();

                // Mac (BBVideoTagger) CSV Schema v2
                // Header must match exactly for round-trip compatibility.
                sb.AppendLine("Schema,VideoName,No,TeamKey,TeamSide,TeamName,Start,End,StartSec,EndSec,DurationSec,Tags,SetPlay,Note");

                // ✅ FIX: MainWindow側のフィールドではなくVMの値を使う
                var videoName = string.IsNullOrWhiteSpace(VM.LoadedVideoName) ? "UnknownVideo" : VM.LoadedVideoName;
                var inv = CultureInfo.InvariantCulture;

                // Sort by start time to match Mac export order
                var sorted = clips.OrderBy(c => c.Start).ToList();

                for (int i = 0; i < sorted.Count; i++)
                {
                    var c = sorted[i];

                    var teamKey = NormalizeTeamToAB(c.Team);
                    var teamSide = teamKey == "A" ? "Home" : "Away";

                    // ✅ FIX: Team名もVMから
                    var teamName = teamKey == "A" ? VM.TeamAName : VM.TeamBName;

                    var startStr = FormatTime(c.Start);
                    var endStr = FormatTime(c.End);

                    var startSec = c.Start.ToString("0.000", inv);
                    var endSec = c.End.ToString("0.000", inv);
                    var durationSec = Math.Max(0, c.End - c.Start).ToString("0.000", inv);

                    // Tags: "; " (semicolon + space)
                    var tagsStr = string.Join("; ", c.Tags ?? new List<string>());
                    var setPlayStr = c.SetPlay ?? string.Empty;
                    var noteStr = c.Comment ?? string.Empty;

                    var row = string.Join(",", new[]
                    {
                        EscapeCsv("2"),
                        EscapeCsv(videoName),
                        EscapeCsv((i + 1).ToString(inv)),
                        EscapeCsv(teamKey),
                        EscapeCsv(teamSide),
                        EscapeCsv(teamName),
                        EscapeCsv(startStr),
                        EscapeCsv(endStr),
                        EscapeCsv(startSec),
                        EscapeCsv(endSec),
                        EscapeCsv(durationSec),
                        EscapeCsv(tagsStr),
                        EscapeCsv(setPlayStr),
                        EscapeCsv(noteStr),
                    });

                    sb.AppendLine(row);
                }

                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
                VM.StatusText = $"Exported CSV: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Export CSV Error");
            }
        }

        private void ImportCSV_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import CSV",
                Filter = "CSV (*.csv)|*.csv|All Files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var text = File.ReadAllText(dlg.FileName, Encoding.UTF8);

                VM.ImportCsv(text);
                VM.StatusText = $"Imported CSV: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Import CSV Error");
            }
        }

        private static string EscapeCsv(string s)
        {
            s ??= "";
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        private static string NormalizeTeamToAB(string team)
        {
            // team is stored as "A"/"B" or "Team A"/"Team B"
            if (string.IsNullOrWhiteSpace(team)) return "A";
            var t = team.Trim();
            if (t.Equals("A", StringComparison.OrdinalIgnoreCase) || t.Contains("Team A")) return "A";
            if (t.Equals("B", StringComparison.OrdinalIgnoreCase) || t.Contains("Team B")) return "B";
            return "A";
        }
    
        // ===== Compatibility handlers for MainWindow.xaml event names =====

        private void ClipList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lv)
            {
                VM.SelectedClip = lv.SelectedItem as ClipRow;
                VM.HasSelectedClip = VM.SelectedClip != null;
            }
        }

        private void DeleteSelectedClip_Click(object sender, RoutedEventArgs e)
        {
            // Forward to existing delete if present
            try { Delete_Click(sender, e); } catch { VM.DeleteSelectedClip(); }
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            // Forward to existing handler name
            ImportCSV_Click(sender, e);
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            ExportCSV_Click(sender, e);
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            // If you later add real ExportAll, replace this.
            MessageBox.Show("Export All は次で実装（現状はCSV出力のみ）", "Export",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            // Keep duration text in sync (LoadVideo_Click also subscribes; this is safe)
            if (Player.NaturalDuration.HasTimeSpan)
            {
                VM.VideoDurationSeconds = Player.NaturalDuration.TimeSpan.TotalSeconds;
                UpdateTimeTexts();
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show(e.ErrorException?.ToString() ?? "Media failed.", "Media Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Timeline_PreviewMouseDown(sender, e);
        }

        private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Timeline_PreviewMouseUp(sender, e);
        }

        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => SeekRelative(-5);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => SeekRelative(-1);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => SeekRelative(+1);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => SeekRelative(+5);

        private void ClipStart_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            VM.ClipStart = Player.Position.TotalSeconds;
            VM.StatusText = $"START: {FormatTime(VM.ClipStart)}";
        }

        private void ClipEnd_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            VM.ClipEnd = Player.Position.TotalSeconds;
            VM.StatusText = $"END: {FormatTime(VM.ClipEnd)}";
        }

        private void AddCustomTag_Click(object sender, RoutedEventArgs e)
        {
            var name = (VM.CustomTagInput ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            if (VM.CustomTags.Any(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                VM.CustomTagInput = "";
                return;
            }

            var item = new TagItem(name);
            VM.AttachTag(item);
            VM.CustomTags.Add(item);
            VM.CustomTagInput = "";
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            foreach (var t in VM.OffenseTags) t.IsChecked = false;
            foreach (var t in VM.DefenseTags) t.IsChecked = false;
            foreach (var t in VM.CustomTags) t.IsChecked = false;

            // Apply to selected clip if any
            if (VM.SelectedClip != null)
            {
                // triggers tag change via TagToggled subscriptions
                VM.StatusText = "Tags cleared";
            }
        }
}

    // ==========================================================
    // ViewModel / Models (kept here for single-file simplicity)
    // ==========================================================

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _loadedVideoPath = "";
        private string _loadedVideoName = "";

        private string _teamAName = "Home / Our Team";
        private string _teamBName = "Away / Opponent";

        private double _currentTimeSeconds;
        private double _videoDurationSeconds;

        private string _currentTimeText = "0:00";
        private string _durationText = "0:00";

        private bool _isPlaying = false;

        private string _statusText = "";

        private double _clipStart;
        private double _clipEnd;

        public ObservableCollection<ClipRow> TeamAClips { get; } = new();
        public ObservableCollection<ClipRow> TeamBClips { get; } = new();

        // ===== Clip Filters (UI) =====
        public ObservableCollection<string> ClipFilters { get; } = new ObservableCollection<string>(new[] { "All Clips", "Team A", "Team B" });

        private string _selectedClipFilter = "All Clips";
        public string SelectedClipFilter
        {
            get => _selectedClipFilter;
            set
            {
                if (_selectedClipFilter == value) return;
                _selectedClipFilter = value ?? "All Clips";
                OnPropertyChanged();
                RefreshClipViews();
            }
        }

        private ICollectionView _teamAView;
        public ICollectionView TeamAView
        {
            get => _teamAView;
            private set { _teamAView = value; OnPropertyChanged(); }
        }

        private ICollectionView _teamBView;
        public ICollectionView TeamBView
        {
            get => _teamBView;
            private set { _teamBView = value; OnPropertyChanged(); }
        }


        public ObservableCollection<TagItem> OffenseTags { get; } = new();
        public ObservableCollection<TagItem> DefenseTags { get; } = new();
        public ObservableCollection<TagItem> CustomTags { get; } = new();

        private ClipRow? _selectedClip;
        public ClipRow? SelectedClip
        {
            get => _selectedClip;
            set
            {
                if (_selectedClip == value) return;
                _selectedClip = value;
                HasSelectedClip = _selectedClip != null;
                OnPropertyChanged();
                SyncTagTogglesFromSelectedClip();
                OnPropertyChanged(nameof(SelectedClipHasSetTag));
            }
        }

        public bool SelectedClipHasSetTag
        {
            get
            {
                if (SelectedClip?.Tags == null) return false;
                return SelectedClip.Tags.Any(t => string.Equals(t, "Set", StringComparison.OrdinalIgnoreCase));
            }
        }

        public string LoadedVideoPath
        {
            get => _loadedVideoPath;
            set { _loadedVideoPath = value ?? ""; OnPropertyChanged(); }
        }

        public string LoadedVideoName
        {
            get => _loadedVideoName;
            set { _loadedVideoName = value ?? ""; OnPropertyChanged(); }
        }

        public string TeamAName { get => _teamAName; set { _teamAName = value; OnPropertyChanged(); } }
        public string TeamBName { get => _teamBName; set { _teamBName = value; OnPropertyChanged(); } }

        public double CurrentTimeSeconds
        {
            get => _currentTimeSeconds;
            set { _currentTimeSeconds = value; OnPropertyChanged(); }
        }

        public double VideoDurationSeconds
        {
            get => _videoDurationSeconds;
            set { _videoDurationSeconds = value; OnPropertyChanged(); }
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

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying == value) return;
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayPauseIcon));
            }
        }

        // Segoe MDL2 Assets: Play=E768, Pause=E769
        public string PlayPauseIcon => IsPlaying ? "\uE769" : "\uE768";

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        private bool _hasSelectedClip;
        public bool HasSelectedClip
        {
            get => _hasSelectedClip;
            set { _hasSelectedClip = value; OnPropertyChanged(); }
        }

        private string _customTagInput = string.Empty;
        public string CustomTagInput
        {
            get => _customTagInput;
            set { _customTagInput = value ?? string.Empty; OnPropertyChanged(); }
        }

        public double ClipStart
        {
            get => _clipStart;
            set { _clipStart = value; OnPropertyChanged(); }
        }

        public double ClipEnd
        {
            get => _clipEnd;
            set { _clipEnd = value; OnPropertyChanged(); }
        }

        public MainWindowViewModel()
        {
            // Default tags (example)
            OffenseTags.Add(new TagItem("Transition"));
            OffenseTags.Add(new TagItem("Set"));
            OffenseTags.Add(new TagItem("PnR"));
            OffenseTags.Add(new TagItem("BLOB"));
            OffenseTags.Add(new TagItem("SLOB"));
            OffenseTags.Add(new TagItem("vs M/M"));
            OffenseTags.Add(new TagItem("vs Zone"));
            OffenseTags.Add(new TagItem("2nd Attack"));
            OffenseTags.Add(new TagItem("3rd Attack more"));

            DefenseTags.Add(new TagItem("M/M"));
            DefenseTags.Add(new TagItem("Zone"));
            DefenseTags.Add(new TagItem("Rebound"));
            DefenseTags.Add(new TagItem("Steal"));

            foreach (var t in OffenseTags) t.PropertyChanged += TagToggled;
            foreach (var t in DefenseTags) t.PropertyChanged += TagToggled;

// Views for clip lists
TeamAView = CollectionViewSource.GetDefaultView(TeamAClips);
TeamBView = CollectionViewSource.GetDefaultView(TeamBClips);

// Filter by clip dropdown (XAML values: All Clips / Team A / Team B)
TeamAView.Filter = _ => SelectedClipFilter == "All Clips" || SelectedClipFilter == "Team A";
TeamBView.Filter = _ => SelectedClipFilter == "All Clips" || SelectedClipFilter == "Team B";

TeamAClips.CollectionChanged += (_, __) => RefreshClipViews();
TeamBClips.CollectionChanged += (_, __) => RefreshClipViews();
RefreshClipViews();
        }

        public string AllClipsCountText => $"Clips (Total {TeamAClips.Count + TeamBClips.Count})";


private void RefreshClipViews()
{
    TeamAView?.Refresh();
    TeamBView?.Refresh();
    OnPropertyChanged(nameof(AllClipsCountText));
}


        private void TagToggled(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(TagItem.IsChecked)) return;

            if (SelectedClip != null)
            {
                // edit selected clip tags
                ApplyTogglesToSelectedClip();
            }
        }

        private void ApplyTogglesToSelectedClip()
        {
            if (SelectedClip == null) return;

            var tags = new List<string>();

            void take(IEnumerable<TagItem> src)
            {
                foreach (var t in src)
                    if (t.IsChecked) tags.Add(t.Name);
            }

            take(OffenseTags);
            take(DefenseTags);
            take(CustomTags);

            SelectedClip.Tags = tags;
            OnPropertyChanged(nameof(SelectedClipHasSetTag));
        }

        public void AttachTag(TagItem item)
        {
            if (item == null) return;

            // Toggle selection state
            item.IsChecked = !item.IsChecked;

            // If a clip is selected, reflect immediately
            if (SelectedClip != null)
            {
                ApplyTogglesToSelectedClip();
                OnPropertyChanged(nameof(SelectedClipHasSetTag));
            }
        }


        private void SyncTagTogglesFromSelectedClip()
        {
            void clear(IEnumerable<TagItem> src)
            {
                foreach (var t in src) t.IsChecked = false;
            }

            clear(OffenseTags);
            clear(DefenseTags);
            clear(CustomTags);

            if (SelectedClip?.Tags == null) return;

            var set = new HashSet<string>(SelectedClip.Tags, StringComparer.OrdinalIgnoreCase);

            void apply(IEnumerable<TagItem> src)
            {
                foreach (var t in src)
                {
                    if (set.Contains(t.Name)) t.IsChecked = true;
                }
            }

            apply(OffenseTags);
            apply(DefenseTags);
            apply(CustomTags);
        }

        public void SaveClip(string teamKey)
        {
            var start = Math.Min(ClipStart, ClipEnd);
            var end = Math.Max(ClipStart, ClipEnd);
            if (end <= start) return;

            var tags = new List<string>();
            foreach (var t in OffenseTags) if (t.IsChecked) tags.Add(t.Name);
            foreach (var t in DefenseTags) if (t.IsChecked) tags.Add(t.Name);
            foreach (var t in CustomTags) if (t.IsChecked) tags.Add(t.Name);

            var clip = new ClipRow
            {
                Team = teamKey,
                Start = start,
                End = end,
                Tags = tags
            };

            if (teamKey == "A") TeamAClips.Add(clip);
            else TeamBClips.Add(clip);

            StatusText = $"Saved {teamKey} clip ({FormatTime(start)} - {FormatTime(end)})";
        }

        public void DeleteSelectedClip()
        {
            if (SelectedClip == null) return;

            if (SelectedClip.Team == "A") TeamAClips.Remove(SelectedClip);
            else TeamBClips.Remove(SelectedClip);

            SelectedClip = null;
            StatusText = "Deleted clip";
        }

        public List<ClipRow> GetAllClipsForExport()
        {
            var all = new List<ClipRow>();
            all.AddRange(TeamAClips);
            all.AddRange(TeamBClips);
            return all;
        }

                public void ImportCsv(string csvText)
        {
            if (string.IsNullOrWhiteSpace(csvText)) return;

            TeamAClips.Clear();
            TeamBClips.Clear();
            SelectedClip = null;
            HasSelectedClip = false;

            var lines = csvText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lines.Count < 2) return;

            var header = SplitCsvLine(lines[0]).Select(h => (h ?? "").Trim()).ToList();
            var hl = header.Select(h => h.ToLowerInvariant()).ToList();

            bool isMacV2 = hl.Contains("schema") && hl.Contains("videoname") && hl.Contains("teamkey") && hl.Contains("startsec");

            int idxTeamKey = hl.IndexOf("teamkey");
            int idxTeamOld = hl.IndexOf("team");
            int idxStartSec = hl.IndexOf("startsec");
            int idxEndSec = hl.IndexOf("endsec");
            int idxStart = hl.IndexOf("start");
            int idxEnd = hl.IndexOf("end");
            int idxDurSec = hl.IndexOf("durationsec");
            int idxDuration = hl.IndexOf("duration");
            int idxTags = hl.IndexOf("tags");
            int idxSetPlay = hl.IndexOf("setplay");
            int idxNote = hl.IndexOf("note");
            int idxComment = hl.IndexOf("comment");

            int imported = 0;

            for (int i = 1; i < lines.Count; i++)
            {
                var cols = SplitCsvLine(lines[i]);

                string teamKey = "A";
                double startSec = 0;
                double endSec = 0;

                if (isMacV2)
                {
                    teamKey = NormalizeTeamToAB(GetSafe(cols, idxTeamKey));
                    startSec = TryParseDouble(GetSafe(cols, idxStartSec));
                    endSec = TryParseDouble(GetSafe(cols, idxEndSec));

                    if (endSec <= 0 && idxDurSec >= 0)
                    {
                        var dur = TryParseDouble(GetSafe(cols, idxDurSec));
                        if (dur > 0) endSec = startSec + dur;
                    }

                    if (startSec <= 0 && idxStart >= 0) startSec = ParseTimeToSeconds(GetSafe(cols, idxStart));
                    if (endSec <= 0 && idxEnd >= 0) endSec = ParseTimeToSeconds(GetSafe(cols, idxEnd));
                }
                else
                {
                    if (idxTeamOld >= 0) teamKey = NormalizeTeamToAB(GetSafe(cols, idxTeamOld));
                    else teamKey = NormalizeTeamToAB(GetSafe(cols, idxTeamKey));

                    if (idxStartSec >= 0) startSec = TryParseDouble(GetSafe(cols, idxStartSec));
                    if (idxEndSec >= 0) endSec = TryParseDouble(GetSafe(cols, idxEndSec));

                    if (startSec <= 0 && idxStart >= 0) startSec = ParseTimeToSeconds(GetSafe(cols, idxStart));
                    if (endSec <= 0 && idxEnd >= 0) endSec = ParseTimeToSeconds(GetSafe(cols, idxEnd));

                    if (endSec <= 0)
                    {
                        double dur = 0;
                        if (idxDurSec >= 0) dur = TryParseDouble(GetSafe(cols, idxDurSec));
                        else if (idxDuration >= 0) dur = ParseTimeToSeconds(GetSafe(cols, idxDuration));

                        if (dur > 0) endSec = startSec + dur;
                    }
                }

                if (endSec <= startSec) continue;

                var tagsRaw = GetSafe(cols, idxTags);
                var setPlay = GetSafe(cols, idxSetPlay);
                var note = GetSafe(cols, idxNote);
                if (string.IsNullOrWhiteSpace(note) && idxComment >= 0) note = GetSafe(cols, idxComment);

                var clip = new ClipRow
                {
                    Team = teamKey,
                    Start = startSec,
                    End = endSec,
                    Tags = ParseTags(tagsRaw),
                    SetPlay = setPlay ?? string.Empty,
                    Comment = note ?? string.Empty
                };

                if (teamKey == "A") TeamAClips.Add(clip);
                else TeamBClips.Add(clip);

                imported++;
            }

            StatusText = $"Imported {imported} clips";
        }

        private static double TryParseDouble(string s)
        {
            if (double.TryParse((s ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            return 0;
        }

        private static string GetSafe(List<string> cols, int idx)
        {
            if (idx < 0) return "";
            if (idx >= cols.Count) return "";
            return cols[idx] ?? "";
        }

        private static List<string> ParseTags(string raw)
        {
            raw ??= "";
            raw = raw.Trim();
            if (raw.Length == 0) return new List<string>();

            var parts = raw
                .Split(new[] { ";", "|", "," }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return parts;
        }

        private static double ParseTimeToSeconds(string s)
        {
            s ??= "";
            s = s.Trim();
            if (s.Length == 0) return 0;

            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var sec)) return sec;

            var parts = s.Split(':');
            if (parts.Length == 2)
            {
                if (int.TryParse(parts[0], out var m) && double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var ss))
                    return m * 60 + ss;
            }
            else if (parts.Length == 3)
            {
                if (int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m) && double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var ss))
                    return h * 3600 + m * 60 + ss;
            }

            return 0;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null) return result;

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
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

    public class TagItem : INotifyPropertyChanged
    {
        public string Name { get; }

        // Optional grouping (compat with PlayCutWin.Models.TagItem usage)
        public PlayCutWin.Models.TagGroup Group { get; }

        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // Alias
        public bool IsSelected
        {
            get => IsChecked;
            set => IsChecked = value;
        }

        public TagItem(string name) : this(name, PlayCutWin.Models.TagGroup.Offense) { }

        public TagItem(string name, PlayCutWin.Models.TagGroup group)
        {
            Name = name;
            Group = group;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string prop)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }

    public class ClipRow : INotifyPropertyChanged
    {
        private string _team = "A";
        private double _start;
        private double _end;
        private List<string> _tags = new();
        private string _comment = "";
        private string _setPlay = "";

        public string Team { get => _team; set { _team = value ?? "A"; OnPropertyChanged(); } }

        public double Start { get => _start; set { _start = value; OnPropertyChanged(); OnPropertyChanged(nameof(StartText)); } }
        public double End { get => _end; set { _end = value; OnPropertyChanged(); OnPropertyChanged(nameof(EndText)); } }

        public List<string> Tags
        {
            get => _tags;
            set { _tags = value ?? new List<string>(); OnPropertyChanged(); OnPropertyChanged(nameof(TagsText)); }
        }

        public string Comment
        {
            get => _comment;
            set { _comment = value ?? ""; OnPropertyChanged(); }
        }

        public string SetPlay
        {
            get => _setPlay;
            set { _setPlay = value ?? ""; OnPropertyChanged(); }
        }

        public string StartText => FormatTime(Start);
        public string EndText => FormatTime(End);
        public string TagsText => Tags == null || Tags.Count == 0 ? "" : string.Join(", ", Tags);

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
