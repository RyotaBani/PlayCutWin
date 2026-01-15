using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContext = PlayCutWin.AppState.Instance;
        }
    }
}
