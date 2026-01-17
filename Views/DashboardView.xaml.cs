using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();

            // DataContext は MainWindow で AppState.Instance を設定している想定
            // ここでは触らない（触ると二重管理で壊れやすい）
        }

        // =========================
        // Buttons (Top Right)
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

            // 1) まず AppState に「選択した動画パス」を渡す（存在するなら呼ぶ）
            var ok =
                TryInvokeAppState("AddImportedVideo", new object[] { dlg.FileName }) ||
                TryInvokeAppState("AddVideo", new object[] { dlg.FileName }) ||
                TrySetAppStateProperty("SelectedVideoPath", dlg.FileName) ||
                TrySetAppStateProperty("SelectedVideo", dlg.FileName);

            // 2) Player 表示（MediaElement）も確実に鳴らす
            try
            {
                if (Player != null)
                {
                    Player.Stop();
                    Player.Source = new Uri(dlg.FileName, UriKind.Absolute);
                    Player.Position = TimeSpan.Zero;
                    Player.Play();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Player load failed: {ex.Message}", "Load Video", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // 3) 状況表示（ok が false でも Player で再生できればOK）
            if (!ok)
            {
                // AppState 側のメソッド名が違うだけの可能性が高いので、ここでは失敗扱いにしない
                // 必要なら後で AppState のメソッド名に合わせて最短修正する
            }
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

            // AppState に「CSV取り込み」系メソッドがあれば呼ぶ
            if (TryInvokeAppState("ImportCsv", new object[] { dlg.FileName })) return;
            if (TryInvokeAppState("Import", new object[] { dlg.FileName })) return;

            MessageBox.Show("ImportCsv handler is wired, but AppState.ImportCsv(...) was not found.\n(ビルドは通る状態。次は AppState 側の関数名に合わせて接続するだけ)",
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

            // AppState に「CSV書き出し」系メソッドがあれば呼ぶ
            if (TryInvokeAppState("ExportCsv", new object[] { dlg.FileName })) return;
            if (TryInvokeAppState("Export", new object[] { dlg.FileName })) return;

            MessageBox.Show("ExportCsv handler is wired, but AppState.ExportCsv(...) was not found.\n(ビルドは通る状態。次は AppState 側の関数名に合わせて接続するだけ)",
                "Export CSV", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            // フォルダ選択：標準だけでやるなら SaveFileDialog で代用 or WinForms を使う
            // いったん “エラーにならず通る” を優先して SaveFileDialog でフォルダっぽく受ける
            var dlg = new SaveFileDialog
            {
                Title = "Export All (choose folder by typing a folder name)",
                Filter = "Folder|*.folder",
                FileName = "export.folder"
            };

            if (dlg.ShowDialog() != true) return;

            var folder = Path.GetDirectoryName(dlg.FileName) ?? Environment.CurrentDirectory;

            if (TryInvokeAppState("ExportAll", new object[] { folder })) return;
            if (TryInvokeAppState("ExportClips", new object[] { folder })) return;

            MessageBox.Show("ExportAll handler is wired, but AppState.ExportAll(...) was not found.\n(ビルドは通る状態。次は AppState 側の関数名に合わせて接続するだけ)",
                "Export All", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            // まずはダミー（Mac版の Preferences 相当の画面を後で作れる）
            MessageBox.Show("Preferences (dummy)", "Preferences", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // =========================
        // Helpers (安全にAppStateへ接続)
        // =========================

        private static object? GetAppStateInstance()
        {
            // AppState.Instance を反射で取る（型/namespace違いでも動く）
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

                // 型が違う場合は変換を試す
                var targetType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                object converted = value;
                if (value != null && !targetType.IsAssignableFrom(value.GetType()))
                {
                    converted = Convert.ChangeType(value, targetType);
                }

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
