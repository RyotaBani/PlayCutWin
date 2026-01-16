using System.Windows;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // アプリ全体の状態をここで固定
            DataContext = AppState.Instance;
        }
    }
}
