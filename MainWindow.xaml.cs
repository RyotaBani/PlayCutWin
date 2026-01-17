using System.Windows;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = AppState.Instance; // 全体で統一
        }
    }
}
