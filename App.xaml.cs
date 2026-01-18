using System;
using System.Windows;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var w = new MainWindow
                {
                    DataContext = AppState.Instance
                };
                w.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Startup crash", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(-1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Unhandled UI Exception", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(ex?.ToString() ?? e.ExceptionObject?.ToString() ?? "Unknown",
                "Unhandled Exception", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
