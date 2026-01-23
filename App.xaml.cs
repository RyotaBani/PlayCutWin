using System;
using System.Windows;
using System.Windows.Threading;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace PlayCutWin
{
    public partial class App : Application
    {
        private bool _mainWindowShown;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global crash guards (show dialog + write log)
            this.DispatcherUnhandledException += (_, ex) =>
            {
                try { LogCrash(ex.Exception, "DispatcherUnhandledException"); } catch { }
                try
                {
                    MessageBox.Show(
                        $"Unhandled UI exception:\n\n{ex.Exception}",
                        "PlayCutWin Crash",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch { }
                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
            {
                try { LogCrash(ex.ExceptionObject as Exception, "UnhandledException"); } catch { }
                try
                {
                    MessageBox.Show(
                        $"Unhandled exception:\n\n{ex.ExceptionObject}",
                        "PlayCutWin Crash",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch { }
            };

            TaskScheduler.UnobservedTaskException += (_, ex) =>
            {
                try { LogCrash(ex.Exception, "UnobservedTaskException"); } catch { }
                ex.SetObserved();
            };

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
