using System.ComponentModel;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        public ClipsView()
        {
            InitializeComponent();

            VideosGrid.ItemsSource = PlayCutWin.AppState.Current.ImportedVideos;

            RefreshCount();
            PlayCutWin.AppState.Current.PropertyChanged += OnStateChanged;
            PlayCutWin.AppState.Current.ImportedVideos.CollectionChanged += (_, __) => RefreshCount();
        }

        private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideo))
            {
                // 選択が外から変わった時も DataGrid の選択を合わせる
                var s = PlayCutWin.AppState.Current.SelectedVideo;
                if (s != null && VideosGrid.SelectedItem != s)
                    VideosGrid.SelectedItem = s;
            }
        }

        private void RefreshCount()
        {
            var count = PlayCutWin.AppState.Current.ImportedCount;
            CountText.Text = $"Count: {count}";
            RightCount.Text = $"Count: {count}";
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideosGrid.SelectedItem is PlayCutWin.VideoItem item)
            {
                PlayCutWin.AppState.Current.SetSelected(item);
            }
        }
    }
}
