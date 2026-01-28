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
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

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


                Player.Stop();
                Player.Source = new Uri(dlg.FileName, UriKind.Absolute);

                // Reset speed to 1x (Mac-like)
                SetSpeed(1.0);

                // Preload a frame (Play->Pause) so NaturalDuration becomes ready sooner
                Player.Play();
                Player.Pause();
                VM.IsPlaying = false;

                VM.StatusText = "Video loaded.";
            }
            catch (Exception ex)
            {
                VM.StatusText = $"Load failed: {ex.Message}";
                MessageBox.Show(ex.Message, "Load Video Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    VM.DurationSeconds = Player.NaturalDuration.TimeSpan.TotalSeconds;
                    TimelineSlider.Maximum = VM.DurationSeconds;
                    VM.StatusText = "Ready";
                }

                // Apply any pending jump (request came before duration ready)
                if (_pendingJumpSeconds.HasValue)
                {
                    var sec = _pendingJumpSeconds.Value;
                    var autoPlay = _pendingAutoPlayAfterJump;
                    _pendingJumpSeconds = null;
                    _pendingAutoPlayAfterJump = false;
                    SeekToSeconds(sec, autoPlay);
                }
            }
            catch
            {
                // ignore
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
                TimelineSlider.Value = VM.CurrentSeconds;
            }

            VM.TimeDisplay = $"{FormatTime(Player.Position.TotalSeconds)} / {FormatTime(VM.DurationSeconds)}";
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingTimeline)
            {
                VM.CurrentSeconds = TimelineSlider.Value;
                VM.TimeDisplay = $"{FormatTime(VM.CurrentSeconds)} / {FormatTime(VM.DurationSeconds)}";
            }
        }

        private void TimelineSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingTimeline = true;
        }

        private void TimelineSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingTimeline = false;
            SeekToSeconds(VM.CurrentSeconds, autoPlay: VM.IsPlaying);
        }

        private void SeekToSeconds(double seconds, bool autoPlay)
        {
            if (Player.Source == null) return;

            if (!Player.NaturalDuration.HasTimeSpan)
            {
                _pendingJumpSeconds = seconds;
                _pendingAutoPlayAfterJump = autoPlay;
                return;
            }

            var max = Player.NaturalDuration.TimeSpan.TotalSeconds;
            if (double.IsNaN(max) || max <= 0) max = seconds;

            if (seconds < 0) seconds = 0;
            if (seconds > max) seconds = max;

            // Avoid crashy state transitions: seek only, then optionally Play.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Player.Position = TimeSpan.FromSeconds(seconds);
                    VM.CurrentSeconds = seconds;
                    TimelineSlider.Value = seconds;

                    if (autoPlay)
                    {
                        Player.Play();
                        VM.IsPlaying = true;
                    }
                }
                catch
                {
                    // If seek fails (codec/driver), ignore instead of crashing.
                }
            }), DispatcherPriority.Background);
        }

        private void SeekBy(double deltaSeconds)
        {
            if (Player.Source == null) return;
            SeekToSeconds(Player.Position.TotalSeconds + deltaSeconds, autoPlay: VM.IsPlaying);
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
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

        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        private void SetSpeed(double speed)
        {
            _currentSpeed = speed;
            HighlightSpeedButtons(speed);

            if (Player?.Source != null)
            {
                try { Player.SpeedRatio = speed; } catch { }
            }

            VM.StatusText = $"Speed: {speed:0.##}x";
        }

        private void HighlightSpeedButtons(double speed)
        {
            if (Speed025Button != null) Speed025Button.Background = SpeedNormalBrush;
            if (Speed05Button != null) Speed05Button.Background = SpeedNormalBrush;
            if (Speed1Button != null) Speed1Button.Background = SpeedNormalBrush;
            if (Speed2Button != null) Speed2Button.Background = SpeedNormalBrush;

            Button? selected = speed switch
            {
                0.25 => Speed025Button,
                0.5 => Speed05Button,
                1.0 => Speed1Button,
                2.0 => Speed2Button,
                _ => null
            };

            if (selected != null)
            {
                selected.Background = SpeedSelectedBrush;
                selected.BorderBrush = SpeedSelectedBrush;
            }
        }

        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => SeekBy(-5);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => SeekBy(-1);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => SeekBy(+1);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => SeekBy(+5);

        // ----------------------------
        // Clip
        // ----------------------------
        private void ClipStart_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            VM.ClipStartSeconds = Player.Position.TotalSeconds;
            VM.StatusText = $"Clip START = {FormatTime(VM.ClipStartSeconds)}";
        }

        private void ClipEnd_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            VM.ClipEndSeconds = Player.Position.TotalSeconds;
            VM.StatusText = $"Clip END = {FormatTime(VM.ClipEndSeconds)}";
        }

        private void SaveTeamA_Click(object sender, RoutedEventArgs e) => SaveClip("A");
        private void SaveTeamB_Click(object sender, RoutedEventArgs e) => SaveClip("B");

        private void SaveClip(string team)
        {
            if (Player.Source == null) return;

            var start = VM.ClipStartSeconds;
            var end = VM.ClipEndSeconds;

            if (end <= start)
            {
                VM.StatusText = "Clip range invalid (END must be after START).";
                return;
            }

            var tags = VM.GetSelectedTags().ToList();

            var item = new ClipRow
            {
                Team = team,
                Start = start,
                End = end,
                Tags = tags,
                Comment = ""
            };

            VM.AllClips.Add(item);

            VM.StatusText = $"Saved Team {team} clip ({FormatTime(start)} - {FormatTime(end)})";
            VM.UpdateHeadersAndCurrentTagsText();
        }

        private void ClipList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Jump -> auto play (requested)
            if (sender is ListView lv && lv.SelectedItem is ClipRow clip)
            {
                SeekToSeconds(clip.Start, autoPlay: true);
            }
        }

        private void ClipList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lv)
            {
                if (lv.SelectedItem is ClipRow c)
                {
                    VM.SelectedClip = c;
                }
            }
        }

        private void DeleteSelectedClip_Click(object sender, RoutedEventArgs e)
        {
            if (VM.SelectedClip == null) return;

            var target = VM.SelectedClip;
            VM.SelectedClip = null;

            if (VM.AllClips.Contains(target))
                VM.AllClips.Remove(target);

            VM.UpdateHeadersAndCurrentTagsText();
        }

        // ----------------------------
        // Tags
        // ----------------------------
        private void AddCustomTag_Click(object sender, RoutedEventArgs e)
        {
            var t = (VM.CustomTagInput ?? "").Trim();
            if (string.IsNullOrWhiteSpace(t)) return;

            if (!VM.OffenseTags.Any(x => string.Equals(x.Name, t, StringComparison.OrdinalIgnoreCase)))
            {
                VM.OffenseTags.Add(new TagToggleModel { Name = t, IsSelected = true });
            }
            else
            {
                var existing = VM.OffenseTags.First(x => string.Equals(x.Name, t, StringComparison.OrdinalIgnoreCase));
                existing.IsSelected = true;
            }

            VM.CustomTagInput = "";
            VM.UpdateHeadersAndCurrentTagsText();
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            foreach (var t in VM.OffenseTags) t.IsSelected = false;
            foreach (var t in VM.DefenseTags) t.IsSelected = false;
            VM.CustomTagInput = "";
            VM.UpdateHeadersAndCurrentTagsText();
        }

        // ----------------------------
        // CSV / Export Clips
        // ----------------------------
        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportClipsInternal(VM.AllClips.ToList());
        }

        private void ExportClips_Click(object sender, RoutedEventArgs e)
        {
            ExportClipsInternal(VM.AllClips.ToList());
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var list = VM.AllClips.ToList();
            ExportCsvInternal(list);
        }

        private void ExportCsvInternal(List<ClipRow> clips)
        {
            if (clips.Count == 0)
            {
                VM.StatusText = "No clips to export.";
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
                sb.AppendLine("team,start,end,tags,comment");
                foreach (var c in clips)
                {
                    var tags = string.Join("|", c.Tags ?? new List<string>());
                    sb.AppendLine($"{c.Team},{c.Start.ToString("0.###", CultureInfo.InvariantCulture)},{c.End.ToString("0.###", CultureInfo.InvariantCulture)},{EscapeCsv(tags)},{EscapeCsv(c.Comment ?? "")}");
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
                VM.StatusText = $"Exported: {Path.GetFileName(dlg.FileName)}";
            }
            catch (Exception ex)
            {
                VM.StatusText = "Export failed.";
                MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                Title = "Import CSV"
            };

            if (ofd.ShowDialog() != true) return;

            try
            {
                var lines = File.ReadAllLines(ofd.FileName);
                if (lines.Length < 2)
                {
                    MessageBox.Show("CSV is empty.");
                    return;
                }

                var header = SplitCsv(lines[0]).Select(h => (h ?? string.Empty).Trim()).ToList();
                var headerLower = header.Select(h => h.ToLowerInvariant()).ToList();

                int teamIdx = headerLower.IndexOf("team");
                int startIdx = headerLower.IndexOf("start");
                int endIdx = headerLower.IndexOf("end");
                int durationIdx = headerLower.IndexOf("duration");
                int tagsIdx = headerLower.IndexOf("tags");
                int commentIdx = headerLower.IndexOf("comment");

                if (teamIdx < 0 || startIdx < 0)
                {
                    MessageBox.Show("CSV format not recognized. Need at least 'team' and 'start' columns.");
                    return;
                }

                if (VM.AllClips.Any())
                {
                    var res = MessageBox.Show(
                        "Existing clips will be cleared before import. Continue?",
                        "Import CSV",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (res != MessageBoxResult.Yes) return;

                    VM.AllClips.Clear();
                }

                int imported = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var cols = SplitCsv(lines[i]);
                    if (cols.Count <= startIdx || cols.Count <= teamIdx) continue;

                    string teamRaw = (cols[teamIdx] ?? string.Empty).Trim();
                    string team = NormalizeTeamToAB(teamRaw);

                    double startSec = ParseTimeToSeconds(GetSafe(cols, startIdx));
                    double endSec = endIdx >= 0 ? ParseTimeToSeconds(GetSafe(cols, endIdx)) : 0;

                    if (endSec <= 0 && durationIdx >= 0)
                    {
                        var dur = ParseTimeToSeconds(GetSafe(cols, durationIdx));
                        if (dur > 0) endSec = startSec + dur;
                    }

                    if (endSec <= startSec) continue;

                    string tagsRaw = tagsIdx >= 0 ? GetSafe(cols, tagsIdx) : string.Empty;
                    var tags = ParseTags(tagsRaw);

                    string comment = commentIdx >= 0 ? GetSafe(cols, commentIdx) : string.Empty;

                    VM.AllClips.Add(new ClipRow
                    {
                        Team = team,
                        Start = startSec,
                        End = endSec,
                        Tags = tags,
                        Comment = comment
                    });
                    imported++;
                }

                VM.UpdateHeadersAndCurrentTagsText();
                VM.StatusText = $"Imported {imported} clips.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Import failed: " + ex.Message);
            }
        }

        // ----------------------------
        // Video export (ffmpeg)
        // ----------------------------
        private void ExportClipsInternal(List<ClipRow> clips)
        {
            if (clips == null || clips.Count == 0)
            {
                MessageBox.Show("No clips to export.", "Export Clips", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(VM.LoadedVideoPath) || !File.Exists(VM.LoadedVideoPath))
            {
                MessageBox.Show("Please load a video first.", "Export Clips", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var outFolder = ChooseFolder("Select export folder");
            if (string.IsNullOrWhiteSpace(outFolder)) return;

            var ffmpeg = ResolveFfmpegPath();
            if (ffmpeg == null)
            {
                MessageBox.Show(
                    "ffmpeg was not found. Please install ffmpeg and make sure it's available in PATH.\n\n" +
                    "Tip: Open cmd and run: ffmpeg -version",
                    "Export Clips", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var baseName = Path.GetFileNameWithoutExtension(VM.LoadedVideoPath);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var sessionDir = Path.Combine(outFolder, $"{SanitizeFileName(baseName)}_clips_{stamp}");
            Directory.CreateDirectory(sessionDir);

            int ok = 0;
            int fail = 0;
            VM.StatusText = "Exporting clips...";

            for (int i = 0; i < clips.Count; i++)
            {
                var c = clips[i];
                if (c.End <= c.Start) { fail++; continue; }

                var tags = string.Join("-", (c.Tags ?? new List<string>()).Take(5));
                var safeTags = SanitizeFileName(tags);
                var safeTeam = (c.Team == "B") ? "B" : "A";
                var file = $"{safeTeam}_{(i + 1):0000}_{FormatTimeForFile(c.Start)}_{FormatTimeForFile(c.End)}";
                if (!string.IsNullOrWhiteSpace(safeTags)) file += "_" + safeTags;
                file += ".mp4";

                var outPath = Path.Combine(sessionDir, file);
                var duration = Math.Max(0.01, c.End - c.Start);

                var args = BuildFfmpegArgs(
                    inputPath: VM.LoadedVideoPath,
                    startSeconds: c.Start,
                    durationSeconds: duration,
                    outputPath: outPath);

                VM.StatusText = $"Exporting {i + 1}/{clips.Count}...";

                var result = RunProcess(ffmpeg, args);
                if (result.exitCode == 0 && File.Exists(outPath)) ok++;
                else
                {
                    fail++;
                    Debug.WriteLine(result.stdErr);
                }
            }

            VM.StatusText = $"Export done. OK:{ok} / Fail:{fail}";
            MessageBox.Show($"Exported {ok} clip(s) to:\n{sessionDir}", "Export Clips", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string BuildFfmpegArgs(string inputPath, double startSeconds, double durationSeconds, string outputPath)
        {
            var ss = startSeconds.ToString("0.###", CultureInfo.InvariantCulture);
            var t = durationSeconds.ToString("0.###", CultureInfo.InvariantCulture);

            return $"-y -hide_banner -loglevel error -ss {ss} -i \"{inputPath}\" -t {t} -c:v libx264 -preset veryfast -crf 23 -c:a aac -b:a 160k \"{outputPath}\"";
        }

        private static (int exitCode, string stdOut, string stdErr) RunProcess(string exePath, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return (-1, "", "Process start failed.");
                var stdout = p.StandardOutput.ReadToEnd();
                var stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();
                return (p.ExitCode, stdout, stderr);
            }
            catch (Exception ex)
            {
                return (-1, "", ex.ToString());
            }
        }

        private static string? ResolveFfmpegPath()
        {
            var r = RunProcess("ffmpeg", "-version");
            if (r.exitCode == 0) return "ffmpeg";

            var candidates = new[]
            {
                @"C:\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
                @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe"
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c)) return c;
            }
            return null;
        }

        private static string? ChooseFolder(string title)
        {
            var sfd = new SaveFileDialog
            {
                Title = title,
                FileName = "export_here",
                DefaultExt = ".txt",
                Filter = "Folder (select location)|*.txt"
            };

            var ok = sfd.ShowDialog();
            if (ok == true)
            {
                var dir = Path.GetDirectoryName(sfd.FileName);
                return string.IsNullOrWhiteSpace(dir) ? null : dir;
            }
            return null;
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "";
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (var ch in name)
            {
                if (invalid.Contains(ch)) sb.Append('_');
                else sb.Append(ch);
            }
            return sb.ToString().Trim().Trim('_');
        }

        private static string FormatTimeForFile(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "0_00";
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return $"{ts.Hours}_{ts.Minutes:00}_{ts.Seconds:00}";
            return $"{ts.Minutes}_{ts.Seconds:00}";
        }

        private static string EscapeCsv(string s)
        {
            s ??= "";
            if (s.Contains(",") || s.Contains("\"") || s.Contains("\n"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
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

        private static string GetSafe(IReadOnlyList<string> cols, int index)
        {
            if (index < 0 || index >= cols.Count) return string.Empty;
            return cols[index] ?? string.Empty;
        }

        private static string NormalizeTeamToAB(string team)
        {
            var t = (team ?? string.Empty).Trim();
            if (t.Length == 0) return "A";

            var lower = t.ToLowerInvariant();
            if (lower == "a" || lower == "team a" || lower.Contains("home") || lower.Contains("our")) return "A";
            if (lower == "b" || lower == "team b" || lower.Contains("away") || lower.Contains("opponent")) return "B";

            if (lower.Contains("b") && !lower.Contains("a")) return "B";
            return "A";
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

        private static List<string> ParseTags(string tagsRaw)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(tagsRaw)) return result;

            string raw = tagsRaw.Trim();
            char[] seps = new[] { ';', '|' };
            var tokens = raw.Split(seps, StringSplitOptions.RemoveEmptyEntries);
            foreach (var t in tokens)
            {
                var tag = t.Trim();
                if (tag.Length == 0) continue;
                result.Add(tag);
            }
            return result;
        }

        private static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "--:--";
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }
    }

    // ============================
    // ViewModel / Models (self-contained for this project)
    // ============================

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _teamAName = "Home / Our Team";
        private string _teamBName = "Away / Opponent";
        private string _loadedVideoName = "";
        private string _loadedVideoPath = "";
        private string _statusText = "";
        private bool _isPlaying = false;

        private double _durationSeconds = 0;
        private double _currentSeconds = 0;

        private double _clipStartSeconds = 0;
        private double _clipEndSeconds = 0;

        private string _timeDisplay = "--:-- / --:--";
        private string _customTagInput = "";
        private string _currentTagsText = "(No tags selected)";
        private string _clipsHeader = "Clips (Total 0)";
        private ClipRow? _selectedClip = null;

        public ObservableCollection<string> ClipFilters { get; } = new ObservableCollection<string>(new[] { "All Clips", "Team A", "Team B" });

        private string _selectedClipFilter = "All Clips";
        public string SelectedClipFilter
        {
            get => _selectedClipFilter;
            set
            {
                if (_selectedClipFilter == value) return;
                _selectedClipFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TeamAView));
                OnPropertyChanged(nameof(TeamBView));
                UpdateHeadersAndCurrentTagsText();
            }
        }

        public ObservableCollection<ClipRow> AllClips { get; } = new ObservableCollection<ClipRow>();

        private readonly ICollectionView _teamAView;
        private readonly ICollectionView _teamBView;

        public ICollectionView TeamAView => _teamAView;
        public ICollectionView TeamBView => _teamBView;

        public ObservableCollection<TagToggleModel> OffenseTags { get; } = new ObservableCollection<TagToggleModel>(
            new[]
            {
                "Transition","Set","PnR","BLOB","SLOB","vs M/M","vs Zone","2nd Attack","3rd Attack more"
            }.Select(x => new TagToggleModel { Name = x })
        );

        public ObservableCollection<TagToggleModel> DefenseTags { get; } = new ObservableCollection<TagToggleModel>(
            new[]
            {
                "M/M","Zone","Rebound","Steal"
            }.Select(x => new TagToggleModel { Name = x })
        );

        public MainWindowViewModel()
        {
            foreach (var t in OffenseTags) t.PropertyChanged += (_, __) => UpdateHeadersAndCurrentTagsText();
            foreach (var t in DefenseTags) t.PropertyChanged += (_, __) => UpdateHeadersAndCurrentTagsText();

            AllClips.CollectionChanged += AllClips_CollectionChanged;

            _teamAView = CollectionViewSource.GetDefaultView(AllClips);
            _teamBView = new ListCollectionView(AllClips);

            _teamAView.Filter = o =>
            {
                if (o is not ClipRow c) return false;
                if (SelectedClipFilter == "Team B") return false;
                return string.Equals(c.Team, "A", StringComparison.OrdinalIgnoreCase);
            };

            _teamBView.Filter = o =>
            {
                if (o is not ClipRow c) return false;
                if (SelectedClipFilter == "Team A") return false;
                return string.Equals(c.Team, "B", StringComparison.OrdinalIgnoreCase);
            };

            UpdateHeadersAndCurrentTagsText();
        }

        private void AllClips_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            _teamAView.Refresh();
            _teamBView.Refresh();
            UpdateHeadersAndCurrentTagsText();
            OnPropertyChanged(nameof(HasSelectedClip));
        }

        public string TeamAName { get => _teamAName; set { _teamAName = value; OnPropertyChanged(); } }
        public string TeamBName { get => _teamBName; set { _teamBName = value; OnPropertyChanged(); } }

        public string LoadedVideoName { get => _loadedVideoName; set { _loadedVideoName = value; OnPropertyChanged(); } }
        public string LoadedVideoPath { get => _loadedVideoPath; set { _loadedVideoPath = value; OnPropertyChanged(); } }

        public string StatusText { get => _statusText; set { _statusText = value; OnPropertyChanged(); } }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlayPauseLabel));
                OnPropertyChanged(nameof(PlayPauseIcon));
            }
        }

        public string PlayPauseLabel => IsPlaying ? "Pause" : "Play";
        public string PlayPauseIcon => IsPlaying ? "\uE769" : "\uE768"; // Segoe MDL2 Assets

        public double DurationSeconds { get => _durationSeconds; set { _durationSeconds = value; OnPropertyChanged(); } }
        public double CurrentSeconds { get => _currentSeconds; set { _currentSeconds = value; OnPropertyChanged(); } }

        public double ClipStartSeconds { get => _clipStartSeconds; set { _clipStartSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClipStartText)); } }
        public double ClipEndSeconds { get => _clipEndSeconds; set { _clipEndSeconds = value; OnPropertyChanged(); OnPropertyChanged(nameof(ClipEndText)); } }

        public string ClipStartText => $"START: {FormatTime(ClipStartSeconds)}";
        public string ClipEndText => $"END: {FormatTime(ClipEndSeconds)}";

        public string TimeDisplay { get => _timeDisplay; set { _timeDisplay = value; OnPropertyChanged(); } }

        public string CustomTagInput { get => _customTagInput; set { _customTagInput = value; OnPropertyChanged(); } }

        public string CurrentTagsText { get => _currentTagsText; set { _currentTagsText = value; OnPropertyChanged(); } }

        public string ClipsHeader { get => _clipsHeader; set { _clipsHeader = value; OnPropertyChanged(); } }

        public ClipRow? SelectedClip
        {
            get => _selectedClip;
            set
            {
                _selectedClip = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedClip));
            }
        }

        public bool HasSelectedClip => SelectedClip != null;

        public IEnumerable<string> GetSelectedTags()
        {
            return OffenseTags.Where(x => x.IsSelected).Select(x => x.Name)
                .Concat(DefenseTags.Where(x => x.IsSelected).Select(x => x.Name));
        }

        public void UpdateHeadersAndCurrentTagsText()
        {
            ClipsHeader = $"Clips (Total {AllClips.Count})";

            var tags = GetSelectedTags().ToList();
            CurrentTagsText = tags.Count == 0 ? "(No tags selected)" : string.Join(", ", tags);

            _teamAView.Refresh();
            _teamBView.Refresh();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private static string FormatTime(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "--:--";
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }
    }

    public class TagToggleModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _name = "";
        private bool _isSelected = false;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(); } }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ClipRow : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private string _team = "A";
        private double _start;
        private double _end;
        private List<string> _tags = new();
        private string _comment = "";

        public string Team
        {
            get => _team;
            set { _team = value; OnPropertyChanged(); }
        }

        public double Start
        {
            get => _start;
            set { _start = value; OnPropertyChanged(); OnPropertyChanged(nameof(StartText)); }
        }

        public double End
        {
            get => _end;
            set { _end = value; OnPropertyChanged(); OnPropertyChanged(nameof(EndText)); }
        }

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

        public string StartText => FormatTime(Start);
        public string EndText => FormatTime(End);
        public string TagsText => Tags == null || Tags.Count == 0 ? "" : string.Join(", ", Tags);

        private static string FormatTime(double seconds)
        {
            var ts = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (ts.Hours > 0) return ts.ToString(@"h\:mm\:ss");
            return ts.ToString(@"m\:ss");
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
