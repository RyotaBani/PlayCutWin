using Microsoft.Win32;
using System;
using System.IO;
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
            Navigate("Dashboard");

            // Reflect status changes
            AppState.Instance.PropertyChanged += (_, __) =>
            {
                StatusText.Text = AppState.Instance.StatusMessage;
            };
            StatusText.Text = AppState.Instance.StatusMessage;
        }

        private void Navigate(string page)
        {
            PageTitle.Text = page;

            switch (page)
            {
                case "Dashboard":
                    MainContent.Content = new DashboardView();
                    break;
                case "Clips":
                    MainContent.Content = new ClipsView();
                    break;
                case "Tags":
                    MainContent.Content = new TagsView();
                    break;
                case "Exports":
                    MainContent.Content = new ExportsView();
                    break;
                default:
                    MainContent.Content = new DashboardView();
                    break;
            }

            AppState.Instance.StatusMessage = $"Ready - {page}";
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is ListBoxItem item && item.Content is string label)
            {
                Navigate(label);
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import Video",
                Filter = "Video files|*.mp4;*.mov;*.mkv;*.avi;*.wmv|All files|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                AppState.Instance.AddImportedVideo(dlg.FileName);

                // auto-select last imported
                AppState.Instance.SetSelected(dlg.FileName);

                MessageBox.Show($"Selected:\n{dlg.FileName}", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export (placeholder)\nここに書き出し処理を入れる。", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            AppState.Instance.StatusMessage = "Export clicked";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)\nここに設定画面を追加する。", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            AppState.Instance.StatusMessage = "Settings clicked";
        }
    }
}
