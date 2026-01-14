using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 初期表示
            MenuList.SelectedIndex = 0;
            SetPage("Dashboard");
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is ListBoxItem item && item.Tag is string tag)
            {
                SetPage(tag);
            }
        }

        private void SetPage(string key)
        {
            switch (key)
            {
                case "Dashboard":
                    PageTitle.Text = "Dashboard";
                    MainContent.ContentTemplate = (DataTemplate)Resources["DashboardTemplate"];
                    StatusText.Text = "Ready - Dashboard";
                    break;

                case "Clips":
                    PageTitle.Text = "Clips";
                    MainContent.ContentTemplate = (DataTemplate)Resources["ClipsTemplate"];
                    StatusText.Text = "Ready - Clips";
                    break;

                case "Tags":
                    PageTitle.Text = "Tags";
                    MainContent.ContentTemplate = (DataTemplate)Resources["TagsTemplate"];
                    StatusText.Text = "Ready - Tags";
                    break;

                case "Exports":
                    PageTitle.Text = "Exports";
                    MainContent.ContentTemplate = (DataTemplate)Resources["ExportsTemplate"];
                    StatusText.Text = "Ready - Exports";
                    break;

                default:
                    PageTitle.Text = "Dashboard";
                    MainContent.ContentTemplate = (DataTemplate)Resources["DashboardTemplate"];
                    StatusText.Text = "Ready - Dashboard";
                    break;
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            // まずは “Windowsでファイル選べる” を確実にする
            var dlg = new OpenFileDialog
            {
                Title = "Select a video file",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.mkv|All Files|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                StatusText.Text = $"Imported: {System.IO.Path.GetFileName(dlg.FileName)}";
                MessageBox.Show($"Selected:\n{dlg.FileName}", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                StatusText.Text = "Import canceled";
            }
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export (placeholder)\nここに Export 処理を追加する。", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Export clicked";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)\nここに設定画面を追加する。", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Settings clicked";
        }
    }
}
