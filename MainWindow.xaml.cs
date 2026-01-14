using System.Windows;
using System.Windows.Controls;
using PlayCutWin.Views;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Default page
            MenuList.SelectedIndex = 0;
        }

        private void SetStatus(string text)
        {
            StatusText.Text = text;
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is not ListBoxItem item) return;

            var key = item.Content?.ToString() ?? "Dashboard";

            switch (key)
            {
                case "Dashboard":
                    ContentHost.Content = new DashboardView();
                    SetStatus("Ready - Dashboard");
                    break;

                case "Clips":
                    ContentHost.Content = new ClipsView();
                    SetStatus("Ready - Clips");
                    break;

                case "Tags":
                    ContentHost.Content = new TagsView();
                    SetStatus("Ready - Tags");
                    break;

                case "Exports":
                    ContentHost.Content = new ExportsView();
                    SetStatus("Ready - Exports");
                    break;

                default:
                    ContentHost.Content = new DashboardView();
                    SetStatus("Ready - Dashboard");
                    break;
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import (placeholder)\nここにOpenFileDialogで動画選択を入れる。",
                "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            SetStatus("Import clicked");
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export (placeholder)\nここにクリップ書き出し処理を入れる。",
                "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            SetStatus("Export clicked");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)\n次にここへ設定画面(UserControl/Window)を追加する。",
                "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            SetStatus("Settings clicked");
        }
    }
}
