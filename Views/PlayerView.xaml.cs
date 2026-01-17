using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class PlayerView : UserControl
    {
        private readonly object _state; // AppState.Instance
        private INotifyPropertyChanged? _npc;

        private string? _currentVideoPath;
        private bool _pauseOnOpened;

        public PlayerView()
        {
            InitializeComponent();

            _state = GetAppStateInstance();
            DataContext = _state;

            _npc = _state as INotifyPropertyChanged;
            if (_npc != null) _npc.PropertyChanged += State_PropertyChanged;

            SyncFromState();
        }

        private object GetAppStateInstance()
        {
            try
            {
                var t = Type.GetType("PlayCutWin.AppState, PlayCutWin");
                if (t == null) return new object();

                var p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                return p?.GetValue(null) ?? new object();
            }
            catch
            {
                return new object();
            }
        }

        private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 軽いのでまとめて同期
            Dispatcher.Invoke(SyncFromState);
        }

        private void SyncFromState()
        {
            // video path
            var videoPath = GetSelectedVideoPath();

            // タイトル表示
            if (string.IsNullOrWhiteSpace(videoPath))
            {
                VideoTitleText.Text = "Video (16:9)";
                VideoPlaceholderText.Visibility = Visibility.Visible;
                StopPlayerIfNeeded();
            }
            else
            {
                VideoTitleText.Text = Path.GetFileName(videoPath);
                VideoPlaceholderText.Visibility = Visibility.Collapsed;

                // 変化したときだけロード
                if (!string.Equals(_currentVideoPath, videoPath, StringComparison.OrdinalIgnoreCase))
                {
                    _currentVideoPath = videoPath;
                    LoadToMediaElement(videoPath);
                }
            }

            // (no clip selected)
            ClipHintText.Text = IsClipSelected() ? "" : "(no clip selected)";

            // Team A/B (AppStateに値があれば反映)
            var teamA = TryGetString("TeamAName") ?? TryGetString("TeamA") ?? TryGetString("HomeTeam");
            var teamB = TryGetString("TeamBName") ?? TryGetString("TeamB") ?? TryGetString("AwayTeam");

            if (!string.IsNullOrWhiteSpace(teamA) && TeamATextBox.Text != teamA)
                TeamATextBox.Text = teamA;

            if (!string.IsNullOrWhiteSpace(teamB) && TeamBTextBox.Text != teamB)
                TeamBTextBox.Text = teamB;
        }

        private void LoadToMediaElement(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    VideoPlaceholderText.Text = "Video file not found";
                    VideoPlaceholderText.Visibility = Visibility.Visible;
                    return;
                }

                // いったん停止して差し替え
                try { Player.Stop(); } catch { }

                Player.Source = new Uri(path, UriKind.Absolute);

                // “表示されない”対策：MediaOpenedで一瞬Play→Pauseしてフレーム出す
                _pauseOnOpened = true;
                Player.Play();
            }
            catch
            {
                VideoPlaceholderText.Text = "Failed to load video";
                VideoPlaceholderText.Visibility = Visibility.Visible;
            }
        }

        private void StopPlayerIfNeeded()
        {
            _currentVideoPath = null;
            try
            {
                Player.Stop();
                Player.Source = null;
            }
            catch { }
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (_pauseOnOpened)
            {
                _pauseOnOpened = false;
                try
                {
                    Player.Position = TimeSpan.Zero;
                    Player.Pause(); // 先頭フレーム表示
                }
                catch { }
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            VideoPlaceholderText.Text = "Failed to play video";
            VideoPlaceholderText.Visibility = Visibility.Visible;
        }

        private string? GetSelectedVideoPath()
        {
            // よくある名前揺れ
            var s =
                TryGetString("SelectedVideoPath")
                ?? TryGetString("CurrentVideoPath")
                ?? TryGetString("SelectedVideoText");

            if (!string.IsNullOrWhiteSpace(s)) return s;

            // SelectedVideo がオブジェクトなら Path を探す
            try
            {
                var v = TryGetObject("SelectedVideo");
                if (v == null) return null;

                var tp =
                    v.GetType().GetProperty("Path")
                    ?? v.GetType().GetProperty("FilePath")
                    ?? v.GetType().GetProperty("FullPath");

                return tp?.GetValue(v) as string;
            }
            catch
            {
                return null;
            }
        }

        private bool IsClipSelected()
        {
            try
            {
                var clip = TryGetObject("SelectedClip");
                if (clip != null) return true;

                var id = TryGetObject("SelectedClipId");
                if (id != null)
                {
                    var ss = id.ToString();
                    if (!string.IsNullOrWhiteSpace(ss) && ss != "0") return true;
                }

                var idxObj = TryGetObject("SelectedClipIndex");
                if (idxObj is int idx && idx >= 0) return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string? TryGetString(string propName)
        {
            try
            {
                var p = _state.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                return p?.GetValue(_state) as string;
            }
            catch { return null; }
        }

        private object? TryGetObject(string propName)
        {
            try
            {
                var p = _state.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                return p?.GetValue(_state);
            }
            catch { return null; }
        }

        private void TrySetString(string propName, string value)
        {
            try
            {
                var p = _state.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                    p.SetValue(_state, value);
            }
            catch { }
        }

        private void TeamATextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var v = TeamATextBox.Text ?? "";
            TrySetString("TeamAName", v);
            TrySetString("TeamA", v);
            TrySetString("HomeTeam", v);
        }

        private void TeamBTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var v = TeamBTextBox.Text ?? "";
            TrySetString("TeamBName", v);
            TrySetString("TeamB", v);
            TrySetString("AwayTeam", v);
        }
    }
}
