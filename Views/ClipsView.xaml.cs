using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private bool _suppressSelection = false;

        public ClipsView()
        {
            InitializeComponent();

            Refresh();

            PlayCutWin.AppState.Instance.ImportedVideos.CollectionChanged += (_, __) =>
                Dispatcher.Invoke(Refresh);

            PlayCutWin.AppState.Instance.PropertyChanged += AppState_PropertyChanged;
        }

        private void AppState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideoPath))
            {
                Dispatcher.Invoke(SyncSelectionFromState);
            }
        }

        private sealed class VideoRow
        {
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
        }

        private void Refresh()
        {
            var rows = PlayCutWin.AppState.Instance.ImportedVideos
                .Select(p => new VideoRow { Name = Path.GetFileName(p), Path = p })
                .ToList();

            VideoList.ItemsSource = rows;
            CountText.Text = $"Count: {rows.Count}";

            SyncSelectionFromState();
        }

        private void SyncSelectionFromState()
        {
            if (VideoList.ItemsSource is not IEnumerable<VideoRow> rows) return;

            var selectedPath = PlayCutWin.AppState.Instance.SelectedVideoPath;
            if (string.IsNullOrWhiteSpace(selectedPath)) return;

            var target = rows.FirstOrDefault(r => r.Path == selectedPath);
            if (target == null) return;

            _suppressSelection = true;
            VideoList.SelectedItem = target;
            VideoList.ScrollIntoView(target);
            _suppressSelection = false;
        }

        private void VideoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection) return;

            if (VideoList.SelectedItem is VideoRow row)
            {
                PlayCutWin.AppState.Instance.SelectedVideoPath = row.Path;
            }
        }
    }
}
