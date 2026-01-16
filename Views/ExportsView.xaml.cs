using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        AppState state = AppState.Instance;

        public ExportsView()
        {
            InitializeComponent();
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            if (state.SelectedClip == null) return;
            Export(new[] { state.SelectedClip });
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            Export(state.Tags.Keys);
        }

        void Export(IEnumerable<VideoItem> clips)
        {
            var dialog = new SaveFileDialog { Filter = "CSV|*.csv" };
            if (dialog.ShowDialog() != true) return;

            using var sw = new StreamWriter(dialog.FileName, false, Encoding.UTF8);
            sw.WriteLine("Video,Tag");

            foreach (var c in clips)
                if (state.Tags.ContainsKey(c))
                    foreach (var t in state.Tags[c])
                        sw.WriteLine($"{c.FileName},{t}");
        }
    }
}
