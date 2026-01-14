using System.Windows.Controls;
using PlayCutWin;

namespace PlayCutWin.Views
{
    public partial class DashboardView : UserControl
    {
        public DashboardView()
        {
            InitializeComponent();
            DataContext = AppState.Current;
        }
    }
}
