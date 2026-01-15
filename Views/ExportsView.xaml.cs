using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        private PlayCutWin.ClipItem? _selectedClip;

        public ExportsView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;
        }

        private void ClipsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedClip = ClipsGrid.SelectedItem as PlayCutWin.ClipItem;
            if (_selectedClip != null)
            {
                PlayCutWin.AppState.Instance.StatusMessage =
                    $"Selected clip: {_selectedClip.StartText}-{_selectedClip.EndText}";
            }
        }

        private void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var dialog = new FolderBrowserDialog();
                dialog.Description = "Choose export destination folder";
                dialog.ShowNewFolderButton = true;

                var result = dialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    PlayCutWin.AppState.Instance.ExportFolder = dialog.SelectedPath;
                    PlayCutWin.AppState.Instance.StatusMessage = $"Export folder set: {dialog.SelectedPath}";
                }
            }
            catch (Exception ex)
            {
                PlayCutWin.AppState.Instance.StatusMessage = $"Choose folder failed: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Choose folder failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_selectedClip == null)
                {
                    System.Windows.MessageBox.Show("クリップを選択してね", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var folder = PlayCutWin.AppState.Instance.ExportFolder;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    System.Windows.MessageBox.Show("先にExportフォルダを選択してね", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // ダミー書き出し：txtにメタ情報を出す（後でffmpegに差し替え）
                var safeName = MakeSafeFileName($"{_selectedClip.VideoName}_{_selectedClip.StartText}-{_selectedClip.EndText}");
                var filePath = Path.Combine(folder, safeName + ".txt");

                var content =
$@"PlayCutWin Export (dummy)
Video: {_selectedClip.VideoName}
Path:  {_selectedClip.VideoPath}
Start: {_selectedClip.StartText}
End:   {_selectedClip.EndText}
Team:  {_selectedClip.Team}
Tags:  {_selectedClip.TagsText}
Note:  {_selectedClip.Note}
ExportedAt: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
";

                File.WriteAllText(filePath, content);

                PlayCutWin.AppState.Instance.StatusMessage = $"Exported (dummy): {filePath}";
                System.Windows.MessageBox.Show($"書き出し完了（ダミー）\n{filePath}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                PlayCutWin.AppState.Instance.StatusMessage = $"Export failed: {ex.Message}";
                System.Windows.MessageBox.Show(ex.Message, "Export failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name.Trim();
        }
    }
}
