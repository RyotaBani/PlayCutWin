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

        private readonly DispatcherTimer _uiTimer = new DispatcherTimer();

        public DashboardView()
        {
            InitializeComponent();

            // placeholder
            ApplyPlaceholder(TeamATextBox, _teamAPlaceholder, ref _teamAIsPlaceholder);
            ApplyPlaceholder(TeamBTextBox, _teamBPlaceholder, ref _teamBIsPlaceholder);

            UpdateVideoUI(null);

            // timer: 再生中はPosition更新を見える化（= 再生できてるか即わかる）
            _uiTimer.Interval = TimeSpan.FromMilliseconds(200);
            _uiTimer.Tick += (_, __) => UpdateTimeText();
            _uiTimer.Start();

            // Player初期化（重要）
            Player.LoadedBehavior = MediaState.Manual;
            Player.UnloadedBehavior = MediaState.Manual;
            Player.Volume = 1.0;
            Player.SpeedRatio = 1.0;
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
                TimeText.Text = "00:00  /  00:00";
                return;
            }

            var name = Path.GetFileName(videoPath);
            VideoTitle.Text = name; // XAMLのEllipsisで省略
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
                // MediaOpenedが来た時点で再生を強制
                Player.LoadedBehavior = MediaState.Manual;
                Player.UnloadedBehavior = MediaState.Manual;

                Player.Volume = 1.0;
                Player.SpeedRatio = 1.0;

                // ここでPlay（来たり来なかったりする環境があるので保険）
                Player.Play();

                UpdateTimeText();
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
                $"MediaFailed:\n{msg}\n\n※WPF MediaElement はPCのコーデック依存です。\nH.265/HEVC だと失敗しがち。\nまずは H.264 の mp4 で試してみて。",
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

            UpdateVideoUI(path);

            // AppStateへ（あれば）
            _ =
                TryInvokeAppState("AddImportedVideo", new object[] { path }) ||
                TryInvokeAppState("AddVideo", new object[] { path }) ||
                TrySetAppStateProperty("SelectedVideoPath", path) ||
                TrySetAppStateProperty("SelectedVideo", path);

            // ★ここが肝：Sourceを一回null→再セット→即Play（MediaOpened待ちだけにしない）
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    Player.LoadedBehavior = MediaState.Manual;
                    Player.UnloadedBehavior = MediaState.Manual;

                    Player.Stop();
                    Player.Source = null;               // 重要：同一動画再セット問題の回避
                    Player.Position = TimeSpan.Zero;

                    Player.Source = new Uri(path, UriKind.Absolute);

                    // MediaOpenedが来ない環境があるので、ここでもPlay叩く（保険）
                    Player.Play();

                    UpdateTimeText();
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
                "Import CSV はUI側は接続済み。\nただし AppState 側に ImportCsv/Import が見つからなかった。",
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
                "Export CSV はUI側は接続済み。\nただし AppState 側に ExportCsv/Export が見つからなかった。",
                "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export All (choose folder by selecting file name)",
                Filter = "Folder|*.folder",
                FileName = "export.folder"
            };

            if (dlg.ShowDialog() != true) return;

            var folder = Path.GetDirectoryName(dlg.FileName) ?? Environment.CurrentDirectory;

            if (TryInvokeAppState("ExportAll", new object[] { folder })) return;
            if (TryInvokeAppState("ExportClips", new object[] { folder })) return;

            MessageBox.Show(
                "Export All はUI側は接続済み。\nただし AppState 側に ExportAll/ExportClips が見つからなかった。",
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
            try
            {
                if (Player.Source == null && !string.IsNullOrWhiteSpace(_currentVideoPath))
                {
                    Player.Source = new Uri(_currentVideoPath!, UriKind.Absolute);
                }

                Player.LoadedBehavior = MediaState.Manual;
                Player.UnloadedBehavior = MediaState.Manual;

                Player.Play();
                UpdateTimeText();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Play failed:\n{ex.Message}", "Player", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            try { Player?.Pause(); } catch { }
            UpdateTimeText();
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Player?.Stop();
                Player.Position = TimeSpan.Zero;
            }
            catch { }
            UpdateTimeText();
        }

        private void Minus05_Click(object sender, RoutedEventArgs e) => SeekBy(TimeSpan.FromSeconds(-0.5));
        private void Plus05_Click(object sender, RoutedEventArgs e) => SeekBy(TimeSpan.FromSeconds(0.5));

        private void SeekBy(TimeSpan delta)
        {
            try
            {
                if (Player.Source == null) return;

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
