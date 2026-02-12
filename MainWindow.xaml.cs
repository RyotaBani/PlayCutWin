using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly MainVm _vm = new();
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
        private bool _isDraggingSlider;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;

            _timer.Tick += (_, _) =>
            {
                if (!_vm.HasVideo) return;
                if (_isDraggingSlider) return;

                try
                {
                    var pos = Player.Position.TotalSeconds;
                    var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.TotalSeconds : 0;
                    if (dur > 0)
                    {
                        TimelineSlider.Maximum = dur;
                        TimelineSlider.Value = Math.Max(0, Math.Min(dur, pos));
                        _vm.TimeText = $"{FormatTime(pos)} / {FormatTime(dur)}";
                    }
                }
                catch { /* ignore */ }
            };
        }

        // =========================
        // Video
        // =========================
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.mov;*.mkv;*.avi|All Files|*.*"
            };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    Player.Source = new Uri(ofd.FileName);
                    Player.Position = TimeSpan.Zero;
                    Player.Play();
                    _vm.IsPlaying = true;
                    _vm.HasVideo = true;
                    _vm.LoadedVideoName = System.IO.Path.GetFileName(ofd.FileName);
                    _vm.VideoHeaderText = $"Video (16:9)   {_vm.LoadedVideoName}";
                    _timer.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Load Video Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            TogglePlay();
        }

        private void TogglePlay()
        {
            if (!_vm.HasVideo) return;

            if (_vm.IsPlaying)
            {
                Player.Pause();
                _vm.IsPlaying = false;
            }
            else
            {
                Player.Play();
                _vm.IsPlaying = true;
            }
        }

        private void StepSeconds(double sec)
        {
            if (!_vm.HasVideo) return;
            var p = Player.Position.TotalSeconds + sec;
            if (p < 0) p = 0;
            Player.Position = TimeSpan.FromSeconds(p);
        }

        private void Back5_Click(object sender, RoutedEventArgs e) => StepSeconds(-5);
        private void Back1_Click(object sender, RoutedEventArgs e) => StepSeconds(-1);
        private void Fwd1_Click(object sender, RoutedEventArgs e) => StepSeconds(+1);
        private void Fwd5_Click(object sender, RoutedEventArgs e) => StepSeconds(+5);

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_vm.HasVideo) return;
            if (_isDraggingSlider) return;
            // value change from timer is handled; user drag uses preview events if you add later
        }

        // =========================
        // Keyboard
        // =========================
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                TogglePlay();
                e.Handled = true;
                return;
            }

            // ← → コマ送り（ざっくり 0.25s。必要なら後で frame step に寄せる）
            if (e.Key == Key.Left)
            {
                StepSeconds(-0.25);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Right)
            {
                StepSeconds(+0.25);
                e.Handled = true;
                return;
            }
        }

        // =========================
        // Clips (stub for now)
        // =========================
        private void SaveTeamA_Click(object sender, RoutedEventArgs e)
        {
            // ここは今後「開始/終了」「タグ」「ノート」を詰めていく
            var clip = _vm.MakeDummyClip("A");
            _vm.TeamAClips.Add(clip);
            _vm.UpdateHeaders();
        }

        private void SaveTeamB_Click(object sender, RoutedEventArgs e)
        {
            var clip = _vm.MakeDummyClip("B");
            _vm.TeamBClips.Add(clip);
            _vm.UpdateHeaders();
        }

        private void ClipGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not DataGrid dg) return;
            if (dg.SelectedItem is ClipItem clip)
            {
                _vm.SelectedClip = clip;
                ClipNoteBox.Text = clip.Note ?? "";
            }
        }

        private void ClipGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // ダブルクリックでジャンプ（仮：Startへ）
            if (sender is not DataGrid dg) return;
            if (dg.SelectedItem is ClipItem clip)
            {
                Player.Position = TimeSpan.FromSeconds(clip.StartSeconds);
                Player.Play();
                _vm.IsPlaying = true;
            }
        }

        // =========================
        // CSV buttons (stub)
        // =========================
        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import CSV: ここは次の段階で Mac完全一致の実装を入れ直します。", "Info");
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export CSV: ここは次の段階で Mac完全一致の実装を入れ直します。", "Info");
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export All: 進捗ダイアログ＆キャンセル対応は次の段階で戻します。", "Info");
        }

        // Filter ComboBox handler (AllClips_Click という名前のイベントをXAMLが要求している)
        private void AllClips_Click(object sender, SelectionChangedEventArgs e)
        {
            _vm.UpdateHeaders();
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences: 後でショートカット/タグ編集UIをまとめます。", "Info");
        }

        // =========================
        // Tags
        // =========================
        private void Tag_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb && tb.Content is string tag)
            {
                _vm.AddTag(tag);
            }
        }

        private void Tag_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb && tb.Content is string tag)
            {
                _vm.RemoveTag(tag);
            }
        }

        private void AddCustomTag_Click(object sender, RoutedEventArgs e)
        {
            var t = (CustomTagBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(t)) return;
            _vm.AddTag(t);
            CustomTagBox.Text = "";
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            _vm.ClearTags();
        }

        // =========================
        // TextBox "click to select all" handlers
        // =========================
        private void TextBox_SelectAllOnFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown_SelectAll(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
                tb.SelectAll();
            }
        }

        private static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
        }
    }

    // =========================================
    // ViewModel / Models (minimum stable)
    // =========================================
    public class MainVm : INotifyPropertyChanged
    {
        public ObservableCollection<ClipItem> TeamAClips { get; } = new();
        public ObservableCollection<ClipItem> TeamBClips { get; } = new();

        private ClipItem? _selectedClip;
        public ClipItem? SelectedClip
        {
            get => _selectedClip;
            set { _selectedClip = value; OnPropertyChanged(); }
        }

        private bool _hasVideo;
        public bool HasVideo
        {
            get => _hasVideo;
            set { _hasVideo = value; OnPropertyChanged(); }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; OnPropertyChanged(); }
        }

        private string _loadedVideoName = "";
        public string LoadedVideoName
        {
            get => _loadedVideoName;
            set { _loadedVideoName = value; OnPropertyChanged(); }
        }

        private string _videoHeaderText = "Video (16:9)";
        public string VideoHeaderText
        {
            get => _videoHeaderText;
            set { _videoHeaderText = value; OnPropertyChanged(); }
        }

        private string _clipsHeaderText = "Clips (Total 0)";
        public string ClipsHeaderText
        {
            get => _clipsHeaderText;
            set { _clipsHeaderText = value; OnPropertyChanged(); }
        }

        private string _timeText = "0:00 / 0:00";
        public string TimeText
        {
            get => _timeText;
            set { _timeText = value; OnPropertyChanged(); }
        }

        private string _teamAName = "";
        public string TeamAName
        {
            get => _teamAName;
            set { _teamAName = value; OnPropertyChanged(); }
        }

        private string _teamBName = "";
        public string TeamBName
        {
            get => _teamBName;
            set { _teamBName = value; OnPropertyChanged(); }
        }

        private readonly ObservableCollection<string> _currentTags = new();
        public string CurrentTagsText => _currentTags.Count == 0 ? "(No tags selected)" : string.Join(", ", _currentTags);

        public void AddTag(string tag)
        {
            if (!_currentTags.Contains(tag))
            {
                _currentTags.Add(tag);
                OnPropertyChanged(nameof(CurrentTagsText));
            }
        }

        public void RemoveTag(string tag)
        {
            if (_currentTags.Contains(tag))
            {
                _currentTags.Remove(tag);
                OnPropertyChanged(nameof(CurrentTagsText));
            }
        }

        public void ClearTags()
        {
            _currentTags.Clear();
            OnPropertyChanged(nameof(CurrentTagsText));
        }

        public ClipItem MakeDummyClip(string team)
        {
            // 仮：あとで「実際のStart/End」「タグ」「ノート」「SetPlay」を詰める
            var start = 60 * (TeamAClips.Count + TeamBClips.Count + 1);
            var end = start + 8;
            var tags = _currentTags.Count == 0 ? "" : string.Join(", ", _currentTags);

            return new ClipItem
            {
                Team = team,
                StartSeconds = start,
                EndSeconds = end,
                StartText = TimeSpan.FromSeconds(start).ToString(@"m\:ss"),
                EndText = TimeSpan.FromSeconds(end).ToString(@"m\:ss"),
                TagsText = tags,
                Note = ""
            };
        }

        public void UpdateHeaders()
        {
            ClipsHeaderText = $"Clips (Total {TeamAClips.Count + TeamBClips.Count})";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ClipItem
    {
        public string Team { get; set; } = "";
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }
        public string StartText { get; set; } = "";
        public string EndText { get; set; } = "";
        public string TagsText { get; set; } = "";
        public string? Note { get; set; }
    }
}
