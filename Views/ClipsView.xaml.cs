using System;
using System.Reflection;
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

        // Range (Start/End) : クリップ選択が来たら追従させる想定
        // ※現状XAMLにRange UIが無くても内部で保持しておく（後でUI追加してもそのまま使える）
        private double _rangeStartSec = 0.0;
        private double _rangeEndSec = 0.0;

        public ClipsView()
        {
            InitializeComponent();

            // 既存の流れに合わせて Instance を使う
            DataContext = PlayCutWin.AppState.Instance;

            RefreshCount();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += (_, __) => UpdateTime();

            // キーボードで±0.5s / ±5s を使えるように（XAML変更なしで追加）
            Loaded += (_, __) =>
            {
                Focusable = true;
                Keyboard.Focus(this);
            };
            KeyDown += ClipsView_KeyDown;
        }

        private void RefreshCount()
        {
            CountText.Text = $"Count: {PlayCutWin.AppState.Instance.ImportedVideos.Count}";
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                // 共有（他Viewへ）
                PlayCutWin.AppState.Instance.SetSelected(item.Path);

                // もし「クリップアイテム」が混ざっていて Start/End を持っていたら Range 追従させる
                TrySyncRangeFromSelectedItem(item);

                SelectedTitle.Text = $"Selected: {item.Name}";

                // 選択しただけでは自動再生しない（ダブルクリックで再生）
                LoadVideo(item.Path, autoPlay: false);
            }
        }

        private void VideosGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                TrySyncRangeFromSelectedItem(item);
                LoadVideo(item.Path, autoPlay: true);
            }
        }

        private void LoadVideo(string path, bool autoPlay)
        {
            try
            {
                _mediaReady = false;
                _timer.Stop();

                Player.Stop();
                Player.Source = new Uri(path, UriKind.Absolute);
                Player.Position = TimeSpan.Zero;

                // seek 初期化
                _ignoreSeek = true;
                SeekSlider.Value = 0;
                SeekSlider.Maximum = 1;
                SeekSlider.IsEnabled = false;
                _ignoreSeek = false;

                // AppStateへ共有
                PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
                PlayCutWin.AppState.Instance.PlaybackDuration = TimeSpan.Zero;

                TimeText.Text = "00:00 / 00:00";

                if (autoPlay)
                {
                    Player.Play();
                    _timer.Start();
                    PlayCutWin.AppState.Instance.StatusMessage = $"Playing: {System.IO.Path.GetFileName(path)}";
                }
                else
                {
                    PlayCutWin.AppState.Instance.StatusMessage = $"Selected: {System.IO.Path.GetFileName(path)}";
                }
            }
            catch (Exception ex)
            {
                PlayCutWin.AppState.Instance.StatusMessage = $"Load failed: {ex.Message}";
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            _mediaReady = Player.NaturalDuration.HasTimeSpan;
            if (!_mediaReady) return;

            var dur = Player.NaturalDuration.TimeSpan;

            PlayCutWin.AppState.Instance.PlaybackDuration = dur;
            PlayCutWin.AppState.Instance.PlaybackPosition = Player.Position;

            _ignoreSeek = true;
            SeekSlider.Maximum = Math.Max(1, dur.TotalSeconds);
            SeekSlider.Value = 0;
            SeekSlider.IsEnabled = true;
            _ignoreSeek = false;

            // Range が未設定っぽい時は「動画全体」をRangeに
            if (_rangeEndSec <= 0.0 || _rangeEndSec > dur.TotalSeconds)
            {
                _rangeStartSec = 0.0;
                _rangeEndSec = dur.TotalSeconds;
                TryPushRangeToAppState();
            }

            UpdateTime();
            _timer.Start();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Player.Stop();
            Player.Position = TimeSpan.Zero;

            PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
            PlayCutWin.AppState.Instance.StatusMessage = "Ended";
            UpdateTime();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                PlayCutWin.AppState.Instance.StatusMessage = "No video loaded.";
                return;
            }
            Player.Play();
            _timer.Start();
            PlayCutWin.AppState.Instance.StatusMessage = "Play";
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            Player.Pause();
            PlayCutWin.AppState.Instance.StatusMessage = "Pause";
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Player.Stop();
            Player.Position = TimeSpan.Zero;

            PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.Zero;
            PlayCutWin.AppState.Instance.StatusMessage = "Stop";
            UpdateTime();
        }

        private void UpdateTime()
        {
            if (!_mediaReady) return;

            var pos = Player.Position;
            var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;

            // AppState共有（Tags側などで使用）
            PlayCutWin.AppState.Instance.PlaybackPosition = pos;
            PlayCutWin.AppState.Instance.PlaybackDuration = dur;

            TimeText.Text = PlayCutWin.AppState.Instance.PlaybackPositionText;

            if (_isDragging) return;

            if (dur.TotalSeconds > 0)
            {
                _ignoreSeek = true;
                SeekSlider.Maximum = dur.TotalSeconds;
                SeekSlider.Value = Math.Min(dur.TotalSeconds, pos.TotalSeconds);
                _ignoreSeek = false;
            }
        }

        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_mediaReady) { _isDragging = false; return; }

            _isDragging = false;
            SeekToSeconds(SeekSlider.Value);
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ignoreSeek) return;
            if (!_isDragging) return;

            // ドラッグ中のプレビュー時間も共有
            PlayCutWin.AppState.Instance.PlaybackPosition = TimeSpan.FromSeconds(SeekSlider.Value);
            TimeText.Text = PlayCutWin.AppState.Instance.PlaybackPositionText;
        }

        // =========================
        // 追加：±0.5s / ±5s シーク
        // =========================
        private void ClipsView_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_mediaReady) return;

            // 入力中(TextBoxなど)は邪魔しない
            if (Keyboard.FocusedElement is TextBox) return;

            // 基本：←/→ で ±0.5s、Shift で ±5s
            if (e.Key == Key.Left)
            {
                var delta = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? -5.0 : -0.5;
                Nudge(delta);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Right)
            {
                var delta = (Keyboard.Modifiers & ModifierKeys.Shift) != 0 ? +5.0 : +0.5;
                Nudge(delta);
                e.Handled = true;
                return;
            }

            // おまけ：J/L でも ±0.5s（Mac版の感覚に寄せる）
            if (e.Key == Key.J)
            {
                Nudge(-0.5);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.L)
            {
                Nudge(+0.5);
                e.Handled = true;
                return;
            }

            // Spaceで再生/一時停止トグル
            if (e.Key == Key.Space)
            {
                if (Player.CanPause)
                {
                    Player.Pause();
                    PlayCutWin.AppState.Instance.StatusMessage = "Pause";
                }
                else
                {
                    Player.Play();
                    _timer.Start();
                    PlayCutWin.AppState.Instance.StatusMessage = "Play";
                }
                e.Handled = true;
                return;
            }

            // Ctrl+←/→ : Range Start を ±0.5
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.Left)
            {
                AdjustRangeStart(-0.5);
                e.Handled = true;
                return;
            }
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.Right)
            {
                AdjustRangeStart(+0.5);
                e.Handled = true;
                return;
            }

            // Ctrl+Shift+←/→ : Range End を ±0.5
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift)
                && e.Key == Key.Left)
            {
                AdjustRangeEnd(-0.5);
                e.Handled = true;
                return;
            }
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift)
                && e.Key == Key.Right)
            {
                AdjustRangeEnd(+0.5);
                e.Handled = true;
                return;
            }
        }

        private void Nudge(double deltaSeconds)
        {
            if (!_mediaReady) return;

            var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.TotalSeconds : 0.0;
            if (dur <= 0.0) return;

            var next = Clamp(Player.Position.TotalSeconds + deltaSeconds, 0.0, dur);
            SeekToSeconds(next);
            PlayCutWin.AppState.Instance.StatusMessage = $"Seek {deltaSeconds:+0.0;-0.0}s";
        }

        private void SeekToSeconds(double seconds)
        {
            if (!_mediaReady) return;

            Player.Position = TimeSpan.FromSeconds(seconds);
            PlayCutWin.AppState.Instance.PlaybackPosition = Player.Position;

            _ignoreSeek = true;
            SeekSlider.Value = seconds;
            _ignoreSeek = false;

            UpdateTime();
        }

        // =========================
        // 追加：Range追従（可能なら）
        // =========================
        private void TrySyncRangeFromSelectedItem(object item)
        {
            // クリップ用のプロパティ名を複数候補で探す
            // Start / End / StartSec / EndSec / StartSeconds / EndSeconds など
            var start = TryGetDouble(item, "Start")
                     ?? TryGetDouble(item, "StartSec")
                     ?? TryGetDouble(item, "StartSeconds");

            var end = TryGetDouble(item, "End")
                   ?? TryGetDouble(item, "EndSec")
                   ?? TryGetDouble(item, "EndSeconds");

            if (start.HasValue && end.HasValue && end.Value > start.Value)
            {
                _rangeStartSec = Math.Max(0.0, start.Value);
                _rangeEndSec = Math.Max(_rangeStartSec, end.Value);

                TryPushRangeToAppState();

                // まだメディアが開いてない場合は、MediaOpened後に全体Rangeへ補正される
                // メディアが開いてるなら Range開始へシーク
                if (_mediaReady)
                {
                    var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.TotalSeconds : 0.0;
                    _rangeStartSec = Clamp(_rangeStartSec, 0.0, dur);
                    _rangeEndSec = Clamp(_rangeEndSec, 0.0, dur);

                    SeekToSeconds(_rangeStartSec);
                    PlayCutWin.AppState.Instance.StatusMessage = $"Range: {_rangeStartSec:0.0}s - {_rangeEndSec:0.0}s";
                }
            }
        }

        private void AdjustRangeStart(double deltaSeconds)
        {
            if (!_mediaReady) return;
            var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.TotalSeconds : 0.0;
            if (dur <= 0.0) return;

            _rangeStartSec = Clamp(_rangeStartSec + deltaSeconds, 0.0, dur);
            if (_rangeStartSec > _rangeEndSec) _rangeEndSec = _rangeStartSec;

            TryPushRangeToAppState();
            PlayCutWin.AppState.Instance.StatusMessage = $"RangeStart: {_rangeStartSec:0.0}s";
        }

        private void AdjustRangeEnd(double deltaSeconds)
        {
            if (!_mediaReady) return;
            var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan.TotalSeconds : 0.0;
            if (dur <= 0.0) return;

            _rangeEndSec = Clamp(_rangeEndSec + deltaSeconds, 0.0, dur);
            if (_rangeEndSec < _rangeStartSec) _rangeStartSec = _rangeEndSec;

            TryPushRangeToAppState();
            PlayCutWin.AppState.Instance.StatusMessage = $"RangeEnd: {_rangeEndSec:0.0}s";
        }

        // AppState 側に ClipStart/ClipEnd みたいなプロパティがあれば反映（無ければ何もしない）
        private void TryPushRangeToAppState()
        {
            var app = PlayCutWin.AppState.Instance;
            TrySetProperty(app, "ClipStartSeconds", _rangeStartSec);
            TrySetProperty(app, "ClipEndSeconds", _rangeEndSec);
            TrySetProperty(app, "ClipStart", TimeSpan.FromSeconds(_rangeStartSec));
            TrySetProperty(app, "ClipEnd", TimeSpan.FromSeconds(_rangeEndSec));
        }

        // =========================
        // 小道具（Reflection安全版）
        // =========================
        private static double? TryGetDouble(object obj, string propName)
        {
            try
            {
                var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null) return null;

                var v = p.GetValue(obj);
                if (v == null) return null;

                if (v is double d) return d;
                if (v is float f) return (double)f;
                if (v is int i) return i;
                if (v is long l) return l;
                if (v is TimeSpan ts) return ts.TotalSeconds;

                if (double.TryParse(v.ToString(), out var parsed)) return parsed;
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static void TrySetProperty(object obj, string propName, object value)
        {
            try
            {
                var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null || !p.CanWrite) return;

                // 型が違う時は無理しない（落ちないこと優先）
                if (value != null && !p.PropertyType.IsAssignableFrom(value.GetType()))
                {
                    // double <-> int など最低限だけ
                    if (p.PropertyType == typeof(double) && value is IConvertible)
                    {
                        p.SetValue(obj, Convert.ToDouble(value));
                        return;
                    }
                    if (p.PropertyType == typeof(TimeSpan) && value is TimeSpan)
                    {
                        p.SetValue(obj, value);
                        return;
                    }
                    return;
                }

                p.SetValue(obj, value);
            }
            catch
            {
                // ignore
            }
        }

        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }
    }
}
