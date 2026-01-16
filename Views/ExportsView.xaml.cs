using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        private AppState State => AppState.Instance;

        public ExportsView()
        {
            InitializeComponent();
            DataContext = State;
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            if (State.SelectedVideo == null)
            {
                MessageBox.Show("No clip selected.", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ExportCsv(State.SelectedVideo.Name + ".csv", onlySelected: true);
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            ExportCsv("all_clips.csv", onlySelected: false);
        }

        private void ExportCsv(string defaultName, bool onlySelected)
        {
            var dlg = new SaveFileDialog
            {
                FileName = defaultName,
                Filter = "CSV Files (*.csv)|*.csv",
                Title = "Export CSV"
            };

            if (dlg.ShowDialog() != true) return;

            using var sw = new StreamWriter(dlg.FileName, false, Encoding.UTF8);

            // ヘッダー
            sw.WriteLine("Video,Tag");

            if (onlySelected)
            {
                WriteClip(sw, State.SelectedVideo!);
            }
            else
            {
                foreach (var video in State.Videos)
                    WriteClip(sw, video);
            }

            MessageBox.Show("Export completed.", "Export",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void WriteClip(StreamWriter sw, VideoItem video)
        {
            if (!State.TagsByVideo.TryGetValue(video, out var tags)) return;

            foreach (var tag in tags)
                sw.WriteLine($"{video.Name},{tag}");
        }
    }
}
