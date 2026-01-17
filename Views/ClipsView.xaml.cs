using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private AppState S => AppState.Instance;

        public ClipsView()
        {
            InitializeComponent();
            DataContext = S;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Import Video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.avi;*.wmv|All Files|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (var p in dlg.FileNames)
                    S.AddImportedVideo(p);
            }
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 選択で再生/Rangeを初期化（事故防止）
            S.PlaybackSeconds = 0;
            S.ResetRange();
        }

        private void OpenExports_Click(object sender, RoutedEventArgs e)
        {
            // 簡易：別ウィンドウで Exports を開く
            var w = new Window
            {
                Title = "Exports",
                Width = 700,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                Content = new ExportsView()
            };
            w.ShowDialog();
        }
    }
}
