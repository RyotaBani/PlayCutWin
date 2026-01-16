using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        private readonly AppState _state = AppState.Current;
        private string _exportFolder = "";

        public ExportsView()
        {
            InitializeComponent();
            DataContext = _state;

            RefreshSelectedText();
            _state.PropertyChanged += State_PropertyChanged;
        }

        private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SelectedVideoPath))
            {
                Dispatcher.Invoke(RefreshSelectedText);
            }
        }

        private void RefreshSelectedText()
        {
            SelectedVideoText.Text = string.IsNullOrWhiteSpace(_state.SelectedVideoPath)
                ? "(none)"
                : _state.SelectedVideoPath;
        }

        private void ChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            // FolderBrowserDialog を避けるため、SaveFileDialog で保存先を選ばせてフォルダだけ抜く
            var sfd = new SaveFileDialog
            {
                Title = "Choose export folder (select a file name, folder will be used)",
                FileName = "export.txt",
                Filter = "Text|*.txt|All files|*.*"
            };

            if (sfd.ShowDialog() == true)
            {
                _exportFolder = Path.GetDirectoryName(sfd.FileName) ?? "";
                SelectedFolderText.Text = string.IsNullOrWhiteSpace(_exportFolder) ? "(not set)" : _exportFolder;
                _state.StatusMessage = $"Export folder: {_exportFolder}";
            }
        }

        private void ExportDummy_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_exportFolder) || !Directory.Exists(_exportFolder))
            {
                MessageBox.Show("Please choose an export folder first.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var safeName = "export_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            var outPath = Path.Combine(_exportFolder, safeName);

            var sb = new StringBuilder();
            sb.AppendLine("PlayCutWin export (dummy)");
            sb.AppendLine("SelectedVideoPath: " + (_state.SelectedVideoPath ?? ""));
            sb.AppendLine("PlaybackPosition: " + _state.PlaybackPositionText);
            sb.AppendLine("");
            sb.AppendLine("Tags:");
            foreach (var t in _state.Tags.Where(t => string.IsNullOrWhiteSpace(_state.SelectedVideoPath) ||
                                                    string.Equals(t.VideoPath, _state.SelectedVideoPath, StringComparison.OrdinalIgnoreCase)))
            {
                sb.AppendLine($"{t.Time}\t{t.Text}");
            }

            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
            _state.StatusMessage = $"Exported: {outPath}";
            MessageBox.Show($"Exported:\n{outPath}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
