using System.Collections.Specialized;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        public ClipsView()
        {
            InitializeComponent();

            VideosGrid.ItemsSource = AppState.Current.ImportedVideos;

            AppState.Current.ImportedVideos.CollectionChanged += ImportedVideos_CollectionChanged;
            UpdateCount();
        }

        private void ImportedVideos_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCount();
        }

        private void UpdateCount()
        {
            CountText.Text = $"Count: {AppState.Current.ImportedVideos.Count}";
        }

        private void VideosGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideosGrid.SelectedItem is VideoItem item)
            {
                AppState.Current.SetSelected(item); // ← ここが今回の共有ポイント
            }
        }
    }
}
