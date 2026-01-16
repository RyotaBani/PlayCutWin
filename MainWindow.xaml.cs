using Microsoft.Win32;
using System.Linq;
using System.Windows;
using PlayCutWin.Views;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly DashboardView _dashboard = new DashboardView();
        private readonly ClipsView _clips = new ClipsView();
        private readonly TagsView _tags = new TagsView();
        private readonly ExportsView _exports = new ExportsView();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = AppState.Instance;

            ShowDashboard();
        }

        private void ShowDashboard()
        {
            PageTitle.Text = "Dashboard";
            MainContent.Content = _dashboard;
        }

        private void ShowClips()
        {
            PageTitle.Text = "Clips";
            MainContent.Content = _clips;
        }

        private void ShowTags()
        {
            PageTitle.Text = "Tags";
            MainContent.Content = _tags;
        }

        private void ShowExports()
        {
            PageTitle.Text = "Exports";
            MainContent.Content = _exports;
        }

        private void GoDashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();
        private void GoClips_Click(object sender, RoutedEventArgs e) => ShowClips();
        private void GoTags_Click(object sender, RoutedEventArgs e) => ShowTags();
        private void GoExports_Click(object sender, RoutedEventArgs e) => ShowExports();

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (dummy)", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var p in dlg.FileNames)
                    AppState.Instance.AddImportedVideo(p);

                // 自動で Clips に移動
                ShowClips();

                // 先頭を選択
                if (AppState.Instance.SelectedVideo == null && AppState.Instance.ImportedVideos.Any())
                    AppState.Instance.SetSelected(AppState.Instance.ImportedVideos.First());
            }
        }
    }
}
