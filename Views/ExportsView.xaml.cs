using System;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        public ExportsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
        }

        private void ImportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppState.Instance.ImportCsvFromDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exports", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppState.Instance.ExportCsvToDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exports", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // まだ本実装前：落ちないダミー（ボタン押してもビルド/実行が死なないため）
        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppState.Instance.StatusMessage = "Export All: not implemented yet (dummy)";
                MessageBox.Show("Export All は次で実装する（今はダミー）", "Exports",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exports", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
