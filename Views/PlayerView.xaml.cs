using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class PlayerView : UserControl
    {
        public PlayerView()
        {
            InitializeComponent();

            // 表示だけなので AppState を DataContext にするだけ
            DataContext = AppState.Instance;
        }
    }
}
