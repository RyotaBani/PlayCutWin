using Microsoft.Win32;
using System.ComponentModel;
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
            MenuList.SelectedIndex = 0;
            ShowPage("Dashboard");

            // 状態変化でステータス更新
            AppState.Current.PropertyChanged += AppState_PropertyChanged;
        }

        private void AppState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SelectedVideo) ||
                e.PropertyName == nameof(AppState.SelectedVideoDisplay))
            {
                var sel = AppState.Current.SelectedVideo;
                if (sel == null)
                {
                    StatusText.Text = $"Ready - {PageTitle.Text}";
                }
                else
                {
                    StatusText.Text = $"Selected: {sel.Name} - {PageTitle.Text}";
                }
            }
        }

        private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuList.SelectedItem is ListBoxItem item && item.Content is string label)
            {
                ShowPage(label);
            }
        }

        private void ShowPage(string label)
        {
            PageTitle.Text = label;

            UserControl view = label switch
            {
                "Clips" => new ClipsView(),
                "Tags" => new TagsView(),
                "Exports" => new ExportsView(),
                _ => new DashboardView()
            };

            MainContent.Content = view;

            // ステータスも更新
            AppState_PropertyChanged(null, new PropertyChangedEventArgs(nameof(AppState.SelectedVideo)));
        }

        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Select Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                AppState.Current.AddImportedVideo(dlg.FileName);
                StatusText.Text = $"Imported: {System.IO.Path.GetFileName(dlg.FileName)} - {PageTitle.Text}";
            }
        }

        private void ExportClip_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export (placeholder)\nここに書き出し処理を実装する。", "PlayCut",
                MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Export clicked";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings (placeholder)\nここに設定画面を追加する。", "PlayCut",
                MessageBoxButton.OK, MessageBoxImage.Information);
            StatusText.Text = "Settings clicked";
        }
    }
}
