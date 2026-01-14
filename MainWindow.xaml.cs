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

            // 起動時はDashboard
            MenuList.SelectedIndex = 0;
            NavigateTo("Dashboard");
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is ListBoxItem item && item.Tag is string key)
            {
                NavigateTo(key);
            }
        }

        private void NavigateTo(string key)
        {
            PageTitle.Text = key;
            StatusText.Text = $"Ready - {key}";

            switch (key)
            {
                case "Dashboard":
                    PageHost.Content = new DashboardView();
                    break;
                case "Clips":
                    PageHost.Content = new ClipsView();
                    break;
                case "Tags":
                    PageHost.Content = new TagsView();
                    break;
                case "Exports":
                    PageHost.Content = new ExportsView();
                    break;
                default:
                    PageHost.Content = new DashboardView();
                    break;
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import Video (placeholder)", "PlayCut");
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export Clip (placeholder)", "PlayCut");
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)", "PlayCut");
        }
    }
}
