using System.Windows;
using System.Windows.Controls;
using PlayCutWin;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        public ExportsView()
        {
            InitializeComponent();
            DataContext = AppState.Current;
        }

        private void ExportDummy_Click(object sender, RoutedEventArgs e)
        {
            if (AppState.Current.SelectedVideo == null)
            {
                MessageBox.Show("Select a video first.", "Exports", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show(
                "(placeholder) ここに書き出しUIを作る\n\n"
                + $"Selected:\n{AppState.Current.SelectedVideoPath}\n\n"
                + $"Tags: {AppState.Current.TagsForSelectedVideo.Count}",
                "Exports",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
