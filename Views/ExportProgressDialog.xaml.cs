using System;
using System.Windows;

namespace PlayCutWin.Views
{
    public partial class ExportProgressDialog : Window
    {
        public ExportProgressDialog()
        {
            InitializeComponent();
        }

        public void SetProgress(int current, int total, string detail)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetProgress(current, total, detail));
                return;
            }

            if (total <= 0) total = 1;
            if (current < 0) current = 0;
            if (current > total) current = total;

            TitleText.Text = "Exporting clips...";
            DetailText.Text = $"{current} / {total}  {detail}";
            Progress.Maximum = total;
            Progress.Value = current;
        }

        public void SetDone(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetDone(message));
                return;
            }

            TitleText.Text = "Export complete";
            DetailText.Text = message;
        }
    }
}
