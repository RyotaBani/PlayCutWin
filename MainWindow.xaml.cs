using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using PlayCutWin.Views;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly DashboardView _dashboardView = new();
        private readonly ClipsView _clipsView = new();
        private readonly TagsView _tagsView = new();
        private readonly ExportsView _exportsView = new();

        public MainWindow()
        {
            InitializeComponent();

            // 初期選択
            MenuList.SelectedIndex = 0;

            // AppState変更に追従してステータス更新
            AppState.Current.PropertyChanged += (_, __) => UpdateStatusSelected();
            UpdateStatusSelected();
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is not ListBoxItem item) return;
            var name = item.Content?.ToString() ?? "Dashboard";

            PageTitle.Text = name;

            MainContent.Content = name switch
            {
                "Dashboard" => _dashboardView,
                "Clips" => _clipsView,
                "Tags" => _tagsView,
                "Exports" => _exportsView,
                _ => _dashboardView
            };

            UpdateStatusSelected();
        }

        public void UpdateStatusSelected()
        {
            var sel = AppState.Current.SelectedVideoFileName;
            if (sel == "(none)")
            {
                StatusText.Text = $"Ready - {PageTitle.Text}";
            }
            else
            {
                StatusText.Text = $"Selected: {sel} - {PageTitle.Text}";
            }
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.mkv|All Files|*.*",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                AppState.Current.AddImportedVideo(dlg.FileName);

                // Clips に飛ぶ（分かりやすい）
                MenuList.SelectedIndex = 1;

                MessageBox.Show(
                    $"Selected:\n{dlg.FileName}",
                    "Import",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                UpdateStatusSelected();
            }
            else
            {
                StatusText.Text = "Import cancelled";
            }
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Export (placeholder)\nここに後でクリップ書き出しを実装します。",
                "PlayCut",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            StatusText.Text = "Export clicked";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Settings (placeholder)\nここに後で設定画面を追加します。",
                "PlayCut",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            StatusText.Text = "Settings clicked";
        }
    }
}
