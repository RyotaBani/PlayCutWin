using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        // XAMLから参照するためのstaticインスタンス（Converter）
        public static IValueConverter FileExtConverterInstance { get; } = new FileExtConverter();

        public ClipsView()
        {
            InitializeComponent();
        }

        private void VideoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VideoList.SelectedItem is string path)
            {
                // 選択中動画を共有状態に保存
                AppState.Current.SetSelected(path);

                // ステータスバー更新
                if (Application.Current.MainWindow is MainWindow mw)
                    mw.UpdateStatusSelected();
            }
        }

        private sealed class FileExtConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var path = value as string;
                if (string.IsNullOrWhiteSpace(path)) return "";

                var ext = Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
                return string.IsNullOrWhiteSpace(ext) ? "FILE" : ext;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
                => Binding.DoNothing;
        }
    }
}
