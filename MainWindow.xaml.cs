using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        // Playback speed (default: 1x)
        private double _playbackSpeed = 1.0;

        private readonly DispatcherTimer _timer;
        private bool _isDraggingTimeline = false;

        private double _clipStart = 0.0;
        private double _clipEnd = 0.0;

        public MainWindow()
        {
            InitializeComponent();

            // Timer for updating UI while playing
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _timer.Tick += (_, __) => Tick();
            _timer.Start();

            StatusText.Text = "Ready";

            // Default playback speed: 1x (UI highlight)
            SetSpeed(1.0, applyToPlayer: false);
        }

        // ================= Video Load =================
        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                VideoHint.Visibility = Visibility.Collapsed;

                Player.Stop();
                Player.Source = new Uri(dlg.FileName, UriKind.Absolute);

                // apply current selected speed
                Player.SpeedRatio = _playbackSpeed;

                // show first frame
                Player.Play();
                Player.Pause();

                StatusText.Text = "Loaded";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Load failed: {ex.Message}";
                VideoHint.Visibility = Visibility.Visible;
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (Player.NaturalDuration.HasTimeSpan)
            {
                var dur = Player.NaturalDuration.TimeSpan.TotalSeconds;
                TimelineSlider.Maximum = dur;
                TimelineSlider.Value = 0;

                // re-apply speed on open
                Player.SpeedRatio = _playbackSpeed;
                UpdateSpeedButtons();

                StatusText.Text = "Ready";
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            StatusText.Text = $"Media failed: {e.ErrorException?.Message}";
            VideoHint.Visibility = Visibility.Visible;
        }

        // ================= Playback =================
        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;

            // MediaElement doesn't expose IsPlaying; we infer from Position updates.
            // If it is effectively paused, play; otherwise pause.
            // Simple approach: toggle based on a tag.
            if ((string?)Player.Tag != "Playing")
            {
                Player.Play();
                Player.Tag = "Playing";
                StatusText.Text = "Playing";
            }
            else
            {
                Player.Pause();
                Player.Tag = "Paused";
                StatusText.Text = "Paused";
            }
        }

        private void Tick()
        {
            if (Player.Source == null) return;
            if (_isDraggingTimeline) return;

            // Update slider while playing/paused
            TimelineSlider.Value = Player.Position.TotalSeconds;
        }

        // ================= Timeline =================
        private void TimelineSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDraggingTimeline = true;
        }

        private void TimelineSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDraggingTimeline = false;
            if (Player.Source == null) return;

            Player.Position = TimeSpan.FromSeconds(TimelineSlider.Value);
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isDraggingTimeline) return;
            // when not dragging, value is driven by Tick()
        }

        // ================= Speed =================
        private void Speed025_Click(object sender, RoutedEventArgs e) => SetSpeed(0.25);
        private void Speed05_Click(object sender, RoutedEventArgs e) => SetSpeed(0.5);
        private void Speed1_Click(object sender, RoutedEventArgs e) => SetSpeed(1.0);
        private void Speed2_Click(object sender, RoutedEventArgs e) => SetSpeed(2.0);

        private void SetSpeed(double speed, bool applyToPlayer = true)
        {
            _playbackSpeed = speed;
            UpdateSpeedButtons();

            if (applyToPlayer && Player.Source != null)
            {
                Player.SpeedRatio = speed;
            }

            StatusText.Text = $"Speed: {speed:0.##}x";
        }

        private void UpdateSpeedButtons()
        {
            // Brushes are defined in MainWindow.xaml resources
            var accent = (Brush)FindResource("Accent");
            var normalBg = (Brush)FindResource("ButtonBg");
            var normalBorder = (Brush)FindResource("ButtonBorder");
            var text = (Brush)FindResource("TextPrimary");

            ApplySpeedButton(Speed025Button, Math.Abs(_playbackSpeed - 0.25) < 0.0001);
            ApplySpeedButton(Speed05Button, Math.Abs(_playbackSpeed - 0.5) < 0.0001);
            ApplySpeedButton(Speed1Button, Math.Abs(_playbackSpeed - 1.0) < 0.0001);
            ApplySpeedButton(Speed2Button, Math.Abs(_playbackSpeed - 2.0) < 0.0001);

            void ApplySpeedButton(Button? btn, bool selected)
            {
                if (btn == null) return;
                btn.Background = selected ? accent : normalBg;
                btn.BorderBrush = selected ? accent : normalBorder;
                btn.Foreground = text;
            }
        }

        // ================= Seek =================
        private void SeekMinus5_Click(object sender, RoutedEventArgs e) => SeekBy(-5);
        private void SeekMinus1_Click(object sender, RoutedEventArgs e) => SeekBy(-1);
        private void SeekPlus1_Click(object sender, RoutedEventArgs e) => SeekBy(1);
        private void SeekPlus5_Click(object sender, RoutedEventArgs e) => SeekBy(5);

        private void SeekBy(double seconds)
        {
            if (Player.Source == null) return;
            var t = Player.Position.TotalSeconds + seconds;
            if (t < 0) t = 0;
            if (Player.NaturalDuration.HasTimeSpan)
            {
                var max = Player.NaturalDuration.TimeSpan.TotalSeconds;
                if (t > max) t = max;
            }
            Player.Position = TimeSpan.FromSeconds(t);
            TimelineSlider.Value = t;
        }

        // ================= Clip Range =================
        private void ClipStart_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            _clipStart = Player.Position.TotalSeconds;
            UpdateClipRangeText();
        }

        private void ClipEnd_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null) return;
            _clipEnd = Player.Position.TotalSeconds;
            UpdateClipRangeText();
        }

        private void UpdateClipRangeText()
        {
            if (_clipEnd < _clipStart) _clipEnd = _clipStart;
            ClipRangeText.Text = $"START: {_clipStart:0.00}   END: {_clipEnd:0.00}";
        }

        private void SaveTeamA_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Saved Team A (stub)";
        }

        private void SaveTeamB_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Saved Team B (stub)";
        }

        // ================= CSV/Export/Tags (stubs) =================
        private void ImportCsv_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Import CSV (stub)";
        private void ExportCsv_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Export CSV (stub)";
        private void ExportAll_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Export All (stub)";

        private void Preferences_Click(object sender, RoutedEventArgs e) => StatusText.Text = "Preferences (stub)";

        private void Tag_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                CurrentTagsText.Text = b.Content?.ToString() ?? "(tag)";
                StatusText.Text = $"Tag: {CurrentTagsText.Text}";
            }
        }

        private void AddCustomTag_Click(object sender, RoutedEventArgs e)
        {
            var t = CustomTagTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(t)) return;
            CurrentTagsText.Text = t;
            StatusText.Text = $"Custom Tag: {t}";
        }

        private void ClearCustomTag_Click(object sender, RoutedEventArgs e)
        {
            CustomTagTextBox.Text = "";
            CurrentTagsText.Text = "(No tags selected)";
            StatusText.Text = "Cleared";
        }
    }
}
