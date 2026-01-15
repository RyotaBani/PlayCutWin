using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        public ExportsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
            Loaded += (_, __) => RefreshSelectedLabel();
        }

        private void RefreshSelectedLabel()
        {
            // x:Name があれば表示更新（無ければ何もしない）
            var tb = this.FindName("SelectedVideoText") as TextBlock;
            if (tb == null) return;

            var path = GetString(AppState.Instance, "SelectedVideoPath")
                       ?? GetString(AppState.Instance, "SelectedPath")
                       ?? "(no selected)";
            tb.Text = path;
        }

        // XAML: Button Click="ChooseFolder_Click"（あれば）
        private void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            // ここは “仮”：フォルダ選択UIはあとで本実装でもOK
            // とりあえず AppState.ExportFolder に入れる互換だけ用意
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            TrySetString(AppState.Instance, "ExportFolder", folder);
            TrySetString(AppState.Instance, "StatusMessage", $"Export folder: {folder}");
        }

        // ✅ XAML: Button Click="ExportDummy_Click" ←これが無くて落ちてた
        private void ExportDummy_Click(object sender, RoutedEventArgs e)
        {
            var folder = GetString(AppState.Instance, "ExportFolder")
                         ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            var selected = GetString(AppState.Instance, "SelectedVideoPath")
                           ?? GetString(AppState.Instance, "SelectedPath")
                           ?? "";

            // ダミー出力：export.txt を作る（実クリップ書き出しは次フェーズ）
            var outPath = Path.Combine(folder, "export.txt");
            File.WriteAllText(outPath,
                $"Export(dummy)\nSelected:\n{selected}\nTime:{DateTime.Now}\n");

            MessageBox.Show($"Exported (dummy):\n{outPath}", "Exports",
                MessageBoxButton.OK, MessageBoxImage.Information);

            TrySetString(AppState.Instance, "StatusMessage", $"Exported(dummy): {outPath}");
        }

        // XAMLで参照されてる可能性があるので “保険” で置いとく（無害）
        private void ExportSelected_Click(object sender, RoutedEventArgs e) => ExportDummy_Click(sender, e);

        // ---- reflection helpers ----
        private static object? GetProperty(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);

        private static string? GetString(object obj, string name)
            => GetProperty(obj, name) as string;

        private static bool TrySetString(object obj, string name, string value)
        {
            try
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                {
                    p.SetValue(obj, value);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
