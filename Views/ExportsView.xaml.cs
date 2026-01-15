using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        public ExportsView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;
        }

        private void ExportDummy_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("(dummy)\nExport処理は次のステップで入れる。", "Exports", MessageBoxButton.OK, MessageBoxImage.Information);
            PlayCutWin.AppState.Instance.StatusMessage = "Export (dummy) clicked";
        }
    }
}
