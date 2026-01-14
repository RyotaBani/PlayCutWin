using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
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

            // 初期ページ
            MenuList.SelectedIndex = 0;
            NavigateTo("Dashboard");
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is ListBoxItem item && item.Content is string name)
            {
                NavigateTo(name);
            }
        }

        private void NavigateTo(string pageName)
        {
            PageTitle.Text = pageName;

            switch (pageName)
            {
                case "Dashboard":
                    MainContent.Content = _dashboard;
                    StatusText.Text = $"Selected: {AppState.Current.SelectedVideoName} - Dashboard";
                    break;

                case "Clips":
                    MainContent.Content = _clips;
                    StatusText.Text = $"Selected: {AppState.Current.SelectedVideoName} - Clips";
                    break;

                case "Tags":
                    MainContent.Content = _tags;
                    StatusText.Text = $"Selected: {AppState.Current.SelectedVideoName} - Tags";
                    break;

                case "Exports":
                    MainContent.Content = _exports;
                    StatusText.Text = $"Selected: {AppState.Current.SelectedVideoName} - Exports";
                    break;

                default:
                    MainContent.Content = _dashboard;
                    StatusText.Text = $"Selected: {AppState.Current.SelectedVideoName}";
                    break;
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select a video file",
                    Filter = "Video Files|*.mp4;*.mov;*.mkv;*.avi|All Files|*.*"
                };

                if (dlg.ShowDialog() == true)
                {
                    AppState.Current.AddImportedVideo(dlg.FileName);
                    StatusText.Text = $"Imported: {AppState.Current.SelectedVideoName}";
                    MessageBox.Show($"Selected:\n{dlg.FileName}", "Import", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Dashboard へ戻して「選択中」を見せる
                    MenuList.SelectedIndex = 0;
                }
                else
                {
                    StatusText.Text = "Import cancelled";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Import error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export (placeholder)\nここに書き出し処理を入れる。", "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Export clicked";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)\nここに設定画面を追加する。", "PlayCut", MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Settings clicked";
        }
    }
}
