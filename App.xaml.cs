using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class App : Application
    {
        private bool _mainWindowShown;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Crash guards (show dialog + write log)
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

            base.OnStartup(e);

            // 二重起動ガード（保険）
            if (_mainWindowShown) return;
            _mainWindowShown = true;

            ShutdownMode = ShutdownMode.OnMainWindowClose;

            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }

        private static void LogCrash(Exception? ex, string kind)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PlayCutWin");
                Directory.CreateDirectory(dir);

                var path = Path.Combine(dir, "crash.log");
                var sb = new StringBuilder();
                sb.AppendLine("==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====");
                sb.AppendLine("Kind: " + kind);
                if (ex != null) sb.AppendLine(ex.ToString());
                else sb.AppendLine("(null exception object)");
                sb.AppendLine();
                File.AppendAllText(path, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // ignore
            }
        }
    }
}
