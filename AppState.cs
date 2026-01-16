using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PlayCutWin
{
    public class AppState : INotifyPropertyChanged
    {
        private static AppState _instance = new AppState();
        public static AppState Instance => _instance;

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ===== Selected Clip =====
        private VideoItem _selectedClip;
        public VideoItem SelectedClip
        {
            get => _selectedClip;
            set { _selectedClip = value; Notify(); Notify(nameof(SelectedVideoText)); }
        }

        public string SelectedVideoText =>
            SelectedClip == null ? "No video selected" : SelectedClip.FileName;

        // ===== Playback =====
        private double _playbackSeconds;
        public double PlaybackSeconds
        {
            get => _playbackSeconds;
            set { _playbackSeconds = value; Notify(); Notify(nameof(PlaybackPositionText)); }
        }

        private double _playbackDuration;
        public double PlaybackDuration
        {
            get => _playbackDuration;
            set { _playbackDuration = value; Notify(); Notify(nameof(PlaybackDurationText)); }
        }

        public string PlaybackPositionText => FormatTime(PlaybackSeconds);
        public string PlaybackDurationText => FormatTime(PlaybackDuration);

        // ===== Tags =====
        public Dictionary<VideoItem, List<string>> Tags
            = new Dictionary<VideoItem, List<string>>();

        public List<string> CurrentTags =>
            SelectedClip != null && Tags.ContainsKey(SelectedClip)
                ? Tags[SelectedClip]
                : new List<string>();

        public void AddTag(string tag)
        {
            if (SelectedClip == null || string.IsNullOrWhiteSpace(tag)) return;

            if (!Tags.ContainsKey(SelectedClip))
                Tags[SelectedClip] = new List<string>();

            Tags[SelectedClip].Add(tag);
            Notify(nameof(CurrentTags));
        }

        public void ClearTagsForSelected()
        {
            if (SelectedClip == null) return;
            Tags.Remove(SelectedClip);
            Notify(nameof(CurrentTags));
        }

        // ===== Utils =====
        public static string FormatTime(double sec)
        {
            var t = TimeSpan.FromSeconds(sec);
            return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
        }
    }
}
