using System;
using System.Threading;
using System.Windows;

namespace PlayCutWin
{
    public partial class App : Application
    {
        private Mutex? _singleInstanceMutex;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // --- Single instance guard ---
            // If the user launches the EXE twice, Windows runs 2 processes and you'll see 2 windows.
            bool createdNew;
            _singleInstanceMutex = new Mutex(initiallyOwned: true, name: "PlayCutWin.SingleInstance", createdNew: out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("Play Cut はすでに起動しています。", "Play Cut");
                Shutdown();
                return;
            }

            var main = new MainWindow();
            MainWindow = main;
            main.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
            }
            catch
            {
                // ignore
            }

            base.OnExit(e);
        }
    }
}
