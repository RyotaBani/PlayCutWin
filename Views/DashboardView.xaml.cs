using System.ComponentModel;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            Refresh();

            PlayCutWin.AppState.Current.PropertyChanged += OnAppStateChanged;
        }

        private void OnAppStateChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideo) ||
                e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideoName) ||
                e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideoPath))
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            var s = PlayCutWin.AppState.Current;
            SelectedText.Text = s.SelectedVideoPath.Length > 0
                ? s.SelectedVideoPath
                : "(no video selected)";
        }
    }
}
