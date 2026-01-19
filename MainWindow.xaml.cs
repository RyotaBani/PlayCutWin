using PlayCutWin.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer = new DispatcherTimer();
        private bool _isPlaying = false;

        public MainWindow()
        {
            InitializeComponent();

            var vm = (MainViewModel)DataContext;

            vm.RequestLoadVideo = path =>
            {
                try
                {
                    Player.Source = new Uri(path);
                    Player.SpeedRatio = vm.SpeedRatio;
                    Player.Position = TimeSpan.Zero;

                    Player.MediaOpened += (_, __) =>
                    {
                        vm.NotifyVideoLoaded(true);
                        // 自動再生は好み。とりあえず止めた状態から開始
                        _isPlaying = false;
                    };

                    Player.MediaFailed += (_, e) =>
                    {
                        vm.NotifyVideoLoaded(false);
                        MessageBox.Show($"MediaFailed: {e.ErrorException?.Message}", "Play Cut");
                    };
                }
                catch (Exception ex)
                {
                    vm.NotifyVideoLoaded(false);
                    MessageBox.Show(ex.Message, "Play Cut");
                }
            };

            vm.GetCurrentPosition = () => Player.Position;
            vm.SeekTo = t => Player.Position = t;

            // Speed変更が反映されるように監視（簡易）
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.SpeedRatio))
                {
                    Player.SpeedRatio = vm.SpeedRatio;
                }
            };

            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += (_, __) =>
            {
                // ここにスライダー更新など入れたくなったら追加
            };
            _timer.Start();
        }

        private void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MainViewModel)DataContext;
            if (!vm.IsVideoLoaded) return;

            if (_isPlaying)
            {
                Player.Pause();
                _isPlaying = false;
            }
            else
            {
                Player.Play();
                _isPlaying = true;
            }
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Preferences（仮）", "Play Cut");
        }
    }
}
