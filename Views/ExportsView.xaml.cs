using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        public ExportsView()
        {
            InitializeComponent();
            DataContext = AppState.Current;
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Export (dummy)\n（将来：選択範囲の切り出し→保存）", "Exports", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
