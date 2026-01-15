using System;
using System.IO;
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
            DataContext = PlayCutWin.AppState.Instance;
            UpdateSelectedText();
        }

        private void UpdateSelectedText()
        {
            var path = PlayCutWin.AppState.Instance.SelectedVideoPath ?? "";
            SelectedVideoText.Text = string.IsNullOrWhiteSpace(path) ? "(none)" : path;
        }

        // XAMLのボタン Click="ExportDummy_Click" と一致させる
        private void ExportDummy_Click(object sender, RoutedEventArgs e)
        {
            UpdateSelectedText();

            var app = PlayCutWin.AppState.Instance;
            var src = app.SelectedVideoPath;

            if (string.IsNullOrWhiteSpace(src))
            {
                MessageBox.Show("先に Import で動画を選んでね", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var defaultName = Path.GetFileNameWithoutExtension(src);

            var dlg = new SaveFileDialog
            {
                Title = "Export (dummy) - Save as",
                FileName = $"{defaultName}_export.txt",
                Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = ".txt",
                AddExtension = true
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var start = app.ClipStart.HasValue ? PlayCutWin.AppState.FmtMMSS(app.ClipStart.Value) : "--:--";
                var end   = app.ClipEnd.HasValue   ? PlayCutWin.AppState.FmtMMSS(app.ClipEnd.Value)   : "--:--";

                var content =
$@"PlayCut Export (dummy)
Source: {src}
Range : {start} - {end}
Time  : {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";

                File.WriteAllText(dlg.FileName, content);

                app.StatusMessage = $"Exported (dummy): {Path.GetFileName(dlg.FileName)}";
                MessageBox.Show("ダミー書き出しOK（txt保存）", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                app.StatusMessage = $"Export failed: {ex.Message}";
                MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
