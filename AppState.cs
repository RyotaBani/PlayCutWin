using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public sealed class AppState : INotifyPropertyChanged
    {
        public static AppState Instance { get; } = new AppState();

        private AppState()
        {
            TeamAName = "Home / Our Team";
            TeamBName = "Away / Opponent";

            StatusMessage = "Ready";

            // Mac版っぽいプリセット（まずは固定でOK）
            OffensePresetTags = new ObservableCollection<string>(new[]
            {
                "Transition", "Set", "PnR",
                "BLOB", "SLOB", "vs M/M",
                "vs Zone", "2nd Attack", "3rd Attack more"
            });

            DefensePresetTags = new ObservableCollection<string>(new[]
            {
                "M/M", "Zone", "Rebound", "Steal"
            });

            PlaybackSpeed = 1.0;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }

        // =========
        // Models
        // =========
        public sealed class VideoItem
        {
            public string Path { get; set; } = "";
            public string Name => System.IO.Path.GetFileName(Path);
            public override string ToString() => Name;
        }

        public sealed class TagEntry
        {
            public string Tag { get; set; } = "";
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            public string TimeText => CreatedAt.ToString("HH:mm:ss");
            public override string ToString() => Tag;
        }

        public sealed class ClipItem
        {
            public string Team { get; set; } = "A"; // "A" or "B"
            public double Start { get; set; }
            public double End { get; set; }
            public string TagsText { get; set; } = "";

            public string Display => $"{FormatTime(Start)} - {FormatTime(End)}  {TagsText}";

            private static string FormatTime(double sec)
            {
                if (sec < 0) sec = 0;
                var ts = TimeSpan.FromSeconds(sec);
                return ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
            }
        }

        // =========
        // Core State
        // =========
        private string _teamAName = "";
        public string TeamAName { get => _teamAName; set => Set(ref _teamAName, value); }

        private string _teamBName = "";
        public string TeamBName { get => _teamBName; set => Set(ref _teamBName, value); }

        private string _statusMessage = "";
        public string StatusMessage { get => _statusMessage; set => Set(ref _statusMessage, value); }

        // Presets (Mac寄せ)
        public ObservableCollection<string> OffensePresetTags { get; }
        public ObservableCollection<string> DefensePresetTags { get; }

        // Videos
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new();

        private VideoItem? _selectedVideo;
        public VideoItem? SelectedVideo
        {
            get => _selectedVideo;
            set
            {
                if (Set(ref _selectedVideo, value))
                {
                    OnPropertyChanged(nameof(SelectedVideoPath));
                    OnPropertyChanged(nameof(SelectedVideoName));
                    OnPropertyChanged(nameof(ClipsSubHeaderText));
                }
            }
        }

        public string SelectedVideoPath => SelectedVideo?.Path ?? "";
        public string SelectedVideoName => SelectedVideo?.Name ?? "Video (16:9)";

        // Clips panel sub header (Macは "No video loaded" 表記)
        public string ClipsSubHeaderText => SelectedVideo == null ? "No video loaded" : SelectedVideoName;

        // Playback
        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (Set(ref _isPlaying, value))
                    OnPropertyChanged(nameof(PlayPauseGlyph));
            }
        }

        // ▶ / ⏸
        public string PlayPauseGlyph => IsPlaying ? "⏸" : "▶";

        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set
            {
                if (Set(ref _playbackSeconds, value))
                {
                    OnPropertyChanged(nameof(PlaybackPositionText));
                    OnPropertyChanged(nameof(TimeText));
                }
            }
        }

        private double _playbackDuration;
        public double PlaybackDuration
        {
            get => _playbackDuration;
            set
            {
                if (Set(ref _playbackDuration, value))
                {
                    OnPropertyChanged(nameof(PlaybackDurationText));
                    OnPropertyChanged(nameof(PlaybackDurationTextLong));
                    OnPropertyChanged(nameof(TimeText));
                }
            }
        }

        private double _playbackSpeed;
        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set => Set(ref _playbackSpeed, value);
        }

        public string PlaybackPositionText => FormatClockShort(PlaybackSeconds);
        public string PlaybackDurationText => FormatClockShort(PlaybackDuration);
        public string PlaybackDurationTextLong => FormatClockLong(PlaybackDuration);

        // Windows版で欲しい表示: 00:21 / 01:29:26
        public string TimeText => $"{FormatClockShort(PlaybackSeconds)} / {FormatClockLong(PlaybackDuration)}";

        private static string FormatClockShort(double sec)
        {
            if (sec < 0) sec = 0;
            var ts = TimeSpan.FromSeconds(sec);
            return ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        }

        private static string FormatClockLong(double sec)
        {
            if (sec < 0) sec = 0;
            var ts = TimeSpan.FromSeconds(sec);
            return ts.ToString(@"hh\:mm\:ss");
        }

        // Clip Range
        private double _clipStart;
        public double ClipStart
        {
            get => _clipStart;
            set
            {
                if (Set(ref _clipStart, value))
                    OnPropertyChanged(nameof(ClipStartText));
            }
        }

        private double _clipEnd;
        public double ClipEnd
        {
            get => _clipEnd;
            set
            {
                if (Set(ref _clipEnd, value))
                    OnPropertyChanged(nameof(ClipEndText));
            }
        }

        public string ClipStartText => $"START: {FormatClockShort(ClipStart)}";
        public string ClipEndText => $"END:   {FormatClockShort(ClipEnd)}";

        public void ResetRange()
        {
            ClipStart = 0;
            ClipEnd = 0;
            StatusMessage = "Range reset";
        }

        // Tags / Clips
        public ObservableCollection<TagEntry> Tags { get; } = new();
        public ObservableCollection<ClipItem> Clips { get; } = new();

        public IEnumerable<string> EnumerateAllTags()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in Tags)
            {
                var s = (t.Tag ?? "").Trim();
                if (s.Length > 0) set.Add(s);
            }

            foreach (var c in Clips)
            {
                var raw = (c.TagsText ?? "").Replace(";", ",");
                foreach (var part in raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var s = part.Trim();
                    if (s.Length > 0) set.Add(s);
                }
            }

            return set.OrderBy(x => x);
        }

        public void AddTagToSelected(string tag)
        {
            tag = (tag ?? "").Trim();
            if (tag.Length == 0) return;

            Tags.Add(new TagEntry { Tag = tag, CreatedAt = DateTime.Now });
            StatusMessage = $"Tag added: {tag}";
        }

        public void RemoveSelectedTag(TagEntry? tag)
        {
            if (tag == null) return;
            Tags.Remove(tag);
            StatusMessage = $"Tag removed: {tag.Tag}";
        }

        public void ClearTagsForSelected()
        {
            Tags.Clear();
            StatusMessage = "Tags cleared";
        }

        // Save Team A / B (Macの緑ボタン相当：まずは保存だけ)
        public void SaveClip(string team)
        {
            if (string.IsNullOrWhiteSpace(team)) team = "A";
            team = team.Trim().ToUpperInvariant();
            if (team != "A" && team != "B") team = "A";

            var s = ClipStart;
            var e = ClipEnd;

            // 順序修正
            if (e > 0 && s > e)
            {
                var tmp = s;
                s = e;
                e = tmp;
            }

            if (e <= 0 || Math.Abs(e - s) < 0.01)
            {
                StatusMessage = "Clip range is not set (need START & END)";
                return;
            }

            var tagsText = string.Join("; ", Tags.Select(t => t.Tag).Where(x => !string.IsNullOrWhiteSpace(x)));

            Clips.Add(new ClipItem
            {
                Team = team,
                Start = s,
                End = e,
                TagsText = tagsText
            });

            StatusMessage = $"Saved clip to Team {team}: {FormatClockShort(s)} - {FormatClockShort(e)}";
        }

        // =========
        // Videos
        // =========
        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (ImportedVideos.Any(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase)))
            {
                // 既にあるならそれを選ぶ
                SelectedVideo = ImportedVideos.First(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase));
                StatusMessage = $"Selected: {SelectedVideo.Name}";
                return;
            }

            var item = new VideoItem { Path = path };
            ImportedVideos.Add(item);
            SelectedVideo = item;

            StatusMessage = $"Imported: {System.IO.Path.GetFileName(path)}";
        }

        public void SetSelected(VideoItem item)
        {
            SelectedVideo = item;
            StatusMessage = $"Selected: {item.Name}";
        }

        // =========
        // CSV（暫定。後でMac版フォーマットへ揃える）
        // =========
        public void ImportCsvFromDialog()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import CSV",
                Filter = "CSV file|*.csv|All files|*.*",
                Multiselect = false
            };
            if (dlg.ShowDialog() != true) return;

            ImportCsv(dlg.FileName);
        }

        public void ExportCsvToDialog()
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export CSV",
                Filter = "CSV file|*.csv|All files|*.*",
                FileName = $"tags_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dlg.ShowDialog() != true) return;

            ExportCsv(dlg.FileName);
        }

        // 形式（暫定）:
        // TAG,tag,CreatedAt
        // CLIP,Team,Start,End,Tags
        public void ImportCsv(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                int addedTags = 0, addedClips = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cols = line.Split(',');

                    if (cols.Length == 0) continue;

                    var kind = cols[0].Trim();

                    if (string.Equals(kind, "TAG", StringComparison.OrdinalIgnoreCase))
                    {
                        if (cols.Length >= 2)
                        {
                            var tag = cols[1].Trim();
                            if (tag.Length > 0)
                            {
                                Tags.Add(new TagEntry { Tag = tag, CreatedAt = DateTime.Now });
                                addedTags++;
                            }
                        }
                    }
                    else if (string.Equals(kind, "CLIP", StringComparison.OrdinalIgnoreCase))
                    {
                        if (cols.Length >= 5)
                        {
                            var team = cols[1].Trim();
                            var start = ParseDouble(cols[2]);
                            var end = ParseDouble(cols[3]);
                            var tags = string.Join(",", cols.Skip(4)).Trim();

                            Clips.Add(new ClipItem
                            {
                                Team = string.IsNullOrWhiteSpace(team) ? "A" : team,
                                Start = start,
                                End = end,
                                TagsText = tags
                            });
                            addedClips++;
                        }
                    }
                    else
                    {
                        // 1列だけのCSVはタグ列として読む
                        var tag = cols[0].Trim();
                        if (tag.Length > 0)
                        {
                            Tags.Add(new TagEntry { Tag = tag, CreatedAt = DateTime.Now });
                            addedTags++;
                        }
                    }
                }

                StatusMessage = $"Imported CSV: tags={addedTags}, clips={addedClips}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"CSV import failed: {ex.Message}";
            }
        }

        public void ExportCsv(string filePath)
        {
            try
            {
                var outLines = new List<string>();

                foreach (var t in Tags)
                    outLines.Add($"TAG,{EscapeCsv(t.Tag)},{t.CreatedAt:o}");

                foreach (var c in Clips)
                    outLines.Add($"CLIP,{EscapeCsv(c.Team)},{c.Start.ToString(CultureInfo.InvariantCulture)},{c.End.ToString(CultureInfo.InvariantCulture)},{EscapeCsv(c.TagsText)}");

                File.WriteAllLines(filePath, outLines);
                StatusMessage = $"Exported CSV: {System.IO.Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"CSV export failed: {ex.Message}";
            }
        }

        private static double ParseDouble(string s)
        {
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            if (double.TryParse(s, out v)) return v;
            return 0;
        }

        private static string EscapeCsv(string s)
        {
            s ??= "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }
    }
}
