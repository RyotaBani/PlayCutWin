using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class PlayerView : UserControl
    {
        public PlayerView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
        }
    }
}
