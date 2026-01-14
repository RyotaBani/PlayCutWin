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
            SelectedPathText.Text = sel == null ? "(none)" : sel.Path;
        }

        private void ExportDummy_Click(object sender, RoutedEventArgs e)
        {
            var sel = AppState.Current.SelectedVideo;
            if (sel == null)
            {
                MessageBox.Show("先に Clips で動画を選択してね。", "PlayCut",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            InfoText.Text = $"(dummy) Export requested for: {sel.Name}";
        }
    }
}
