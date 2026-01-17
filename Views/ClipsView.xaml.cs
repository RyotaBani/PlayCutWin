using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private readonly AppState _state;

        public ClipsView()
        {
            InitializeComponent();
            _state = AppState.Instance;
            DataContext = _state;

            RefreshHeader();
        }

        private void RefreshHeader()
        {
            // タイトル：Clips (Total X)
            // AppState に Clips / AllClips などがあれば拾う。なければ 0。
            int total = 0;

            total = TryCount("Clips")
                ?? TryCount("AllClips")
                ?? TryCount("TeamAClips")
                ?? TryCount("TeamBClips")
                ?? 0;

            ClipsTitleText.Text = $"Clips (Total {total})";

            // Loaded video 表示
            var path = GetSelectedVideoPath();
            LoadedVideoText.Text = string.IsNullOrWhiteSpace(path) ? "No video loaded" : Path.GetFileName(path);

            // Export All は「今はダミー」なので、クリップがあるっぽい時だけ有効化
            ExportAllButton.IsEnabled = total > 0;
        }

        private int? TryCount(string propName)
        {
            try
            {
                var p = _state.GetType().GetProperty(propName);
                if (p == null) return null;
                var v = p.GetValue(_state);
                if (v == null) return null;

                // IEnumerable の Count を取る
                if (v is System.Collections.IEnumerable en)
                {
                    int c = 0;
                    foreach (var _ in en) c++;
                    return c;
                }

                return null;
            }
            catch { return null; }
        }

        private string? GetSelectedVideoPath()
        {
            // 既存の AppState の名前揺れに対応
            // SelectedVideoPath / CurrentVideoPath / SelectedVideoText / SelectedVideo(Path)
            string? s = TryGetString("SelectedVideoPath")
                     ?? TryGetString("CurrentVideoPath")
                     ?? TryGetString("SelectedVideoText");

            if (!string.IsNullOrWhiteSpace(s)) return s;

            // SelectedVideo が VideoItem なら Path を探す
            try
            {
                var p = _state.GetType().GetProperty("SelectedVideo");
                var v = p?.GetValue(_state);
                if (v == null) return null;

                var tp = v.GetType().GetProperty("Path") ?? v.GetType().GetProperty("FilePath") ?? v.GetType().GetProperty("FullPath");
                return tp?.GetValue(v) as string;
            }
            catch { return null; }
        }

        private string? TryGetString(string propName)
        {
            try
            {
                var p = _state.GetType().GetProperty(propName);
                if (p == null) return null;
                return p.GetValue(_state) as string;
            }
            catch { return null; }
        }

        // ===== Buttons =====

        private void LoadVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Load Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                // AppState に「動画追加」系があるなら使う（AddImportedVideo / AddVideo 等）
                InvokeIfExists("AddImportedVideo", dlg.FileName);
                InvokeIfExists("AddVideo", dlg.FileName);

                // 可能なら選択もセット（SetSelected/SetSelectedVideo）
                InvokeIfExists("SetSelected", FindVideoItemOrPath(dlg.FileName));
                InvokeIfExists("SetSelectedVideo", FindVideoItemOrPath(dlg.FileName));

                // 最低限、SelectedVideoPath があれば直接入れる
                SetStringIfExists("SelectedVideoPath", dlg.FileName);
                SetStringIfExists("CurrentVideoPath", dlg.FileName);
                SetStringIfExists("SelectedVideoText", dlg.FileName);

                RefreshHeader();
            }
        }

        private object FindVideoItemOrPath(string path)
        {
            // ImportedVideos から該当 VideoItem を探す。なければ path を返す。
            try
            {
                var p = _state.GetType().GetProperty("ImportedVideos");
                var list = p?.GetValue(_state) as System.Collections.IEnumerable;
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        var tp = item.GetType().GetProperty("Path") ?? item.GetType().GetProperty("FilePath") ?? item.GetType().GetProperty("FullPath");
                        var v = tp?.GetValue(item) as string;
                        if (!string.IsNullOrWhiteSpace(v) && string.Equals(v, path, StringComparison.OrdinalIgnoreCase))
                            return item;
                    }
                }
            }
            catch { }
            return path;
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import CSV",
                Filter = "CSV Files|*.csv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                // AppState に ImportCSV っぽいメソッドがあれば呼ぶ
                if (!InvokeIfExists("ImportCsv", dlg.FileName))
                    if (!InvokeIfExists("ImportCSV", dlg.FileName))
                        MessageBox.Show("ImportCsv method not found in AppState (stub).", "Import CSV", MessageBoxButton.OK, MessageBoxImage.Information);

                RefreshHeader();
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export CSV",
                Filter = "CSV Files|*.csv|All Files|*.*",
                FileName = "tags.csv"
            };

            if (dlg.ShowDialog() == true)
            {
                if (!InvokeIfExists("ExportCsv", dlg.FileName))
                    if (!InvokeIfExists("ExportCSV", dlg.FileName))
                        MessageBox.Show("ExportCsv method not found in AppState (stub).", "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            // ここは後で本実装（クリップ書き出し）
            MessageBox.Show("Export All (stub).", "Export All", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ===== Reflection helpers =====

        private bool InvokeIfExists(string methodName, params object[] args)
        {
            try
            {
                var m = _state.GetType().GetMethods().FirstOrDefault(x => x.Name == methodName && x.GetParameters().Length == args.Length);
                if (m == null) return false;
                m.Invoke(_state, args);
                return true;
            }
            catch { return false; }
        }

        private void SetStringIfExists(string propName, string value)
        {
            try
            {
                var p = _state.GetType().GetProperty(propName);
                if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                    p.SetValue(_state, value);
            }
            catch { }
        }
    }
}
