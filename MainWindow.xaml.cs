using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private const string TeamAPlaceholder = "Home / Our Team";
        private const string TeamBPlaceholder = "Away / Opponent";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void TeamBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (tb.Text == TeamAPlaceholder || tb.Text == TeamBPlaceholder)
                {
                    tb.Text = "";
                    tb.Foreground = Brushes.White;
                }
                tb.SelectAll();
            }
        }

        private void TeamBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                if (string.IsNullOrWhiteSpace(tb.Text))
                {
                    if (tb == TeamATextBox)
                        tb.Text = TeamAPlaceholder;
                    else
                        tb.Text = TeamBPlaceholder;

                    tb.Foreground = Brushes.Gray;
                }
            }
        }
    }
}
