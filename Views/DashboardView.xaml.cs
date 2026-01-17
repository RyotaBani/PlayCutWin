using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        private readonly string _teamAPlaceholder = "Home / Our Team";
        private readonly string _teamBPlaceholder = "Away / Opponent";

        private bool _teamAIsPlaceholder = true;
        private bool _teamBIsPlaceholder = true;

        private string? _currentVideoPath;

        public DashboardView()
        {
            InitializeComponent();

            // 初期表示（薄い文字っぽく見せる）
            ApplyPlaceholder(TeamATextBox, _teamAPlaceholder, ref _teamAIsPlaceholder);
            ApplyPlaceholder(TeamBTextBox, _teamBPlaceholder, ref _teamBIsPlaceholder);

            UpdateVideoUI(null);
        }

        // =========================
        // Video UI
        // =========================

        private void UpdateVideoUI(string? videoPath)
        {
            _currentVideoPath = videoPath;

            if (string.IsNullOrWhiteSpace(videoPath))
            {
                VideoTitle.Text = "Video (16:9)";
                ClipsVideoPathText.Text = "No video loaded";
                TagsVideoPathText.Text = "";
                NoVideoText.Visibility = Visibility.Visible;
                return;
            }

            var name = Path.GetFileName(videoPath);
            VideoTitle.Text = name; // 長い時は XAML側で Ellipsis
            ClipsVideoPathText.Text = videoPath;
            TagsVideoPathText.Text = videoPath;
            NoVideoText.Visibility = Visibility.Collapsed;
        }

        // =========================
        // MediaElement events
        // =========================

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                Player.LoadedBehavior = MediaState.Manual;
                Player.UnloadedBehavior = MediaState.Manual;

                Player.Position = TimeSpan.Zero;
                Player.Play();

                // 総時間更新
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var total = Player.NaturalDuration.HasTimeSpan ? Player.NaturalDuration.TimeSpan : TimeSpan.Zero;
                    TimeText.Text = $"{FormatTime(Player.Position)}  /  {FormatTime(total)}";
                }), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"MediaOpened but Play failed:\n{ex.Message}", "Player", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Player_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            var msg = e.ErrorException?.Message ?? "(unknown)";
            MessageBox.Show(
                $"MediaFailed:\n{msg}\n\n※この場合はコーデック/形式の可能性があります。\n(H.265/HEVC は WPF 標準だと失敗しがち)\nまずは H.264 の mp4 でも試してみて。",
                "Player",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        // =========================
        // Top buttons
        // =========================

        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            var path = dlg.FileName;

            // UIの更新（ファイル名/パス）
            UpdateVideoUI(path);

            // AppStateへ渡す（関数名が違っても落ちないように）
            _ =
                TryInvokeAppState("AddImportedVideo", new object[] { path }) ||
                TryInvokeAppState("AddVideo", new object[] { path }) ||
                TrySetAppStateProperty("SelectedVideoPath", path) ||
                TrySetAppStateProperty("SelectedVideo", path);

            // Playerにセット（PlayはMediaOpenedで行う）
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Player.LoadedBehavior = MediaState.Manual;
                    Player.UnloadedBehavior = MediaState.Manual;

                    Player.Stop();
                    Player.Source = new Uri(path, UriKind.Absolute);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Player load failed:\n{ex.Message}", "Load Video", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }), DispatcherPriority.Background);
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import CSV",
                Filter = "CSV|*.csv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            var path = dlg.FileName;

            if (TryInvokeAppState("ImportCsv", new object[] { path })) return;
            if (TryInvokeAppState("Import", new object[] { path })) return;

            MessageBox.Show(
                "Import CSV はUI側は接続済み。\nただし AppState 側に ImportCsv/Import が見つからなかった。\n(AppStateの関数名に合わせればすぐ実働化できる)",
                "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export CSV",
                Filter = "CSV|*.csv|All Files|*.*",
                FileName = "export.csv"
            };

            if (dlg.ShowDialog() != true) return;

            var path = dlg.FileName;

            if (TryInvokeAppState("ExportCsv", new object[] { path })) return;
            if (TryInvokeAppState("Export", new object[] { path })) return;

            MessageBox.Show(
                "Export CSV はUI側は接続済み。\nただし AppState 側に ExportCsv/Export が見つからなかった。\n(AppStateの関数名に合わせればすぐ実働化できる)",
                "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            // フォルダ選択UIは後でちゃんとやる。いまは“保存先を選ばせる”で代用。
            var dlg = new SaveFileDialog
            {
                Title = "Export All (choose destination folder by selecting a file name)",
                Filter = "Folder|*.folder",
                FileName = "export.folder"
            };

            if (dlg.ShowDialog() != true) return;

            var folder = Path.GetDirectoryName(dlg.FileName) ?? Environment.CurrentDirectory;

            if (TryInvokeAppState("ExportAll", new object[] { folder })) return;
            if (TryInvokeAppState("ExportClips", new object[] { folder })) return;

            MessageBox.Show(
                "Export All はUI側は接続済み。\nただし AppState 側に ExportAll/ExportClips が見つからなかった。\n(AppStateの関数名に合わせればすぐ実働化できる)",
                "Export All", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences (dummy)", "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // =========================
        // Controls buttons
        // =========================

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            try { Player?.Play(); }
            catch { }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            try { Player?.Pause(); }
            catch { }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player?.Stop();
                UpdateTimeText();
            }
            catch { }
        }

        private void Minus05_Click(object sender, RoutedEventArgs e)
        {
            SeekBy(TimeSpan.FromSeconds(-0.5));
        }

        private void Plus05_Click(object sender, RoutedEventArgs e)
        {
            SeekBy(TimeSpan.FromSeconds(0.5));
        }

        private void SeekBy(TimeSpan delta)
        {
            try
            {
                if (Player == null) return;

                var pos = Player.Position + delta;
                if (pos < TimeSpan.Zero) pos = TimeSpan.Zero;

                Player.Position = pos;
                UpdateTimeText();
            }
            catch { }
        }

        private void UpdateTimeText()
        {
            var total = (Player != null && Player.NaturalDuration.HasTimeSpan)
                ? Player.NaturalDuration.TimeSpan
                : TimeSpan.Zero;

            var cur = Player?.Position ?? TimeSpan.Zero;
            TimeText.Text = $"{FormatTime(cur)}  /  {FormatTime(total)}";
        }

        private static string FormatTime(TimeSpan t)
        {
            // mm:ss
            if (t.TotalHours >= 1)
                return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}";
            return $"{t.Minutes:00}:{t.Seconds:00}";
        }

        // =========================
        // Team placeholder behavior
        // =========================

        private void TeamATextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RemovePlaceholder(TeamATextBox, _teamAPlaceholder, ref _teamAIsPlaceholder);
        }

        private void TeamATextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyPlaceholderIfEmpty(TeamATextBox, _teamAPlaceholder, ref _teamAIsPlaceholder);
            TrySetAppStateProperty("TeamAName", GetTeamAName());
        }

        private void TeamBTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            RemovePlaceholder(TeamBTextBox, _teamBPlaceholder, ref _teamBIsPlaceholder);
        }

        private void TeamBTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyPlaceholderIfEmpty(TeamBTextBox, _teamBPlaceholder, ref _teamBIsPlaceholder);
            TrySetAppStateProperty("TeamBName", GetTeamBName());
        }

        private string GetTeamAName() => _teamAIsPlaceholder ? "" : (TeamATextBox.Text ?? "");
        private string GetTeamBName() => _teamBIsPlaceholder ? "" : (TeamBTextBox.Text ?? "");

        private static void ApplyPlaceholder(TextBox box, string placeholder, ref bool flag)
        {
            flag = true;
            box.Text = placeholder;
            box.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
        }

        private static void RemovePlaceholder(TextBox box, string placeholder, ref bool flag)
        {
            if (!flag) return;
            flag = false;
            box.Text = "";
            box.Foreground = new SolidColorBrush(Color.FromRgb(237, 237, 237));
        }

        private static void ApplyPlaceholderIfEmpty(TextBox box, string placeholder, ref bool flag)
        {
            var txt = box.Text ?? "";
            if (string.IsNullOrWhiteSpace(txt))
            {
                ApplyPlaceholder(box, placeholder, ref flag);
            }
            else
            {
                flag = false;
                box.Foreground = new SolidColorBrush(Color.FromRgb(237, 237, 237));
            }
        }

        // =========================
        // AppState safe bridge
        // =========================

        private static object? GetAppStateInstance()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var t = asm.GetTypes().FirstOrDefault(x => x.Name == "AppState");
            if (t == null) return null;

            var p = t.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            return p?.GetValue(null);
        }

        private static bool TryInvokeAppState(string methodName, object[] args)
        {
            try
            {
                var inst = GetAppStateInstance();
                if (inst == null) return false;

                var t = inst.GetType();
                var m = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                if (m == null) return false;

                m.Invoke(inst, args);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySetAppStateProperty(string propName, object value)
        {
            try
            {
                var inst = GetAppStateInstance();
                if (inst == null) return false;

                var t = inst.GetType();
                var p = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null || !p.CanWrite) return false;

                var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                object converted = value;
                if (value != null && !targetType.IsAssignableFrom(value.GetType()))
                    converted = Convert.ChangeType(value, targetType);

                p.SetValue(inst, converted);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
