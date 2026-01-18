using System.Windows;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 念のためここでも固定
            DataContext = AppState.Instance;
        }
    }
}
