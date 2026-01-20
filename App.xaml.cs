using System;
using System.Windows;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class App : Application
    {
        public App()
        {
            // WPF UIスレッド例外を捕まえて表示
            this.DispatcherUnhandledException += OnDispatcherUnhandledException;

            // UIスレッド以外の未処理例外（保険）
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                e.Exception.ToString(),
                "Unhandled Exception (UI Thread)",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            e.Handled = true;
            Shutdown(-1);
        }

        private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show(
                    ex?.ToString() ?? "Unknown exception",
                    "Unhandled Exception",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                Shutdown(-1);
            }
        }
    }
}
