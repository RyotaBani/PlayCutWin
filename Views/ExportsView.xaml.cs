// Views/ExportsView.xaml.cs（完全置き換え）
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        public ExportsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
        }

        // （XAML側で Click="ChooseFolder_Click" になっている想定）
        private void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            // WPF標準：フォルダ選択ダイアログが無いので、
            // ここでは「保存先ファイル」を選ばせてフォルダとして扱う（安全にビルド通る）
            var dlg = new SaveFileDialog
            {
                Title = "Export destination (dummy)",
                Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = "PlayCut_export_dummy.txt"
            };

            if (dlg.ShowDialog() == true)
            {
                // ここではファイルパスを AppState に保存しておく（プロパティが無くても落ちない）
                TrySetStringProperty(AppState.Instance, "ExportPath", dlg.FileName);
                TrySetStringProperty(AppState.Instance, "StatusMessage", $"Export path set: {dlg.FileName}");
            }
        }

        // （XAML側で Click="ExportSelected_Click" になっている想定）
        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            var app = AppState.Instance;

            var selectedVideo = GetStringProperty(app, "SelectedVideoPath");
            if (string.IsNullOrWhiteSpace(selectedVideo))
            {
                MessageBox.Show("先に Clips で動画を選択してね（仮）", "Exports",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ExportPath があればそこへ、なければ都度聞く
            var exportPath = GetStringProperty(app, "ExportPath");
            if (string.IsNullOrWhiteSpace(exportPath))
            {
                var dlg = new SaveFileDialog
                {
                    Title = "Export destination (dummy)",
                    Filter = "Text File (*.txt)|*.txt|All Files (*.*)|*.*",
                    FileName = "PlayCut_export_dummy.txt"
                };

                if (dlg.ShowDialog() != true) return;
                exportPath = dlg.FileName;
                TrySetStringProperty(app, "ExportPath", exportPath);
            }

            try
            {
                // まずは “書き出し導線が動く” ことが目的なので、ダミーのテキストを書き出す
                var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var content =
$@"PlayCut Export (dummy)
Time: {now}
SelectedVideoPath:
{selectedVideo}
";

                Directory.CreateDirectory(Path.GetDirectoryName(exportPath)!);
                File.WriteAllText(exportPath, content);

                MessageBox.Show($"Exported (dummy):\n{exportPath}", "Exports",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                TrySetStringProperty(app, "StatusMessage", $"Exported(dummy): {Path.GetFileName(exportPath)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
                TrySetStringProperty(app, "StatusMessage", $"Export failed: {ex.Message}");
            }
        }

        // （XAML側で SelectionChanged="ClipsGrid_SelectionChanged" とかになってても落ちないように）
        private void ClipsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ここは将来「書き出すクリップ選択」に使う想定。
            // いまはビルド通す＆状態更新だけ。
            TrySetStringProperty(AppState.Instance, "StatusMessage", "Clip selection changed (dummy)");
        }

        // ---- helpers（AppStateの実装差を吸収） ----

        private static object? GetPropertyValue(object obj, string name)
        {
            var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(obj);
        }

        private static string? GetStringProperty(object obj, string name)
        {
            return GetPropertyValue(obj, name) as string;
        }

        private static void TrySetStringProperty(object obj, string name, string value)
        {
            try
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                {
                    p.SetValue(obj, value);
                }
            }
            catch { }
        }
    }
}
