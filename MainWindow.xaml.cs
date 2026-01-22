using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private bool _isDraggingTimeline = false;

        // Speed button visuals
        private static readonly SolidColorBrush SpeedNormalBrush = new((Color)ColorConverter.ConvertFromString("#2A2A2A"));
        private static readonly SolidColorBrush SpeedSelectedBrush = new((Color)ColorConverter.ConvertFromString("#0A84FF"));
        private double _currentSpeed = 1.0;

        public MainWindowViewModel VM { get; } = new MainWindowViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = VM;

            // UIの初期状態：Speed は 1x を選択状態にしておく（動画未ロードでも表示だけは合わせる）
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
                // ロードしたら Speed は 1x に戻す（Mac版の挙動に合わせる）
                SetSpeed(1.0);
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
            if (Player.NaturalDuration.HasTimeSpan)
            {
                VM.DurationSeconds = Player.NaturalDuration.TimeSpan.TotalSeconds;
                TimelineSlider.Maximum = VM.DurationSeconds;
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
            }

            VM.TimeDisplay = $"{FormatTime(Player.Position.TotalSeconds)} / {FormatTime(VM.DurationSeconds)}";
        }

        private void TimelineSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDraggingTimeline = true;
        }

        private void TimelineSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDraggingTimeline = false;
            SeekTo(VM.CurrentSeconds);
        }

        private void SeekTo(double seconds)
        {
            if (Player.Source == null) return;

            seconds = Math.Max(0, Math.Min(seconds, VM.DurationSeconds));
            Player.Position = TimeSpan.FromSeconds(seconds);
        }

        private void SeekBy(double deltaSeconds)
        {
            if (Player.Source == null) return;
            SeekTo(Player.Position.TotalSeconds + deltaSeconds);
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

            // 動画未ロードでも選択表示は変える
            if (Player?.Source != null)
            {
                Player.SpeedRatio = speed;
            }

            VM.StatusText = $"Speed: {speed:0.##}x";
        }

        private void HighlightSpeedButtons(double speed)
        {
            // reset
            if (Speed025Button != null) Speed025Button.Background = SpeedNormalBrush;
            if (Speed05Button  != null) Speed05Button.Background  = SpeedNormalBrush;
            if (Speed1Button   != null) Speed1Button.Background   = SpeedNormalBrush;
            if (Speed2Button   != null) Speed2Button.Background   = SpeedNormalBrush;

            // select
            var selected = speed switch
            {
                0.25 => Speed025Button,
                0.5  => Speed05Button,
                1.0  => Speed1Button,
                2.0  => Speed2Button,
                _    => null
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

            var tags = VM.GetSelectedTags();
            var item = new ClipRow
            {
                Team = team,
                Start = start,
                End = end,
                Tags = tags.ToList()
            };

            VM.AllClips.Add(item);
            if (team == "A") VM.TeamAClips.Add(item);
            else VM.TeamBClips.Add(item);

            VM.StatusText = $"Saved Team {team} clip ({FormatTime(start)} - {FormatTime(end)})";
            VM.UpdateHeadersAndCurrentTagsText();
        }

        // ----------------------------
        // Clips (next step)
        // ----------------------------
        // リストのクリップをダブルクリック → そのStartへジャンプして再生
        private void ClipList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListView lv) return;
            if (lv.SelectedItem is not ClipRow row) return;
            if (Player?.Source == null) return;

            Player.Position = TimeSpan.FromSeconds(row.Start);
            Player.Play();
            VM.IsPlaying = true;
            VM.StatusText = $"Jumped to {FormatTime(row.Start)}";
        }

        // ----------------------------
        // Tags
        // ----------------------------
        private void AddCustomTag_Click(object sender, RoutedEventArgs e)
        {
            var t = (VM.CustomTagInput ?? "").Trim();
            if (string.IsNullOrWhiteSpace(t)) return;

            // custom tagは offense側に追加（Macに寄せて「増やせる」挙動）
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

        /// <summary>
        /// Export ALL clips as video files (ffmpeg required).
        /// NOTE: XAML may call either ExportAll_Click or ExportClips_Click depending on version.
        /// </summary>
        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportClipsInternal(VM.AllClips.ToList());
        }

        /// <summary>
        /// Alias for older XAML wiring.
        /// </summary>
        private void ExportClips_Click(object sender, RoutedEventArgs e)
        {
            ExportClipsInternal(VM.AllClips.ToList());
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            // フィルタ反映版（表示上の想定に合わせる）
            var list = VM.GetFilteredClips().ToList();
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
                sb.AppendLine("team,start,end,tags");
                foreach (var c in clips)
                {
                    var tags = string.Join("|", c.Tags ?? new List<string>());
                    sb.AppendLine($"{c.Team},{c.Start.ToString("0.###", CultureInfo.InvariantCulture)},{c.End.ToString("0.###", CultureInfo.InvariantCulture)},{EscapeCsv(tags)}");
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
                    MessageBox.Show("CSV is empty.", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // --- header解析（列名揺れ吸収：空白除去＋小文字化） ---
                var headerRaw = SplitCsv(lines[0]).Select(h => (h ?? string.Empty).Trim()).ToList();
                var headerKey = headerRaw.Select(NormalizeHeaderKey).ToList();

                int teamIdx     = FindColumnIndex(headerKey, "team", "team(home/away)", "team(homeaway)", "team(home-away)");
                int startIdx    = FindColumnIndex(headerKey, "start", "starttime", "start_time");
                int endIdx      = FindColumnIndex(headerKey, "end", "endtime", "end_time");
                int durationIdx = FindColumnIndex(headerKey, "duration", "dur");
                int tagsIdx     = FindColumnIndex(headerKey, "tags", "tag");

                if (teamIdx < 0 || startIdx < 0)
                {
                    MessageBox.Show(
                        "CSV format not recognized. Need at least 'Team' and 'Start' columns.",
                        "Import CSV",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                // Clear existing clips? (recommended)
                if (VM.AllClips.Any() || VM.TeamAClips.Any() || VM.TeamBClips.Any())
                {
                    var res = MessageBox.Show(
                        "Existing clips will be cleared before import. Continue?",
                        "Import CSV",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (res != MessageBoxResult.Yes) return;

                    VM.AllClips.Clear();
                    VM.TeamAClips.Clear();
                    VM.TeamBClips.Clear();
                }

                int imported = 0;
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;

                    var cols = SplitCsv(lines[i]);
                    if (cols.Count <= Math.Max(teamIdx, startIdx)) continue;

                    string teamRaw = GetSafe(cols, teamIdx).Trim();
                    string team = NormalizeTeamToAB(teamRaw); // Home->A / Away->B

                    // Start / End / Duration
                    double startSec = ParseTimeToSeconds(GetSafe(cols, startIdx));
                    if (startSec <= 0) continue;

                    double endSec = 0;
                    if (endIdx >= 0) endSec = ParseTimeToSeconds(GetSafe(cols, endIdx));

                    // Endが空ならDurationで補完（Mac版CSV）
                    if (endSec <= 0 && durationIdx >= 0)
                    {
                        var dur = ParseTimeToSeconds(GetSafe(cols, durationIdx));
                        if (dur > 0) endSec = startSec + dur;
                    }

                    if (endSec <= startSec) continue;

                    string tagsRaw = tagsIdx >= 0 ? GetSafe(cols, tagsIdx) : string.Empty;
                    var tags = ParseTagsFlexible(tagsRaw);

                    var row = new ClipRow
                    {
                        Team = team,
                        Start = startSec,
                        End = endSec,
                        Tags = tags
                    };

                    VM.AllClips.Add(row);
                    if (team == "A") VM.TeamAClips.Add(row);
                    else VM.TeamBClips.Add(row);

                    imported++;
                }

                VM.UpdateHeadersAndCurrentTagsText();
                VM.StatusText = $"Imported {imported} clips.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Import failed: " + ex.Message, "Import CSV", MessageBoxButton.OK, MessageBoxImage.Error);
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

            // Choose output folder
            var outFolder = ChooseFolder("Select export folder");
            if (string.IsNullOrWhiteSpace(outFolder)) return;

            // Ensure ffmpeg exists
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
                    // Keep going, but leave a hint
                    Debug.WriteLine(result.stdErr);
                }
            }

            VM.StatusText = $"Export done. OK:{ok} / Fail:{fail}";
            MessageBox.Show($"Exported {ok} clip(s) to:\n{sessionDir}", "Export Clips", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string BuildFfmpegArgs(string inputPath, double startSeconds, double durationSeconds, string outputPath)
        {
            // Re-encode for reliable accurate cuts.
            // -ss before -i is faster; acceptable for analysis clips.
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
            // 1) PATH
            var r = RunProcess("ffmpeg", "-version");
            if (r.exitCode == 0) return "ffmpeg";

            // 2) Common install locations (optional)
            var candidates = new[]
            {
                @"C:\\ffmpeg\\bin\\ffmpeg.exe",
                @"C:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe",
                @"C:\\Program Files (x86)\\ffmpeg\\bin\\ffmpeg.exe"
            };
            foreach (var c in candidates)
            {
                if (File.Exists(c)) return c;
            }
            return null;
        }

        private static string? ChooseFolder(string title)
        {
            // WPF doesn't ship with a folder picker.
            // Use a SaveFileDialog as a lightweight workaround:
            // user picks a dummy file name, and we use its directory.
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
            // hh_mm_ss
            if (ts.Hours > 0) return $"{ts.Hours}_{ts.Minutes:00}_{ts.Seconds:00}";
            return $"{ts.Minutes}_{ts.Seconds:00}";
        }

        private static string EscapeCsv(string s)
        {
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
            if (lower == "a" || lower == "team a") return "A";
            if (lower == "b" || lower == "team b") return "B";

            if (lower.Contains("home") || lower.Contains("our")) return "A";
            if (lower.Contains("away") || lower.Contains("opponent")) return "B";

            // Fallback: any string that contains 'b' but not 'a'
            if (lower.Contains("b") && !lower.Contains("a")) return "B";
            return "A";
        }

        private static double ParseTimeToSeconds(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            s = s.Trim();

            // Numeric seconds
            if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var secNum))
                return secNum;
            if (double.TryParse(s, out secNum))
                return secNum;

            // Formats: mm:ss, mm:ss.fff, hh:mm:ss, hh:mm:ss.fff
            var parts = s.Split(':');
            try
            {
                if (parts.Length == 2)
                {
                    int minutes = int.Parse(parts[0]);
                    double seconds = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    return minutes * 60 + seconds;
                }
                if (parts.Length == 3)
                {
                    int hours = int.Parse(parts[0]);
                    int minutes = int.Parse(parts[1]);
                    double seconds = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
                    return hours * 3600 + minutes * 60 + seconds;
                }
            }
            catch
            {
                // ignore
            }
            return 0;
        }


        private static string NormalizeHeaderKey(string s)
        {
            var t = (s ?? string.Empty).Trim().ToLowerInvariant();
            t = Regex.Replace(t, @"\s+", "");
            return t;
        }

        private static int FindColumnIndex(List<string> headerKey, params string[] keys)
        {
            foreach (var k in keys)
            {
                var kk = NormalizeHeaderKey(k);
                var idx = headerKey.IndexOf(kk);
                if (idx >= 0) return idx;
            }
            return -1;
        }

        private static List<string> ParseTagsFlexible(string tagsRaw)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(tagsRaw)) return result;

            var raw = tagsRaw.Trim();

            // Treat "///" as a separator too
            raw = raw.Replace("///", "|");

            // Unify separators ; | ,
            raw = raw.Replace(';', '|').Replace(',', '|');

            foreach (var t in raw.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var tag = t.Trim();
                if (tag.Length == 0) continue;
                result.Add(tag);
            }
            return result;
        }

        private static List<string> ParseTags(string tagsRaw)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(tagsRaw)) return result;

            string raw = tagsRaw.Trim();

            char[] seps = new[] { ';', '|'};
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
    // ViewModel / Models (self-contained)
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

        public ObservableCollection<string> ClipFilters { get; } = new ObservableCollection<string>(new[] { "All Clips", "Team A", "Team B" });

        private string _selectedClipFilter = "All Clips";
        public string SelectedClipFilter
        {
            get => _selectedClipFilter;
            set { _selectedClipFilter = value; OnPropertyChanged(); UpdateHeadersAndCurrentTagsText(); }
        }

        public ObservableCollection<ClipRow> AllClips { get; } = new ObservableCollection<ClipRow>();
        public ObservableCollection<ClipRow> TeamAClips { get; } = new ObservableCollection<ClipRow>();
        public ObservableCollection<ClipRow> TeamBClips { get; } = new ObservableCollection<ClipRow>();

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

            AllClips.CollectionChanged += (_, __) => UpdateHeadersAndCurrentTagsText();
            TeamAClips.CollectionChanged += (_, __) => UpdateHeadersAndCurrentTagsText();
            TeamBClips.CollectionChanged += (_, __) => UpdateHeadersAndCurrentTagsText();

            UpdateHeadersAndCurrentTagsText();
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

        // Segoe MDL2 Assets glyphs
        // Play:  E768   Pause: E769
        public string PlayPauseIcon => IsPlaying ? "\uE769" : "\uE768";

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

        public IEnumerable<string> GetSelectedTags()
        {
            return OffenseTags.Where(x => x.IsSelected).Select(x => x.Name)
                .Concat(DefenseTags.Where(x => x.IsSelected).Select(x => x.Name));
        }

        public IEnumerable<ClipRow> GetFilteredClips()
        {
            return SelectedClipFilter switch
            {
                "Team A" => TeamAClips,
                "Team B" => TeamBClips,
                _ => AllClips
            };
        }

        public void UpdateHeadersAndCurrentTagsText()
        {
            ClipsHeader = $"Clips (Total {AllClips.Count})";

            var tags = GetSelectedTags().ToList();
            CurrentTagsText = tags.Count == 0 ? "(No tags selected)" : string.Join(", ", tags);
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
