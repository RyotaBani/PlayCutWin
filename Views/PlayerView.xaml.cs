using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlayCutWin.Views
{
    public partial class PlayerView : UserControl
    {
        private readonly AppState _state;
        private TimeSpan _clipStart = TimeSpan.Zero;
        private TimeSpan _clipEnd = TimeSpan.Zero;

        public PlayerView()
        {
            InitializeComponent();

            _state = AppState.Instance;
            DataContext = _state;

            if (_state is INotifyPropertyChanged npc)
            {
                npc.PropertyChanged += State_PropertyChanged;
            }

            UpdateTitleFromState();
        }

        private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // どの名前で来ても拾えるようにざっくり更新
            if (e.PropertyName == "SelectedVideo" ||
                e.PropertyName == "SelectedVideoPath" ||
                e.PropertyName == "SelectedVideoText" ||
                e.PropertyName == "CurrentVideoPath")
            {
                Dispatcher.Invoke(UpdateTitleFromState);
            }
        }

        private void UpdateTitleFromState()
        {
            var path = GetStateString(
                "SelectedVideoPath",
                "CurrentVideoPath",
                "SelectedVideoText",
                "SelectedVideo");

            if (!string.IsNullOrWhiteSpace(path))
            {
                // "SelectedVideo" が VideoItem 等の可能性があるので、文字列じゃなければ無視
                if (path.Contains("\\") || path.Contains("/") || path.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    TitleText.Text = Path.GetFileName(path);
                    return;
                }

                // すでに "xxx.mp4" みたいな表示文字列の可能性
                TitleText.Text = path;
                return;
            }

            TitleText.Text = "Video (16:9)";
        }

        private string? GetStateString(params string[] propNames)
        {
            foreach (var name in propNames)
            {
                var p = _state.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p == null) continue;

                var v = p.GetValue(_state);
                if (v == null) continue;

                if (v is string s && !string.IsNullOrWhiteSpace(s)) return s;

                // VideoItem等 → Pathっぽいプロパティを探す
                var t = v.GetType();
                var pathProp = t.GetProperty("Path") ?? t.GetProperty("FullPath") ?? t.GetProperty("FilePath");
                if (pathProp != null)
                {
                    var pv = pathProp.GetValue(v) as string;
                    if (!string.IsNullOrWhiteSpace(pv)) return pv;
                }
            }
            return null;
        }

        // ======= MainWindow から呼ばれる想定のAPI =======

        public void LoadVideo(string path, bool autoPlay)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                Player.Source = null;
                PlaceholderText.Visibility = Visibility.Visible;
                TitleText.Text = "Video (16:9)";
                return;
            }

            TitleText.Text = Path.GetFileName(path);

            try
            {
                Player.Source = new Uri(path, UriKind.Absolute);
                PlaceholderText.Visibility = Visibility.Collapsed;

                // これがあると「前の音が残る」系が減ることがある
                Player.Stop();

                if (autoPlay) Player.Play();
            }
            catch
            {
                Player.Source = null;
                PlaceholderText.Visibility = Visibility.Visible;
                TitleText.Text = "Video (16:9)";
            }
        }

        public void Play()  => Player.Play();
        public void Pause() => Player.Pause();
        public void Stop()  => Player.Stop();

        public void SeekBy(double deltaSeconds)
        {
            try
            {
                var t = Player.Position + TimeSpan.FromSeconds(deltaSeconds);
                if (t < TimeSpan.Zero) t = TimeSpan.Zero;
                Player.Position = t;

                // AppState に再生位置を持ってるなら反映（あれば）
                SetStateDouble("PlaybackSeconds", t.TotalSeconds);
            }
            catch { }
        }

        public void MarkClipStart()
        {
            _clipStart = Player.Position;
            SetStateDouble("ClipStart", _clipStart.TotalSeconds);
        }

        public void MarkClipEnd()
        {
            _clipEnd = Player.Position;
            SetStateDouble("ClipEnd", _clipEnd.TotalSeconds);
        }

        public void ResetClipRange()
        {
            _clipStart = TimeSpan.Zero;
            _clipEnd = TimeSpan.Zero;
            SetStateDouble("ClipStart", 0);
            SetStateDouble("ClipEnd", 0);
        }

        private void SetStateDouble(string propName, double value)
        {
            try
            {
                var p = _state.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanWrite && p.PropertyType == typeof(double))
                {
                    p.SetValue(_state, value);
                }
            }
            catch { }
        }

        // ======= Team A / Team B 入力（AppState のプロパティ名が違っても拾える保険） =======

        private void TeamATextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetStateStringAny(TeamATextBox.Text, "TeamAName", "TeamA", "HomeTeamName");
        }

        private void TeamBTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetStateStringAny(TeamBTextBox.Text, "TeamBName", "TeamB", "AwayTeamName");
        }

        private void SetStateStringAny(string value, params string[] propNames)
        {
            foreach (var name in propNames)
            {
                try
                {
                    var p = _state.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                    if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                    {
                        p.SetValue(_state, value);
                        return;
                    }
                }
                catch { }
            }
        }
    }
}
