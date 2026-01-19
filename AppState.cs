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
    /// <summary>
    /// アプリ全体の状態（UIはこれだけをBindingする）
    /// MediaElementへの実命令(Play/Pause/Seek/Rate)は、PlayerView側が
    /// ここからのイベント/要求を受け取って実行する。
    /// </summary>
    public sealed class AppState : INotifyPropertyChanged
    {
        public static AppState Instance { get; } = new AppState();

        private AppState()
        {
            TeamAName = "Home / Our Team";
            TeamBName = "Away / Opponent";
            StatusMessage = "Ready";

            PlaybackSpeed = 1.0; // 0.25/0.5/1/2 を想定
        }

        // =========================
        // INotifyPropertyChanged
        // =========================
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

        // =========================
        // Models
        // =========================
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
        }

        public sealed class ClipItem
        {
            public string Team { get; set; } = "A"; // "A" or "B"
            public double Start { get; set; }
            public double End { get; set; }

            // Mac版に寄せて拡張（今は最低限）
            public List<string> Tags { get; set; } = new();
            public string SetPlay { get; set; } = "";
            public string Note { get; set; } = "";

            public string TagsText
            {
                get => string.Join(", ", Tags);
                set
                {
                    Tags = SplitTags(value).ToList();
                }
            }

            public string Display => $"{FormatTime(Start)} - {FormatTime(End)}  {TagsText}";

            private static IEnumerable<string> SplitTags(string? s)
            {
                s ??= "";
                s = s.Replace(";", ",");
                foreach (var p in s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var t = p.Trim();
                    if (t.Length > 0) yield return t;
                }
            }

            private static string FormatTime(double sec)
            {
                if (sec < 0) sec = 0;
                var ts = TimeSpan.FromSeconds(sec);
                return ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
            }
        }

        // =========================
        // Core State (Team / Status)
        // =========================
        private string _teamAName = "";
        public string TeamAName
        {
            get => _teamAName;
            set => Set(ref _teamAName, value);
        }

        private string _teamBName = "";
        public string TeamBName
        {
            get => _teamBName;
            set => Set(ref _teamBName, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => Set(ref _statusMessage, value);
        }

        // =========================
        // Video Import / Selection
        // =========================
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
                }
            }
        }

        public string SelectedVideoPath => SelectedVideo?.Path ?? "";
        public string SelectedVideoName => SelectedVideo?.Name ?? "Video (16:9)";

        public void AddImportedVideo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (ImportedVideos.Any(v => string.Equals(v.Path, path, StringComparison.OrdinalIgnoreCase)))
                return;

            var item = new VideoItem { Path = path };
            ImportedVideos.Add(item);

            if (SelectedVideo == null)
                SelectedVideo = item;

            StatusMessage = $"Imported: {System.IO.Path.GetFileName(path)}";
        }

        public void SetSelected(VideoItem item)
        {
            SelectedVideo = item;
            StatusMessage = $"Selected: {item.Name}";
        }

        public void LoadVideoFromDialog()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video files|*.mp4;*.mov;*.m4v;*.wmv;*.avi|All files|*.*",
                Multiselect = false
            };
            if (dlg.ShowDialog() != true) return;

            AddImportedVideo(dlg.FileName);

            // PlayerViewに「この動画を開いてね」を通知（命令はPlayerViewが実行）
            RequestOpenVideo?.Invoke(this, dlg.FileName);
        }

        // =========================
        // Playback State (UI Binding)
        // =========================
        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set => Set(ref _isPlaying, value);
        }

        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set
            {
                if (Set(ref _playbackSeconds, value))
                {
                    OnPropertyChanged(nameof(PlaybackPositionText));
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
                }
            }
        }

        public string PlaybackPositionText => FormatClock(PlaybackSeconds);
        public string PlaybackDurationText => FormatClock(PlaybackDuration);
        public string PlaybackDurationTextLong => FormatClockLong(PlaybackDuration);

        private double _playbackSpeed;
        public double PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                if (Set(ref _playbackSpeed, value))
                {
                    StatusMessage = $"Speed: {PlaybackSpeed:0.##}x";
                    RequestSetSpeed?.Invoke(this, PlaybackSpeed);
                }
            }
        }

        private static string FormatClock(double sec)
        {
            if (sec <= 0) return "00:00";
            var ts = TimeSpan.FromSeconds(sec);
            return ts.Hours > 0 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"mm\:ss");
        }

        private static string FormatClockLong(double sec)
        {
            if (sec <= 0) return "00:00:00";
            var ts = TimeSpan.FromSeconds(sec);
            return ts.ToString(@"hh\:mm\:ss");
        }

        // =========================
        // Playback Requests (PlayerView executes)
        // =========================
        public event EventHandler? RequestPlay;
        public event EventHandler? RequestPause;
        public event EventHandler? RequestStop;
        public event EventHandler<double>? RequestSeek;      // seconds
        public event EventHandler<double>? RequestSetSpeed;  // rate
        public event EventHandler<string>? RequestOpenVideo; // path

        public void Play()
        {
            RequestPlay?.Invoke(this, EventArgs.Empty);
            IsPlaying = true;
            StatusMessage = "Playing";
        }

        public void Pause()
        {
            RequestPause?.Invoke(this, EventArgs.Empty);
            IsPlaying = false;
            StatusMessage = "Paused";
        }

        public void StopPlayback()
        {
            // UI側で呼ばれていた名称に合わせた互換メソッド
            Stop();
        }

        public void Stop()
        {
            RequestStop?.Invoke(this, EventArgs.Empty);
            IsPlaying = false;
            StatusMessage = "Stopped";
        }

        public void TogglePlayPause()
        {
            if (IsPlaying) Pause();
            else Play();
        }

        public void SeekBy(double deltaSeconds)
        {
            var next = PlaybackSeconds + deltaSeconds;
            if (next < 0) next = 0;
            if (PlaybackDuration > 0 && next > PlaybackDuration) next = PlaybackDuration;

            RequestSeek?.Invoke(this, next);
            PlaybackSeconds = next;
            StatusMessage = $"Seek: {PlaybackPositionText}";
        }

        public void SeekTo(double seconds)
        {
            var next = seconds;
            if (next < 0) next = 0;
            if (PlaybackDuration > 0 && next > PlaybackDuration) next = PlaybackDuration;

            RequestSeek?.Invoke(this, next);
            PlaybackSeconds = next;
            StatusMessage = $"Seek: {PlaybackPositionText}";
        }

        public void SetSpeed(double rate)
        {
            // UI側がボタンで呼ぶ用
            PlaybackSpeed = rate;
        }

        // =========================
        // Clip Range
        // =========================
        private double _clipStart;
        public double ClipStart
        {
            get => _clipStart;
            set => Set(ref _clipStart, value);
        }

        private double _clipEnd;
        public double ClipEnd
        {
            get => _clipEnd;
            set => Set(ref _clipEnd, value);
        }

        public void ClipMarkStart()
        {
            ClipStart = PlaybackSeconds;
            StatusMessage = $"Clip START: {FormatClock(ClipStart)}";
        }

        public void ClipMarkEnd()
        {
            ClipEnd = PlaybackSeconds;
            StatusMessage = $"Clip END: {FormatClock(ClipEnd)}";
        }

        public void ResetRange()
        {
            ClipStart = 0;
            ClipEnd = 0;
            StatusMessage = "Range reset";
        }

        // =========================
        // Tags / Clips
        // =========================
        public ObservableCollection<TagEntry> Tags { get; } = new();
        public ObservableCollection<ClipItem> Clips { get; } = new();

        private TagEntry? _selectedTag;
        public TagEntry? SelectedTag
        {
            get => _selectedTag;
            set => Set(ref _selectedTag, value);
        }

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
                foreach (var t in c.Tags)
                {
                    var s = (t ?? "").Trim();
                    if (s.Length > 0) set.Add(s);
                }
            }

            return set.OrderBy(x => x);
        }

        public void AddTag(string tag)
        {
            tag = (tag ?? "").Trim();
            if (tag.Length == 0) return;

            Tags.Add(new TagEntry { Tag = tag, CreatedAt = DateTime.Now });
            StatusMessage = $"Tag added: {tag}";
        }

        public void AddTagToSelected(string tag)
        {
            // 互換：以前のメソッド名
            AddTag(tag);
        }

        public void RemoveSelectedTag()
        {
            // UI側でこの名前が呼ばれていたので用意
            RemoveSelectedTag(SelectedTag);
        }

        public void RemoveSelectedTag(TagEntry? tag)
        {
            if (tag == null) return;
            Tags.Remove(tag);
            if (ReferenceEquals(SelectedTag, tag)) SelectedTag = null;
            StatusMessage = $"Tag removed: {tag.Tag}";
        }

        public void ClearTags()
        {
            Tags.Clear();
            SelectedTag = null;
            StatusMessage = "Tags cleared";
        }

        public void ClearTagsForSelected()
        {
            // 互換：以前のメソッド名
            ClearTags();
        }

        private List<string> GetCurrentTagList()
        {
            // 現在の仕様：Tagsコレクション＝「現在選択中のタグ」として扱う
            // （Mac版の "Current Tags" に相当）
            return Tags
                .Select(t => (t.Tag ?? "").Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public void SaveClip(string team)
        {
            // UI側で SaveClip("A") などで呼ぶ想定
            team = (team ?? "A").Trim().ToUpperInvariant();
            if (team != "A" && team != "B") team = "A";

            var s = ClipStart;
            var e = ClipEnd;

            if (e < s)
            {
                // 逆なら入れ替え
                (s, e) = (e, s);
            }

            if (Math.Abs(e - s) < 0.01)
            {
                StatusMessage = "Clip range is empty";
                return;
            }

            var clip = new ClipItem
            {
                Team = team,
                Start = s,
                End = e,
                Tags = GetCurrentTagList()
            };

            Clips.Add(clip);
            StatusMessage = $"Saved clip ({team}): {FormatClock(s)} - {FormatClock(e)}";

            // 保存後、Current TagsはMacと同じく保持したいなら残す
            // 今は維持（必要なら ClearTags() に変更できる）
        }

        public void SaveTeamA()
        {
            SaveClip("A");
        }

        public void SaveTeamB()
        {
            SaveClip("B");
        }

        // =========================
        // CSV Import/Export (暫定)
        // =========================
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
        // TAG,tag,createdAtIso
        // CLIP,team,start,end,tagsText,setPlay,note
        public void ImportCsv(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath);
                int addedTags = 0, addedClips = 0;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var cols = SplitCsvLine(line).ToArray();
                    if (cols.Length == 0) continue;

                    var kind = (cols[0] ?? "").Trim();

                    if (string.Equals(kind, "TAG", StringComparison.OrdinalIgnoreCase))
                    {
                        if (cols.Length >= 2)
                        {
                            var tag = (cols[1] ?? "").Trim();
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
                            var team = (cols[1] ?? "A").Trim();
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
                        // 何も付いてないCSVは「タグ1列」とみなす
                        var tag = (cols[0] ?? "").Trim();
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
                {
                    outLines.Add(
                        $"CLIP,{EscapeCsv(c.Team)}," +
                        $"{c.Start.ToString(CultureInfo.InvariantCulture)}," +
                        $"{c.End.ToString(CultureInfo.InvariantCulture)}," +
                        $"{EscapeCsv(c.TagsText)}," +
                        $"{EscapeCsv(c.SetPlay)}," +
                        $"{EscapeCsv(c.Note)}"
                    );
                }

                File.WriteAllLines(filePath, outLines);
                StatusMessage = $"Exported CSV: {System.IO.Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"CSV export failed: {ex.Message}";
            }
        }

        // =========================
        // Helpers
        // =========================
        private static double ParseDouble(string? s)
        {
            s ??= "";
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            if (double.TryParse(s, out v)) return v;
            return 0;
        }

        private static string EscapeCsv(string? s)
        {
            s ??= "";
            if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static IEnumerable<string> SplitCsvLine(string line)
        {
            // 最小のCSVパーサ（ダブルクォート対応）
            if (line == null) yield break;

            var cur = "";
            bool inQ = false;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (inQ)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            cur += '"';
                            i++;
                        }
                        else
                        {
                            inQ = false;
                        }
                    }
                    else
                    {
                        cur += ch;
                    }
                }
                else
                {
                    if (ch == ',')
                    {
                        yield return cur;
                        cur = "";
                    }
                    else if (ch == '"')
                    {
                        inQ = true;
                    }
                    else
                    {
                        cur += ch;
                    }
                }
            }

            yield return cur;
        }
    }
}
