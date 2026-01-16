using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using PlayCutWin.Views;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly AppState _state = AppState.Current;

        public MainWindow()
        {
            InitializeComponent();

            _state.PropertyChanged += State_PropertyChanged;
            StatusText.Text = _state.StatusMessage;

            ShowDashboard();
        }

        private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.StatusMessage))
            {
                Dispatcher.Invoke(() => StatusText.Text = _state.StatusMessage);
            }
        }

        // ---- Top buttons ----------------------------------------------------
        private void ImportVideo_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Import video",
                Filter = "Video files|*.mp4;*.mov;*.mkv;*.avi;*.wmv|All files|*.*"
            };

            if (ofd.ShowDialog() == true)
            {
                _state.AddImportedVideo(ofd.FileName);
                // 取り込み後は Clips に移動
                ShowClips();
            }
        }

        private void GoExports_Click(object sender, RoutedEventArgs e) => ShowExports();

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Settings is not implemented yet.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ---- Nav ------------------------------------------------------------
        private void NavDashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();
        private void NavClips_Click(object sender, RoutedEventArgs e) => ShowClips();
        private void NavTags_Click(object sender, RoutedEventArgs e) => ShowTags();
        private void NavExports_Click(object sender, RoutedEventArgs e) => ShowExports();

        private void ShowDashboard()
        {
            PageTitle.Text = "Dashboard";
            MainContent.Content = new DashboardView();
            _state.StatusMessage = "Ready - Dashboard";
        }

        private void ShowClips()
        {
            PageTitle.Text = "Clips";
            MainContent.Content = new ClipsView();
            _state.StatusMessage = "Ready - Clips";
        }

        private void ShowTags()
        {
            PageTitle.Text = "Tags";
            MainContent.Content = new TagsView();
            _state.StatusMessage = "Ready - Tags";
        }

        private void ShowExports()
        {
            PageTitle.Text = "Exports";
            MainContent.Content = new ExportsView();
            _state.StatusMessage = "Ready - Exports";
        }
    }
}
