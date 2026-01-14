using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MenuList.SelectedIndex = 0;
            StatusText.Text = "Ready";
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is ListBoxItem item)
            {
                PageTitle.Text = item.Content?.ToString() ?? "PlayCut";
                StatusText.Text = $"Selected: {PageTitle.Text}";
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Import Video (placeholder)\n次はここに動画読み込み処理を入れる。",
                "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Import clicked";
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export Clip (placeholder)\n次はここにクリップ書き出し処理を入れる。",
                "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Export clicked";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)\n次はここに設定画面を追加する。",
                "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Settings clicked";
        }
    }
}
