using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        private string _folder = "";

        public ExportsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
            UpdateFolderText();
        }

        private void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var dlg = new Forms.FolderBrowserDialog();
                dlg.Description = "Choose export folder";
                dlg.UseDescriptionForTitle = true;

                var result = dlg.ShowDialog();
                if (result == Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
                {
                    _folder = dlg.SelectedPath;
                    UpdateFolderText();
                    AppState.Instance.StatusMessage = $"Export folder: {_folder}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exports", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var app = AppState.Instance;
                if (string.IsNullOrWhiteSpace(app.SelectedVideoPath))
                {
                    MessageBox.Show("Clips で動画を選択してね（仮）", "Exports", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrWhiteSpace(_folder))
                {
                    // 未選択ならデスクトップ
                    _folder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    UpdateFolderText();
                }

                var filename = $"export_dummy_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var outPath = Path.Combine(_folder, filename);

                var content =
                    $"SelectedVideoPath={app.SelectedVideoPath}\n" +
                    $"PlaybackSeconds={app.PlaybackSeconds}\n" +
                    $"TagsCount={app.Tags.Count}\n";

                app.ExportDummy(outPath, content);

                MessageBox.Show($"Exported:\n{outPath}", "Exports", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exports", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFolderText()
        {
            FolderText.Text = string.IsNullOrWhiteSpace(_folder) ? "(folder not set)" : _folder;
        }
    }
}
