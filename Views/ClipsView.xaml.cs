using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private readonly DispatcherTimer _timer;

        private bool _isDragging = false;
        private bool _ignoreSeek = false;
        private bool _mediaReady = false;

        // Range play
        private bool _rangePlayActive = false;
        private TimeSpan _rangeEnd = TimeSpan.Zero;

        // NEW: selected clip (for range follow)
        private PlayCutWin.ClipItem? _selectedClip;

        // NEW: nudge step
        private static readonly TimeSpan NudgeStep = TimeSpan.FromMilliseconds(500); // ±0.5s

        public ClipsView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;

            RefreshCount();
            RefreshSelectedTitle();

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (_, __) => Tick();
            _timer.Start();
        }

        // -------------------------
        // UI helpers
        // -------------------------
        private void RefreshCount()
        {
            try { CountText.Text = $"Count: {PlayCutWin.AppState.Instance.ImportedVideos.Count}"; }
            catch { CountText.Text = "Count: 0"; }
        }

        private void RefreshSelectedTitle()
        {
            var path = PlayCutWin.AppState.Instance.SelectedVideoPath ?? "";
            SelectedTitle.Text = string.IsNullOrWhiteSpace(path)
                ? "Selected: (none)"
                : $"Selected: {System.IO.Path.GetFileName(path)}";
        }

        private static string Fmt(TimeSpan t)
        {
            int total = (int)Math.Max(0, t.TotalSeconds);
            int mm = total / 60;
            int ss = total % 60;
            return $"{mm:00}:{ss:00}";
        }

        private static TimeSpan Clamp(TimeSpan value, TimeSpan min, TimeSpan max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private TimeSpan DurationSafe()
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan) return Player.NaturalDuration.TimeSpan;
            }
            catch { }
            return PlayCutWin.AppState.Instance.PlaybackDuration;
        }

        private void EnsureClipRangeOrder()
        {
            var s = PlayCutWin.AppState.Instance.ClipStart;
            var e = PlayCutWin.AppState.Instance.ClipEnd;
            if (s.HasValue && e.HasValue && e.Value <= s.Value)
            {
                // Keep at least 0.1s
                var dur = DurationSafe();
                var newEnd = Clamp(s.Value + TimeSpan.FromMilliseconds(100), TimeSpan.Zero, dur);
                PlayCutWin.AppState.Instance.ClipEnd = newEnd;
            }
        }

        // -------------------------
        // Selection (video list)
        // -------------------------
        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshCount();

            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                PlayCutWin.AppState.Instance.SetSelected(item.Path);
                RefreshSelectedTitle();
                LoadVideo(item.Path, autoPlay: false);
            }
        }

        private void VideosGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                LoadVideo(item.Path, autoPlay: true);
            }
        }

        private void LoadVideo(string path, bool autoPlay)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                _mediaReady = false;
                _rangePlayActive = false;

                _timer.Stop();
                Player.Stop();

                Player.Source = new Uri(path, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;

                _ignoreSeek = true;
                SeekSlider.Value = 0;
                SeekSlider.Maximum = 1;
                SeekSlider.IsEnabled = false;
                _ignoreSeek = false;

                TimeText.Text = "00:00 / 00:00";

                PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                PlayCutWin.AppState.Instance.PlaybackDuration = TimeSpan.Zero;

                if (autoPlay)
                {
                    Player.Play();
                    PlayCutWin.AppState.Instance.StatusMessage = $"Playing: {System.IO.Path.GetFileName(path)}";
                }
                else
                {
                    PlayCutWin.AppState.Instance.StatusMessage = $"Loaded: {System.IO.Path.GetFileName(path)}";
                }

                _timer.Start();
            }
            catch (Exception ex)
            {
                PlayCutWin.AppState.Instance.StatusMessage = $"Load failed: {ex.Message}";
                MessageBox.Show(ex.Message, "Load failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------
        // Player events
        // -------------------------
        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.NaturalDuration.HasTimeSpan)
                {
                    _mediaReady = true;

                    var dur = Player.NaturalDuration.TimeSpan;
                    PlayCutWin.AppState.Instance.PlaybackDuration = dur;
                    PlayCutWin.AppState.Instance.PlaybackPosition = Player.Position;

                    _ignoreSeek = true;
                    SeekSlider.Maximum = Math.Max(1, dur.TotalSeconds);
                    SeekSlider.Value = 0;
                    SeekSlider.IsEnabled = true;
                    _ignoreSeek = false;

                    UpdateTimeText();
                }
                else
                {
                    _mediaReady = false;
                    SeekSlider.IsEnabled = false;
                }
            }
            catch
            {
                _mediaReady = false;
                SeekSlider.IsEnabled = false;
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            try
            {
                _rangePlayActive = false;

                Player.Stop();
                Player.Position = TimeSpan.Zero;

                _ignoreSeek = true;
                SeekSlider.Value = 0;
                _ignoreSeek = false;

                PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                PlayCutWin.AppState.Instance.StatusMessage = "Ended";

                UpdateTimeText();
            }
            catch { }
        }

        // -------------------------
        // Controls
        // -------------------------
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Player.Source == null)
                {
                    PlayCutWin.AppState.Instance.StatusMessage = "No video loaded.";
                    return;
                }

                _rangePlayActive = false;
                Player.Play();
                PlayCutWin.AppState.Instance.StatusMessage = "Play";
            }
            catch { }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _rangePlayActive = false;
                Player.Pause();
                PlayCutWin.AppState.Instance.StatusMessage = "Pause";
            }
            catch { }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _rangePlayActive = false;

                Player.Stop();
                Player.Position = TimeSpan.Zero;

                _ignoreSeek = true;
                SeekSlider.Value = 0;
                _ignoreSeek = false;

                PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                PlayCutWin.AppState.Instance.StatusMessage = "Stop";

                UpdateTimeText();
            }
            catch { }
        }

        // -------------------------
        // Seek
        // -------------------------
        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            _rangePlayActive = false;
            SeekTo(SeekSlider.Value);
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ignoreSeek) return;

            if (_isDragging)
            {
                _rangePlayActive = false;
                var preview = TimeSpan.FromSeconds(SeekSlider.Value);
                PlayCutWin.AppState.Instance.PlaybackPosition = preview;
                UpdateTimeText(preview, PlayCutWin.AppState.Instance.PlaybackDuration);
            }
        }

        private void SeekTo(double seconds)
        {
            if (!_mediaReady) return;

            try
            {
                seconds = Math.Max(0, seconds);
                Player.Position = TimeSpan.FromSeconds(seconds);
                PlayCutWin.AppState.Instance.PlaybackPosition = Player.Position;
                UpdateTimeText();
            }
            catch { }
        }

        private void SeekTo(TimeSpan pos)
        {
            if (!_mediaReady) return;

            try
            {
                if (pos < TimeSpan.Zero) pos = TimeSpan.Zero;

                Player.Position = pos;
                PlayCutWin.AppState.Instance.PlaybackPosition = pos;

                _ignoreSeek = true;
                SeekSlider.Value = Math.Min(SeekSlider.Maximum, Math.Max(0, pos.TotalSeconds));
                _ignoreSeek = false;

                UpdateTimeText();
            }
            catch { }
        }

        // -------------------------
        // Timer tick
        // -------------------------
        private void Tick()
        {
            RefreshCount();
            RefreshSelectedTitle();

            if (!_mediaReady) return;
            if (_isDragging) return;

            try
            {
                var pos = Player.Position;
                var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;

                // range play stop
                if (_rangePlayActive && pos >= _rangeEnd)
                {
                    _rangePlayActive = false;
                    Player.Pause();
                    PlayCutWin.AppState.Instance.StatusMessage = "Range end reached";
                }

                PlayCutWin.AppState.Instance.PlaybackPosition = pos;
                PlayCutWin.AppState.Instance.PlaybackDuration = dur;

                _ignoreSeek = true;
                if (dur.TotalSeconds > 0)
                {
                    SeekSlider.Maximum = dur.TotalSeconds;
                    SeekSlider.Value = Math.Min(dur.TotalSeconds, pos.TotalSeconds);
                }
                _ignoreSeek = false;

                UpdateTimeText(pos, dur);
            }
            catch { }
        }

        private void UpdateTimeText()
        {
            var pos = PlayCutWin.AppState.Instance.PlaybackPosition;
            var dur = PlayCutWin.AppState.Instance.PlaybackDuration;
            UpdateTimeText(pos, dur);
        }

        private void UpdateTimeText(TimeSpan pos, TimeSpan dur)
        {
            var left = Fmt(pos);
            var right = dur.TotalSeconds > 0 ? Fmt(dur) : "--:--";
            TimeText.Text = $"{left} / {right}";
        }

        // -------------------------
        // Range controls (START/END/Add Clip)
        // -------------------------
        private void MarkStart_Click(object sender, RoutedEventArgs e)
        {
            PlayCutWin.AppState.Instance.MarkClipStart();
            EnsureClipRangeOrder();
        }

        private void MarkEnd_Click(object sender, RoutedEventArgs e)
        {
            PlayCutWin.AppState.Instance.MarkClipEnd();
            EnsureClipRangeOrder();
        }

        private void AddClip_Click(object sender, RoutedEventArgs e)
        {
            if (!PlayCutWin.AppState.Instance.CanCreateClip())
            {
                MessageBox.Show("STARTとENDを設定してね（END > START）", "Clip", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // team は AppState.SelectedTeam が採用される（team空で渡す）
            PlayCutWin.AppState.Instance.CreateClipFromRange(tagsText: "", team: "", note: "");
        }

        // -------------------------
        // Clip list -> Jump / Range play + NEW: range follow
        // -------------------------
        private void ClipsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClipsGrid.SelectedItem is PlayCutWin.ClipItem clip)
            {
                _selectedClip = clip;

                // 別動画のクリップなら今回は触らない（簡易）
                if (!string.IsNullOrWhiteSpace(clip.VideoPath) &&
                    !string.Equals(clip.VideoPath, PlayCutWin.AppState.Instance.SelectedVideoPath, StringComparison.OrdinalIgnoreCase))
                {
                    PlayCutWin.AppState.Instance.StatusMessage = "Selected clip belongs to another video.";
                    return;
                }

                // ① Startへジャンプ
                _rangePlayActive = false;
                SeekTo(clip.Start);

                // ② NEW: Range（START/END）を選択クリップに追従
                PlayCutWin.AppState.Instance.ClipStart = clip.Start;
                PlayCutWin.AppState.Instance.ClipEnd = clip.End;
                EnsureClipRangeOrder();

                PlayCutWin.AppState.Instance.StatusMessage = $"Selected clip: {clip.StartText}-{clip.EndText} (Range synced)";
            }
            else
            {
                _selectedClip = null;
            }
        }

        private void ClipsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ClipsGrid.SelectedItem is PlayCutWin.ClipItem clip)
            {
                if (!string.IsNullOrWhiteSpace(clip.VideoPath) &&
                    !string.Equals(clip.VideoPath, PlayCutWin.AppState.Instance.SelectedVideoPath, StringComparison.OrdinalIgnoreCase))
                {
                    PlayCutWin.AppState.Instance.StatusMessage = "Selected clip belongs to another video.";
                    return;
                }

                _rangePlayActive = true;
                _rangeEnd = clip.End;

                SeekTo(clip.Start);
                Player.Play();

                PlayCutWin.AppState.Instance.StatusMessage = $"Range play: {clip.StartText}-{clip.EndText}";
            }
        }

        // Optional: button to force sync range to selected clip (if you added it in XAML)
        private void SetRangeToSelectedClip_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedClip == null) return;

            PlayCutWin.AppState.Instance.ClipStart = _selectedClip.Start;
            PlayCutWin.AppState.Instance.ClipEnd = _selectedClip.End;
            EnsureClipRangeOrder();

            PlayCutWin.AppState.Instance.StatusMessage = "Range set to selected clip.";
        }

        // -------------------------
        // Integrated Tags panel events
        // -------------------------
        private void AddTag_Click(object sender, RoutedEventArgs e) => AddTagFromInput();

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTagFromInput();
                e.Handled = true;
            }
        }

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                var text = (b.Content?.ToString() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(text)) return;

                PlayCutWin.AppState.Instance.AddTagAtCurrentPosition(text);
            }
        }

        private void ClearPending_Click(object sender, RoutedEventArgs e)
        {
            PlayCutWin.AppState.Instance.ClearPendingTags();
            PlayCutWin.AppState.Instance.StatusMessage = "Pending tags cleared.";
            try { TagInput.Focus(); } catch { }
        }

        private void AddTagFromInput()
        {
            var text = (TagInput.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("タグを入力してね", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            PlayCutWin.AppState.Instance.AddTagAtCurrentPosition(text);

            TagInput.Text = "";
            try { TagInput.Focus(); } catch { }
        }

        // -------------------------
        // NEW: Nudge (±0.5s) buttons
        // -------------------------
        private void NudgeStartMinus_Click(object sender, RoutedEventArgs e) => NudgeStart(-NudgeStep);
        private void NudgeStartPlus_Click(object sender, RoutedEventArgs e) => NudgeStart(+NudgeStep);
        private void NudgeEndMinus_Click(object sender, RoutedEventArgs e) => NudgeEnd(-NudgeStep);
        private void NudgeEndPlus_Click(object sender, RoutedEventArgs e) => NudgeEnd(+NudgeStep);

        private void NudgeStart(TimeSpan delta)
        {
            var s = PlayCutWin.AppState.Instance.ClipStart;
            if (!s.HasValue) return;

            var dur = DurationSafe();
            var newStart = Clamp(s.Value + delta, TimeSpan.Zero, dur);

            PlayCutWin.AppState.Instance.ClipStart = newStart;
            EnsureClipRangeOrder();

            PlayCutWin.AppState.Instance.StatusMessage = $"START nudged: {PlayCutWin.AppState.FmtMMSS(newStart)}";
        }

        private void NudgeEnd(TimeSpan delta)
        {
            var e = PlayCutWin.AppState.Instance.ClipEnd;
            if (!e.HasValue) return;

            var dur = DurationSafe();
            var newEnd = Clamp(e.Value + delta, TimeSpan.Zero, dur);

            PlayCutWin.AppState.Instance.ClipEnd = newEnd;
            EnsureClipRangeOrder();

            PlayCutWin.AppState.Instance.StatusMessage = $"END nudged: {PlayCutWin.AppState.FmtMMSS(newEnd)}";
        }
    }
}
