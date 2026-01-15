using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    // Imported video row model
    public class VideoItem
    {
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
    }

    // Tag model (Pending tags for next clip)
    public class TagItem
    {
        public string Text { get; set; } = "";
        public string Time { get; set; } = ""; // e.g. "03:12"
    }

    // Clip model
    public class ClipItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string VideoPath { get; set; } = "";
        public string VideoName { get; set; } = "";

        public TimeSpan Start { get; set; } = TimeSpan.Zero;
        public TimeSpan End { get; set; } = TimeSpan.Zero;

        public string StartText => AppState.FmtMMSS(Start);
        public string EndText => AppState.FmtMMSS(End);

        public string TagsText { get; set; } = "";

        public string Team { get; set; } = "Team A";
        public string Note { get; set; } = "";
    }

    // App-wide shared state
    public class AppState : INotifyPropertyChanged
    {
        private static readonly AppState _instance = new AppState();
        public static AppState Instance => _instance;
        public static AppState Current => _instance; // compat

        // ---- Status ----
        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        // ---- Selected video ----
        private string _selectedVideoPath = "";
        public string SelectedVideoPath
        {
            get => _selectedVideoPath;
            set
            {
                if (_selectedVideoPath == value) return;
                _selectedVideoPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedVideoName));
            }
        }

        public string SelectedVideoName
            => string.IsNullOrWhiteSpace(SelectedVideoPath) ? "(none)" : System.IO.Path.GetFileName(SelectedVideoPath);

        // ---- Collections ----
        public ObservableCollection<VideoItem> ImportedVideos { get; } = new ObservableCollection<VideoItem>();

        // Pending tags (next clip)
        public ObservableCollection<TagItem> Tags { get; } = new ObservableCollection<TagItem>();

        public ObservableCollection<ClipItem> Clips { get; } = new ObservableCollection<ClipItem>();

        // ---- Export ----
        private string _exportFolder = "";
        public string ExportFolder
        {
            get => _exportFolder;
            set
            {
                if (_exportFolder == value) return;
                _exportFolder = value ?? "";
                OnPropertyChanged();
            }
        }

        // ---- Import / Selection ----
        public void AddImportedVideo(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return;

            ImportedVideos.Add(new VideoItem
            {
                Name = System.IO.Path.GetFileName(fullPath),
                Path = fullPath
            });

            StatusMessage = $"Imported: {System.IO.Path.GetFileName(fullPath)}";
        }

        public void SetSelected(string fullPath)
        {
            SelectedVideoPath = fullPath ?? "";
            StatusMessage = string.IsNullOrWhiteSpace(SelectedVideoPath)
                ? "Selected: (none)"
                : $"Selected: {System.IO.Path.GetFileName(SelectedVideoPath)}";
        }

        // ---- Playback shared state ----
        private TimeSpan _playbackPosition = TimeSpan.Zero;
        public TimeSpan PlaybackPosition
        {
            get => _playbackPosition;
            set
            {
                if (_playbackPosition == value) return;
                _playbackPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        private TimeSpan _playbackDuration = TimeSpan.Zero;
        public TimeSpan PlaybackDuration
        {
            get => _playbackDuration;
            set
            {
                if (_playbackDuration == value) return;
                _playbackDuration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlaybackPositionText));
            }
        }

        public string PlaybackPositionText => $"{FmtMMSS(PlaybackPosition)} / {FmtOrDash(PlaybackDuration)}";

        // ---- Clip range ----
        private TimeSpan? _clipStart;
        public TimeSpan? ClipStart
        {
            get => _clipStart;
            set
            {
                if (_clipStart == value) return;
                _clipStart = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClipStartText));
            }
        }

        private TimeSpan? _clipEnd;
        public TimeSpan? ClipEnd
        {
            get => _clipEnd;
            set
            {
                if (_clipEnd == value) return;
                _clipEnd = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ClipEndText));
            }
        }

        public string ClipStartText => ClipStart.HasValue ? FmtMMSS(ClipStart.Value) : "--:--";
        public string ClipEndText => ClipEnd.HasValue ? FmtMMSS(ClipEnd.Value) : "--:--";

        public void MarkClipStart()
        {
            ClipStart = PlaybackPosition;
            StatusMessage = $"Clip START = {ClipStartText}";
        }

        public void MarkClipEnd()
        {
            ClipEnd = PlaybackPosition;
            StatusMessage = $"Clip END = {ClipEndText}";
        }

        public bool CanCreateClip()
        {
            if (string.IsNullOrWhiteSpace(SelectedVideoPath)) return false;
            if (!ClipStart.HasValue || !ClipEnd.HasValue) return false;
            return ClipEnd.Value > ClipStart.Value;
        }

        // ---- Tagging (pending tags) ----
        // legacy compat (not used in new UI, but keep)
        public void AddTag(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            Tags.Add(new TagItem { Text = text.Trim(), Time = "--:--" });
            StatusMessage = $"Tag added: {text.Trim()}";
            OnPropertyChanged(nameof(PendingTagsText));
        }

        public void AddTagAtCurrentPosition(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var time = FmtMMSS(PlaybackPosition);
            Tags.Add(new TagItem { Text = text.Trim(), Time = time });

            StatusMessage = $"Tag added: {time} {text.Trim()}";
            OnPropertyChanged(nameof(PendingTagsText));
        }

        public string PendingTagsText
            => Tags.Count == 0
                ? "(no tags)"
                : string.Join(", ", Tags.Select(t => $"{t.Time} {t.Text}"));

        public void ClearPendingTags()
        {
            Tags.Clear();
            OnPropertyChanged(nameof(PendingTagsText));
        }

        private string BuildTagsTextForClip()
        {
            if (Tags.Count == 0) return "";
            return string.Join(", ", Tags.Select(t => $"{t.Time} {t.Text}"));
        }

        // ---- Create clip ----
        public void CreateClipFromRange(string tagsText = "", string team = "Team A", string note = "")
        {
            if (!CanCreateClip())
            {
                StatusMessage = "Clip range is invalid. Set START and END (END > START).";
                return;
            }

            var finalTags = string.IsNullOrWhiteSpace(tagsText) ? BuildTagsTextForClip() : tagsText.Trim();

            var clip = new ClipItem
            {
                VideoPath = SelectedVideoPath,
                VideoName = SelectedVideoName,
                Start = ClipStart!.Value,
                End = ClipEnd!.Value,
                TagsText = finalTags,
                Team = string.IsNullOrWhiteSpace(team) ? "Team A" : team,
                Note = note ?? ""
            };

            Clips.Add(clip);
            StatusMessage = $"Clip added: {clip.StartText}-{clip.EndText}";

            // Prepare for next clip
            ClipStart = clip.End;
            ClipEnd = null;

            // Clear pending tags after creating a clip
            ClearPendingTags();
        }

        public void RemoveClip(Guid id)
        {
            var hit = Clips.FirstOrDefault(c => c.Id == id);
            if (hit != null)
            {
                Clips.Remove(hit);
                StatusMessage = "Clip removed.";
            }
        }

        // ---- Formatting helpers ----
        public static string FmtMMSS(TimeSpan t)
        {
            int total = (int)Math.Max(0, t.TotalSeconds);
            int mm = total / 60;
            int ss = total % 60;
            return $"{mm:00}:{ss:00}";
        }

        private static string FmtOrDash(TimeSpan t)
        {
            if (t.TotalSeconds <= 0) return "--:--";
            return FmtMMSS(t);
        }

        // ---- INotifyPropertyChanged ----
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
