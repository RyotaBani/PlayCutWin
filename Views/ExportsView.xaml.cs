using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        public ExportsView()
        {
            InitializeComponent();
            Refresh();

            PlayCutWin.AppState.Current.PropertyChanged += OnStateChanged;
        }

        private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideo) ||
                e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideoPath))
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var path = PlayCutWin.AppState.Current.SelectedVideoPath;
            SelectedPathText.Text = string.IsNullOrWhiteSpace(path) ? "(none)" : path;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var selected = PlayCutWin.AppState.Current.SelectedVideoName;
            MessageBox.Show($"Export dummy\nSelected: {selected}", "Exports", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
