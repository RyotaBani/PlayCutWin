using System;
using System.ComponentModel;
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

        // Range UI がある場合に使う（XAML側に無くてもOK）
        private const double NudgeSeconds = 0.5;

        public ClipsView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;

            RefreshCount();

            // AppState の変更（選択クリップなど）を拾って Range を追従させる
            TryHookAppState();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += (_, __) => UpdateTime();
        }

        private void TryHookAppState()
        {
            try
            {
                if (PlayCutWin.AppState.Instance is INotifyPropertyChanged npc)
                {
                    npc.PropertyChanged += AppState_PropertyChanged;
                }
            }
            catch { }
        }

        private void AppState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // ここは「AppState のどのプロパティ名でも落ちない」ようにしてある
            // 想定：
            // - SelectedClipStartSeconds / SelectedClipEndSeconds
            // - RangeStartSeconds / RangeEndSeconds
            // - ClipStartSeconds / ClipEndSeconds など
            if (e.PropertyName == null) return;

            if (e.PropertyName.Contains("SelectedClip", StringComparison.OrdinalIgnoreCase) ||
                e.PropertyName.Contains("Range", StringComparison.OrdinalIgnoreCase) ||
                e.PropertyName.Contains("ClipStart", StringComparison.OrdinalIgnoreCase) ||
                e.PropertyName.Contains("ClipEnd", StringComparison.OrdinalIgnoreCase))
            {
                // UIスレッドで反映
                Dispatcher.Invoke(() =>
                {
                    FollowSelectedClipRangeIfPossible();
                });
            }
        }

        private void RefreshCount()
        {
            CountText.Text = $"Count: {PlayCutWin.AppState.Instance.ImportedVideos.Count}";
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                PlayCutWin.AppState.Instance.SetSelected(item.Path);
                SelectedTitle.Text = $"Selected: {item.Name}";

                // 選択しただけでは自動再生しない（ダブルクリックで再生）
                LoadVideo(item.Path, autoPlay: false);

                // もし既に「選択クリップのRange」がAppStateに入っているなら反映
                FollowSelectedClipRangeIfPossible();
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
                SetAppStateTimeSpan("PlaybackPosition", TimeSpan.Zero);
                SetAppStateTimeSpan("PlaybackDuration", TimeSpan.Zero);

                TimeText.Text = "00:00 / 00:00";

                if (autoPlay)
                {
                    Player.Play();
                    SetAppStateString("StatusMessage", $"Playing: {System.IO.Path.GetFileName(path)}");
                }
                else
                {
                    SetAppStateString("StatusMessage", $"Selected: {System.IO.Path.GetFileName(path)}");
                }
            }
            catch (Exception ex)
            {
                SetAppStateString("StatusMessage", $"Load failed: {ex.Message}");
            }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            _mediaReady = Player.NaturalDuration.HasTimeSpan;
            if (_mediaReady)
            {
                var dur = Player.NaturalDuration.TimeSpan;

                SetAppStateTimeSpan("PlaybackDuration", dur);
                SetAppStateTimeSpan("PlaybackPosition", Player.Position);

                _ignoreSeek = true;
                SeekSlider.Maximum = Math.Max(1, dur.TotalSeconds);
                SeekSlider.Value = 0;
                SeekSlider.IsEnabled = true;
                _ignoreSeek = false;

                UpdateTime();
                _timer.Start();

                // 動画ロード後に「選択クリップRange」を追従
                FollowSelectedClipRangeIfPossible();
            }
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Player.Stop();
            Player.Position = TimeSpan.Zero;

            SetAppStateTimeSpan("PlaybackPosition", TimeSpan.Zero);
            SetAppStateString("StatusMessage", "Ended");
            UpdateTime();
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (Player.Source == null)
            {
                SetAppStateString("StatusMessage", "No video loaded.");
                return;
            }
            Player.Play();
            _timer.Start();
            SetAppStateString("StatusMessage", "Play");
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            Player.Pause();
            SetAppStateString("StatusMessage", "Pause");
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            Player.Stop();
            Player.Position = TimeSpan.Zero;

            SetAppStateTimeSpan("PlaybackPosition", TimeSpan.Zero);
            SetAppStateString("StatusMessage", "Stop");
            UpdateTime();
        }

        private void UpdateTime()
        {
            if (!_mediaReady) return;

            var pos = Player.Position;
            var dur = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;

            // AppState共有（Tags / Exports で使う）
            SetAppStateTimeSpan("PlaybackPosition", pos);
            SetAppStateTimeSpan("PlaybackDuration", dur);

            // AppStateに PlaybackPositionText があればそれを、無ければ自前表示
            var text = GetAppStateString("PlaybackPositionText");
            TimeText.Text = string.IsNullOrWhiteSpace(text)
                ? $"{Format(pos)} / {Format(dur)}"
                : text;

            if (_isDragging) return;

            if (dur.TotalSeconds > 0)
            {
                _ignoreSeek = true;
                SeekSlider.Maximum = dur.TotalSeconds;
                SeekSlider.Value = Math.Min(dur.TotalSeconds, pos.TotalSeconds);
                _ignoreSeek = false;
            }
        }

        private static string Format(TimeSpan t)
        {
            if (t.TotalHours >= 1) return t.ToString(@"hh\:mm\:ss");
            return t.ToString(@"mm\:ss");
        }

        private void SeekSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
        }

        private void SeekSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_mediaReady) { _isDragging = false; return; }

            _isDragging = false;
            Player.Position = TimeSpan.FromSeconds(SeekSlider.Value);
            SetAppStateTimeSpan("PlaybackPosition", Player.Position);
            UpdateTime();
        }

        private void SeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_ignoreSeek) return;
            if (!_isDragging) return;

            // ドラッグ中のプレビュー時間も共有
            SetAppStateTimeSpan("PlaybackPosition", TimeSpan.FromSeconds(SeekSlider.Value));

            var text = GetAppStateString("PlaybackPositionText");
            TimeText.Text = string.IsNullOrWhiteSpace(text)
                ? $"{Format(TimeSpan.FromSeconds(SeekSlider.Value))} / {Format(GetDuration())}"
                : text;
        }

        // =========================================================
        // ① 選択クリップ → Range追従（AppStateにあれば拾ってPlayerをJump）
        // =========================================================
        private void FollowSelectedClipRangeIfPossible()
        {
            if (!_mediaReady) return;
            if (Player.Source == null) return;

            // いろんな命名に対応して拾う（存在しなければ -1 のまま）
            var start =
                GetAppStateDouble("SelectedClipStartSeconds") ??
                GetAppStateDouble("RangeStartSeconds") ??
                GetAppStateDouble("ClipStartSeconds");

            var end =
                GetAppStateDouble("SelectedClipEndSeconds") ??
                GetAppStateDouble("RangeEndSeconds") ??
                GetAppStateDouble("ClipEndSeconds");

            if (start == null || end == null) return;

            var s = Clamp(start.Value, 0, GetDuration().TotalSeconds);
            var t = Clamp(end.Value, 0, GetDuration().TotalSeconds);

            if (t < s)
            {
                // 逆なら入れ替え
                var tmp = s; s = t; t = tmp;
            }

            // AppState側にも正規化して返す（存在するプロパティにだけ書く）
            SetAnyRangeSeconds(s, t);

            // プレイヤーをStartへ（「選択したらRange追従」の体感を作る）
            Player.Position = TimeSpan.FromSeconds(s);
            SetAppStateTimeSpan("PlaybackPosition", Player.Position);
            UpdateTime();
        }

        private void SetAnyRangeSeconds(double startSec, double endSec)
        {
            // どれか存在する名前にだけ書く（無ければ何もしない）
            SetAppStateDoubleIfExists("SelectedClipStartSeconds", startSec);
            SetAppStateDoubleIfExists("SelectedClipEndSeconds", endSec);

            SetAppStateDoubleIfExists("RangeStartSeconds", startSec);
            SetAppStateDoubleIfExists("RangeEndSeconds", endSec);

            SetAppStateDoubleIfExists("ClipStartSeconds", startSec);
            SetAppStateDoubleIfExists("ClipEndSeconds", endSec);
        }

        // =========================================================
        // ①-2 Range START/END + ±0.5s（XAMLにボタンがあればそのまま繋がる）
        // =========================================================
        private void ClipStart_Click(object sender, RoutedEventArgs e)
        {
            if (!_mediaReady) return;
            var pos = Player.Position.TotalSeconds;

            var (s, t) = GetCurrentRangeOrDefault();
            s = pos;

            NormalizeAndWriteRange(ref s, ref t);
        }

        private void ClipEnd_Click(object sender, RoutedEventArgs e)
        {
            if (!_mediaReady) return;
            var pos = Player.Position.TotalSeconds;

            var (s, t) = GetCurrentRangeOrDefault();
            t = pos;

            NormalizeAndWriteRange(ref s, ref t);
        }

        private void StartMinus_Click(object sender, RoutedEventArgs e)
        {
            var (s, t) = GetCurrentRangeOrDefault();
            s -= NudgeSeconds;
            NormalizeAndWriteRange(ref s, ref t);
        }

        private void StartPlus_Click(object sender, RoutedEventArgs e)
        {
            var (s, t) = GetCurrentRangeOrDefault();
            s += NudgeSeconds;
            NormalizeAndWriteRange(ref s, ref t);
        }

        private void EndMinus_Click(object sender, RoutedEventArgs e)
        {
            var (s, t) = GetCurrentRangeOrDefault();
            t -= NudgeSeconds;
            NormalizeAndWriteRange(ref s, ref t);
        }

        private void EndPlus_Click(object sender, RoutedEventArgs e)
        {
            var (s, t) = GetCurrentRangeOrDefault();
            t += NudgeSeconds;
            NormalizeAndWriteRange(ref s, ref t);
        }

        private (double start, double end) GetCurrentRangeOrDefault()
        {
            var start =
                GetAppStateDouble("SelectedClipStartSeconds") ??
                GetAppStateDouble("RangeStartSeconds") ??
                GetAppStateDouble("ClipStartSeconds") ??
                0.0;

            var end =
                GetAppStateDouble("SelectedClipEndSeconds") ??
                GetAppStateDouble("RangeEndSeconds") ??
                GetAppStateDouble("ClipEndSeconds") ??
                Math.Max(0.0, Player.Position.TotalSeconds);

            return (start.Value, end.Value);
        }

        private void NormalizeAndWriteRange(ref double startSec, ref double endSec)
        {
            var dur = GetDuration().TotalSeconds;
            startSec = Clamp(startSec, 0, dur);
            endSec = Clamp(endSec, 0, dur);

            if (endSec < startSec)
            {
                // endがstartを下回ったら、いったん揃える（操作しやすさ優先）
                endSec = startSec;
            }

            SetAnyRangeSeconds(startSec, endSec);

            // 体感を作る：Start を動かしたらそこへジャンプ
            Player.Position = TimeSpan.FromSeconds(startSec);
            SetAppStateTimeSpan("PlaybackPosition", Player.Position);
            UpdateTime();
        }

        private TimeSpan GetDuration()
        {
            if (_mediaReady && Player.NaturalDuration.HasTimeSpan)
                return Player.NaturalDuration.TimeSpan;
            return TimeSpan.Zero;
        }

        private static double Clamp(double v, double min, double max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        // =========================================================
        // AppState 反射ヘルパ（プロパティが無くても落ちない）
        // =========================================================
        private static object? GetAppStateProp(string name)
        {
            try
            {
                var t = typeof(PlayCutWin.AppState);
                return t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(PlayCutWin.AppState.Instance);
            }
            catch { return null; }
        }

        private static void SetAppStateProp(string name, object value)
        {
            try
            {
                var t = typeof(PlayCutWin.AppState);
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p == null || !p.CanWrite) return;

                // 型変換（double->TimeSpanとかはここではしない）
                p.SetValue(PlayCutWin.AppState.Instance, value);
            }
            catch { }
        }

        private static void SetAppStateString(string name, string value)
        {
            try { SetAppStateProp(name, value); } catch { }
        }

        private static string? GetAppStateString(string name)
        {
            try { return GetAppStateProp(name) as string; } catch { return null; }
        }

        private static void SetAppStateTimeSpan(string name, TimeSpan value)
        {
            try { SetAppStateProp(name, value); } catch { }
        }

        private static double? GetAppStateDouble(string name)
        {
            try
            {
                var v = GetAppStateProp(name);
                if (v is double d) return d;
                if (v is float f) return (double)f;
                if (v is int i) return i;
                if (v is long l) return l;
                return null;
            }
            catch { return null; }
        }

        private static void SetAppStateDoubleIfExists(string name, double value)
        {
            try
            {
                var t = typeof(PlayCutWin.AppState);
                var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p == null || !p.CanWrite) return;

                if (p.PropertyType == typeof(double)) p.SetValue(PlayCutWin.AppState.Instance, value);
                else if (p.PropertyType == typeof(float)) p.SetValue(PlayCutWin.AppState.Instance, (float)value);
                else if (p.PropertyType == typeof(int)) p.SetValue(PlayCutWin.AppState.Instance, (int)Math.Round(value));
                else if (p.PropertyType == typeof(long)) p.SetValue(PlayCutWin.AppState.Instance, (long)Math.Round(value));
                // それ以外は無視（落ちない）
            }
            catch { }
        }
    }
}
