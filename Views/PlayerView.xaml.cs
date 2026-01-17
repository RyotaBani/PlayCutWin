using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class PlayerView : UserControl
    {
        private readonly object _state; // AppState.Instance
        private INotifyPropertyChanged? _npc;

        public PlayerView()
        {
            InitializeComponent();

            _state = GetAppStateInstance();
            this.DataContext = _state;

            _npc = _state as INotifyPropertyChanged;
            if (_npc != null) _npc.PropertyChanged += State_PropertyChanged;

            // 初期反映
            SyncFromState();
        }

        private object GetAppStateInstance()
        {
            // AppState.Instance を取りにいく（型が見つからなくても落ちない）
            try
            {
                var t = Type.GetType("PlayCutWin.AppState, PlayCutWin");
                if (t == null) return new object();

                var p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var inst = p?.GetValue(null);
                return inst ?? new object();
            }
            catch
            {
                return new object();
            }
        }

        private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 雑にまとめて同期（軽いのでOK）
            SyncFromState();
        }

        private void SyncFromState()
        {
            // 1) Video title: no video -> "Video (16:9)" / loaded -> filename
            var videoPath = GetSelectedVideoPath();
            if (string.IsNullOrWhiteSpace(videoPath))
            {
                VideoTitleText.Text = "Video (16:9)";
                VideoPlaceholderText.Text = "Load video from the button →";
            }
            else
            {
                VideoTitleText.Text = Path.GetFileName(videoPath);
                VideoPlaceholderText.Text = ""; // ここは実プレイヤー入れたら消える想定
            }

            // 2) (no clip selected)
            ClipHintText.Text = IsClipSelected() ? "" : "(no clip selected)";

            // 3) Team A/B text (もし AppState 側に値があれば反映)
            var teamA = TryGetString("TeamAName") ?? TryGetString("TeamA") ?? TryGetString("HomeTeam");
            var teamB = TryGetString("TeamBName") ?? TryGetString("TeamB") ?? TryGetString("AwayTeam");

            // TextBox に「未入力」なら空のままにする（ウォーターマークで薄く表示）
            if (!string.IsNullOrWhiteSpace(teamA) && TeamATextBox.Text != teamA)
                TeamATextBox.Text = teamA;

            if (!string.IsNullOrWhiteSpace(teamB) && TeamBTextBox.Text != teamB)
                TeamBTextBox.Text = teamB;
        }

        private string? GetSelectedVideoPath()
        {
            // よくある名前揺れを吸収
            string? s =
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
            // SelectedClip / SelectedClipId / SelectedClipIndex あたりを雑に判定
            try
            {
                var clip = TryGetObject("SelectedClip");
                if (clip != null) return true;

                var id = TryGetObject("SelectedClipId");
                if (id != null)
                {
                    var s = id.ToString();
                    if (!string.IsNullOrWhiteSpace(s) && s != "0") return true;
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

        // ===== Team A/B: 入力があれば AppState に反映（あれば） =====

        private void TeamATextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var v = TeamATextBox.Text ?? "";
            // 未入力は空のまま（ウォーターマーク表示）
            // 反映先が存在するなら反映
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
