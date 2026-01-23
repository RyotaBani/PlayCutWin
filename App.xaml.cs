using System;
using System.Windows;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class App : Application
    {
        private bool _mainWindowShown;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Crash dialog (so we can see XAML/runtime errors)
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                try
                {
                    MessageBox.Show(args.ExceptionObject?.ToString() ?? "Unknown error",
                        "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch { }
            };

            base.OnStartup(e);

            // 二重起動ガード（保険）
            if (_mainWindowShown) return;
            _mainWindowShown = true;

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Shutdown(-1);
        }
    }
}
