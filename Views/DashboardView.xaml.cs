using System.ComponentModel;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            UpdateSelected();

            AppState.Current.PropertyChanged += Current_PropertyChanged;
        }

        private void Current_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SelectedVideo) ||
                e.PropertyName == nameof(AppState.SelectedVideoDisplay))
            {
                UpdateSelected();
            }
        }

        private void UpdateSelected()
        {
            var sel = AppState.Current.SelectedVideo;
            SelectedText.Text = sel == null ? "Selected: (none)" : $"Selected: {sel.Name}";
        }
    }
}
