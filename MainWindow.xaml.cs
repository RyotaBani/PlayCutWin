using Microsoft.Win32;
using System;
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

            // 初期画面
            MenuList.SelectedIndex = 0;

            // Status連動
            AppState.Current.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppState.StatusMessage))
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = AppState.Current.StatusMessage;
                    });
                }
            };
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is not ListBoxItem item) return;

            var key = item.Content?.ToString() ?? "Dashboard";
            PageTitle.Text = key;

            switch (key)
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

            StatusText.Text = $"Ready - {key}";
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select a video file",
                Filter = "Video files|*.mp4;*.mov;*.mkv;*.avi;*.wmv|All files|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                AppState.Current.AddImportedVideo(dlg.FileName);
            }
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export (placeholder)\nここは後で実装する。", "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            AppState.Current.StatusMessage = "Export clicked";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)\nここは後で設定画面を追加する。", "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            AppState.Current.StatusMessage = "Settings clicked";
        }
    }
}
