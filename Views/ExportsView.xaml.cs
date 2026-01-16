using Microsoft.Win32;
using System;
using System.IO;
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
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = AppState.Instance;

                if (string.IsNullOrWhiteSpace(app.SelectedVideoPath))
                {
                    MessageBox.Show("Clips で動画を選択してね（仮）", "Exports",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var defaultName = $"export_dummy_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                var dlg = new SaveFileDialog
                {
                    Title = "Export (dummy)",
                    FileName = defaultName,
                    Filter = "Text file|*.txt|All files|*.*",
                    AddExtension = true,
                    OverwritePrompt = true
                };

                if (dlg.ShowDialog() != true) return;

                var content =
                    $"SelectedVideoPath={app.SelectedVideoPath}\n" +
                    $"SelectedVideoName={app.SelectedVideoName}\n" +
                    $"PlaybackSeconds={app.PlaybackSeconds}\n" +
                    $"Duration={app.PlaybackDuration}\n" +
                    $"TagsCount={app.Tags.Count}\n";

                File.WriteAllText(dlg.FileName, content);

                app.StatusMessage = $"Exported: {Path.GetFileName(dlg.FileName)}";

                MessageBox.Show($"Exported:\n{dlg.FileName}", "Exports",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exports", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
