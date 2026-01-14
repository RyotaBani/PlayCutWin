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

            // 初期表示
            MenuList.SelectedIndex = 0; // Dashboard
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is not ListBoxItem item) return;

            var key = item.Content?.ToString() ?? "Dashboard";
            NavigateTo(key);
        }

        private void NavigateTo(string key)
        {
            PageTitle.Text = key;

            switch (key)
            {
                case "Dashboard":
                    MainContent.Content = new DashboardView();
                    StatusText.Text = "Ready - Dashboard";
                    break;

                case "Clips":
                    MainContent.Content = new ClipsView();
                    StatusText.Text = "Ready - Clips";
                    break;

                case "Tags":
                    MainContent.Content = new TagsView();
                    StatusText.Text = "Ready - Tags";
                    break;

                case "Exports":
                    MainContent.Content = new ExportsView();
                    StatusText.Text = "Ready - Exports";
                    break;

                default:
                    MainContent.Content = new DashboardView();
                    StatusText.Text = "Ready";
                    break;
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.mkv|All Files|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() != true)
            {
                StatusText.Text = "Import cancelled";
                return;
            }

            foreach (var file in dlg.FileNames)
            {
                if (File.Exists(file))
                {
                    AppState.Instance.ImportedVideos.Add(file);
                }
            }

            // 先頭だけ見せる（複数選択も対応）
            var first = dlg.FileNames.Length > 0 ? dlg.FileNames[0] : "(none)";
            MessageBox.Show($"Selected:\n{first}", "Import", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = $"Imported: {Path.GetFileName(first)}";

            // Importしたら自動で Clips に移動
            MenuList.SelectedIndex = 1; // Clips
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export (placeholder)\nここにクリップ書き出しを追加する。", "PlayCut",
                MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Export clicked";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)\n次にここに設定画面を追加する。", "PlayCut",
                MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Settings clicked";
        }
    }
}
