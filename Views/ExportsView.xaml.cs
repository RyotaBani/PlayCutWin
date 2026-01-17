using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        private AppState S => AppState.Instance;

        public ExportsView()
        {
            InitializeComponent();
            DataContext = S;
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            if (S.SelectedVideo == null)
            {
                MessageBox.Show("No clip selected.", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExportCsv(onlySelected: true);
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportCsv(onlySelected: false);
        }

        private void ExportCsv(bool onlySelected)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Export CSV",
                Filter = "CSV (*.csv)|*.csv",
                FileName = onlySelected && S.SelectedVideo != null ? $"{S.SelectedVideo.Name}_tags.csv" : "all_tags.csv"
            };

            if (dlg.ShowDialog() != true) return;

            // Excel対策：UTF-8 BOM
            using var sw = new StreamWriter(dlg.FileName, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            sw.WriteLine("Video,Time,Seconds,Tag,ClipStart,ClipEnd,Path");

            if (onlySelected && S.SelectedVideo != null)
            {
                WriteOne(sw, S.SelectedVideo);
            }
            else
            {
                foreach (var v in S.ImportedVideos)
                    WriteOne(sw, v);
            }

            S.StatusMessage = $"Exported: {dlg.FileName}";
            MessageBox.Show("Export completed.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WriteOne(StreamWriter sw, VideoItem v)
        {
            // SelectedVideo以外のTagsも出したいので全列挙を使う
            // v.Pathに紐づくタグだけ書き出し
            foreach (var pair in S.EnumerateAllTags())
            {
                if (!string.Equals(pair.video.Path, v.Path, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var tag = pair.tag;
                var safeText = (tag.Text ?? "").Replace("\"", "\"\"");
                sw.WriteLine($"\"{v.Name}\",\"{tag.TimeText}\",{tag.Seconds:F1},\"{safeText}\",{S.ClipStart:F1},{S.ClipEnd:F1},\"{v.Path}\"");
            }
        }
    }
}
