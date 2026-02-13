using Microsoft.Win32;
using PlayCutWin.Models;
using PlayCutWin.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<Clip> _teamA = new();
        private readonly ObservableCollection<Clip> _teamB = new();

        private readonly HashSet<string> _currentTags = new(StringComparer.OrdinalIgnoreCase);

        private Clip? _selectedClip;
        private bool _isPlaying;
        private double _clipStartSec;
        private double _clipEndSec;

        private string _loadedVideoPath = "";
        private string _loadedVideoName = "";

        private const string PlaceholderA = "Home / Our Team";
        private const string PlaceholderB = "Away / Opponent";

        public MainWindow()
        {
            InitializeComponent();

            TeamAList.ItemsSource = _teamA;
            TeamBList.ItemsSource = _teamB;

            InitPlaceholders();
            InitFilterCombo();

            UpdateCurrentTagsText();
            UpdateTopLeftTitle();
        }

        // -------------------------
        // UI Init
        // -------------------------
        private void InitFilterCombo()
        {
            FilterCombo.Items.Clear();
            FilterCombo.Items.Add("All Clips");
            FilterCombo.Items.Add("Team A Only");
            FilterCombo.Items.Add("Team B Only");
            FilterCombo.SelectedIndex = 0;
            FilterCombo.SelectionChanged += (_, __) => ApplyFilter();
        }

        private void InitPlaceholders()
        {
            SetPlaceholder(TeamABox, PlaceholderA);
            SetPlaceholder(TeamBBox, PlaceholderB);
        }

        private void SetPlaceholder(TextBox tb, string placeholder)
        {
            tb.Text = placeholder;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(0xBD, 0xBD, 0xBD));
            tb.Tag = placeholder; // keep
        }

        private bool IsPlaceholder(TextBox tb)
        {
            var ph = tb.Tag as string ?? "";
            return tb.Text == ph;
        }

        // -------------------------
        // Team placeholder behavior
        // (クリックで全消し・未入力で戻る)
        // -------------------------
        private void TeamBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBox tb) return;

            // 未フォーカス時クリック：ここで「一発で全部選択→消して入力」に持っていく
            if (!tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
            }
        }

        private void TeamBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (IsPlaceholder(tb))
            {
                tb.Text = "";
                tb.Foreground = Brushes.White;
            }

            tb.SelectAll();
        }

        private void TeamBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                SetPlaceholder(tb, (tb == TeamABox) ? PlaceholderA : PlaceholderB);
            }
        }

        // -------------------------
        // Video
        // -------------------------
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.mov;*.mkv;*.avi|All Files|*.*"
            };
            if (ofd.ShowDialog() != true) return;

            _loadedVideoPath = ofd.FileName;
            _loadedVideoName = System.IO.Path.GetFileName(_loadedVideoPath);

            Player.Source = new Uri(_loadedVideoPath);
            Player.Position = TimeSpan.Zero;
            VideoHint.Visibility = Visibility.Collapsed;

            UpdateTopLeftTitle();
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            VideoHint.Visibility = Visibility.Collapsed;
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show($"Video load failed:\n{e.ErrorException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            VideoHint.Visibility = Visibility.Visible;
        }

        private void UpdateTopLeftTitle()
        {
            // Macっぽく「Video (16:9)  + ファイル名」を左上に出す
            if (string.IsNullOrWhiteSpace(_loadedVideoName))
                TopLeftTitle.Text = "Video (16:9)";
            else
                TopLeftTitle.Text = $"Video (16:9)   {_loadedVideoName}";
        }

        // -------------------------
        // Playback
        // -------------------------
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;

            if (_isPlaying)
            {
                Player.Pause();
                _isPlaying = false;
                PlayButton.Content = "▶";
            }
            else
            {
                Player.Play();
                _isPlaying = true;
                PlayButton.Content = "■";
            }
        }

        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        private void SetSpeed(double ratio)
        {
            // MediaElement has SpeedRatio
            Player.SpeedRatio = ratio;
        }

        private void Minus5_Click(object sender, RoutedEventArgs e) => SeekBy(-5);
        private void Minus1_Click(object sender, RoutedEventArgs e) => SeekBy(-1);
        private void Plus1_Click(object sender, RoutedEventArgs e) => SeekBy(1);
        private void Plus5_Click(object sender, RoutedEventArgs e) => SeekBy(5);

        private void SeekBy(double sec)
        {
            if (Player.Source == null) return;
            var t = Player.Position.TotalSeconds + sec;
            if (t < 0) t = 0;
            Player.Position = TimeSpan.FromSeconds(t);
        }

        // -------------------------
        // Clip Marking
        // -------------------------
        private void ClipStart_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            _clipStartSec = Player.Position.TotalSeconds;
            StartLabel.Text = $"START: {Clip.FormatTime(_clipStartSec)}";
        }

        private void ClipEnd_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            _clipEndSec = Player.Position.TotalSeconds;
            EndLabel.Text = $"END: {Clip.FormatTime(_clipEndSec)}";
        }

        private void SaveTeamA_Click(object sender, RoutedEventArgs e) => SaveClip("A");
        private void SaveTeamB_Click(object sender, RoutedEventArgs e) => SaveClip("B");

        private void SaveClip(string team)
        {
            if (Player.Source == null)
            {
                MessageBox.Show("Load a video first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_clipEndSec <= 0 || _clipStartSec <= 0 || Math.Abs(_clipEndSec - _clipStartSec) < 0.05)
            {
                MessageBox.Show("Set Clip START and Clip END first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var s = Math.Min(_clipStartSec, _clipEndSec);
            var e = Math.Max(_clipStartSec, _clipEndSec);

            var clip = new Clip
            {
                Team = team,
                StartSeconds = s,
                EndSeconds = e,
                Tags = string.Join(", ", _currentTags),
                ClipNote = "",
                SetPlay = ""
            };

            if (team == "A") _teamA.Add(clip);
            else _teamB.Add(clip);

            ApplyFilter();
        }

        // -------------------------
        // Clip selection + editing
        // -------------------------
        private void ClipGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg)
            {
                _selectedClip = dg.SelectedItem as Clip;
                if (_selectedClip == null) return;

                ClipNoteBox.Text = _selectedClip.ClipNote ?? "";
                SetPlayBox.Text = _selectedClip.SetPlay ?? "";

                // クリップのTagsを CurrentTags に同期
                _currentTags.Clear();
                foreach (var t in (_selectedClip.Tags ?? "").Split(',').Select(x => x.Trim()).Where(x => x.Length > 0))
                    _currentTags.Add(t);

                SyncToggleButtonsFromCurrentTags();
                UpdateCurrentTagsText();
            }
        }

        private void ClipNoteBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedClip == null) return;
            _selectedClip.ClipNote = ClipNoteBox.Text;
        }

        private void SetPlayBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedClip == null) return;
            _selectedClip.SetPlay = SetPlayBox.Text;
        }

        private void ClipGrid_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid dg) return;
            if (dg.SelectedItem is not Clip clip) return;
            if (Player.Source == null) return;

            Player.Position = TimeSpan.FromSeconds(clip.StartSeconds);
            Player.Play();
            _isPlaying = true;
            PlayButton.Content = "■";
        }

        // -------------------------
        // Tags
        // -------------------------
        private void TagToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ToggleButton tb) return;
            var tag = (tb.Content?.ToString() ?? "").Trim();
            if (tag.Length == 0) return;

            if (tb.IsChecked == true) _currentTags.Add(tag);
            else _currentTags.Remove(tag);

            UpdateCurrentTagsText();
            ApplyTagsToSelectedClipIfAny();
        }

        private void AddCustomTag_Click(object sender, RoutedEventArgs e)
        {
            var tag = (CustomTagBox.Text ?? "").Trim();
            if (tag.Length == 0) return;
            _currentTags.Add(tag);
            CustomTagBox.Text = "";
            UpdateCurrentTagsText();
            ApplyTagsToSelectedClipIfAny();
            SyncToggleButtonsFromCurrentTags(); // 既存トグルと一致するならONになる
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            _currentTags.Clear();
            UpdateCurrentTagsText();
            ApplyTagsToSelectedClipIfAny();
            SyncToggleButtonsFromCurrentTags();
        }

        private void ApplyTagsToSelectedClipIfAny()
        {
            if (_selectedClip == null) return;
            _selectedClip.Tags = string.Join(", ", _currentTags);
        }

        private void UpdateCurrentTagsText()
        {
            if (_currentTags.Count == 0) CurrentTagsText.Text = "(No tags selected)";
            else CurrentTagsText.Text = string.Join(", ", _currentTags);
        }

        private void SyncToggleButtonsFromCurrentTags()
        {
            // Window内のToggleButtonを走査してCurrentTagsと同期
            foreach (var tb in FindVisualChildren<ToggleButton>(this))
            {
                var tag = (tb.Content?.ToString() ?? "").Trim();
                if (tag.Length == 0) continue;
                tb.IsChecked = _currentTags.Contains(tag);
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) yield return t;
                foreach (var c in FindVisualChildren<T>(child)) yield return c;
            }
        }

        // -------------------------
        // CSV Import/Export
        // -------------------------
        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "CSV|*.csv|All Files|*.*" };
            if (ofd.ShowDialog() != true) return;

            var (csvVideoName, clips) = CsvService.Import(ofd.FileName);

            // VideoName mismatch警告は「表示だけ」して、取り込み自体は続行できるようにする
            if (!string.IsNullOrWhiteSpace(csvVideoName) &&
                !string.IsNullOrWhiteSpace(_loadedVideoName) &&
                !string.Equals(csvVideoName, _loadedVideoName, StringComparison.OrdinalIgnoreCase))
            {
                var r = MessageBox.Show(
                    $"CSVのVideoNameが現在読み込み中の動画と一致しません。\n\n" +
                    $"現在: {_loadedVideoName}\nCSV: {csvVideoName}\n\nこのままインポートを続行しますか？",
                    "VideoName mismatch",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (r != MessageBoxResult.Yes) return;
            }

            _teamA.Clear();
            _teamB.Clear();

            foreach (var c in clips)
            {
                if (c.Team == "B") _teamB.Add(c);
                else _teamA.Add(c);
            }

            ApplyFilter();
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog { Filter = "CSV|*.csv", FileName = "play_by_play.csv" };
            if (sfd.ShowDialog() != true) return;

            var all = _teamA.Concat(_teamB).ToList();
            CsvService.Export(sfd.FileName, string.IsNullOrWhiteSpace(_loadedVideoName) ? "" : _loadedVideoName, all);

            MessageBox.Show("Exported.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            // ここは “販売用”では本当は動画切り出し等が入るが、まずはCSV品質優先でExportCsvに寄せる
            ExportCsv_Click(sender, e);
        }

        // -------------------------
        // Filter
        // -------------------------
        private void ApplyFilter()
        {
            if (FilterCombo.SelectedItem == null) return;
            var mode = FilterCombo.SelectedItem.ToString() ?? "All Clips";

            if (mode == "Team A Only")
            {
                TeamAList.Visibility = Visibility.Visible;
                TeamBList.Visibility = Visibility.Collapsed;
            }
            else if (mode == "Team B Only")
            {
                TeamAList.Visibility = Visibility.Collapsed;
                TeamBList.Visibility = Visibility.Visible;
            }
            else
            {
                TeamAList.Visibility = Visibility.Visible;
                TeamBList.Visibility = Visibility.Visible;
            }

            ClipsTitle.Text = $"Clips (Total {_teamA.Count + _teamB.Count})";
        }
    }
}
