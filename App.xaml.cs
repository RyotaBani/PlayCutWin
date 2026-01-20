using System;
using System.Windows;

namespace PlayCutWin
{
    public partial class App : Application
    {
        private bool _mainWindowShown;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 二重起動ガード（保険）
            if (_mainWindowShown) return;
            _mainWindowShown = true;

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
    }
}
