using System;
using System.Windows;
using LibVLCSharp.Shared;
using PlayCutWin.ViewModels;

namespace PlayCutWin.Views;

public partial class MainWindow : Window
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;

    public MainWindow()
    {
        InitializeComponent();

        Core.Initialize(); // LibVLCSharp init

        _libVlc = new LibVLC();
        _player = new MediaPlayer(_libVlc);

        VideoView.MediaPlayer = _player;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        vm.RequestLoadVideo = path =>
        {
            try
            {
                using var media = new Media(_libVlc, new Uri(path));
                _player.Media = media;
                _player.Pause();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"動画の読み込みに失敗しました: {ex.Message}");
            }
        };

        vm.QueryCurrentTimeSeconds = () => _player.Time / 1000.0;

        vm.RequestPlay = () => _player.Play();
        vm.RequestPause = () => _player.Pause();

        vm.RequestSetRate = rate =>
        {
            // LibVLC playback rate
            _player.SetRate((float)rate);
        };

        vm.RequestSeekSeconds = seconds =>
        {
            _player.Time = (long)(seconds * 1000.0);
        };
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        try
        {
            _player.Dispose();
            _libVlc.Dispose();
        }
        catch
        {
            // ignore
        }
    }

    private void ClipsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.JumpToSelectedCommand.Execute(null);
        }
    }
}
