using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainWindowViewModel(this);
            DataContext = _vm;
        }

        private void Player_MediaOpened(object sender, RoutedEventArgs e)
        {
            VideoHint.Visibility = Visibility.Collapsed;
            _vm.OnMediaOpened();
        }

        private void Player_MediaEnded(object sender, RoutedEventArgs e)
        {
            _vm.OnMediaEnded();
        }

        private void Timeline_PreviewMouseDown(object sender, MouseButtonEventArgs e) => _vm.IsDraggingTimeline = true;
        private void Timeline_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            _vm.IsDraggingTimeline = false;
            _vm.SeekTo(_vm.TimelineValue);
        }

        private void TeamAClips_MouseDoubleClick(object sender, MouseButtonEventArgs e) => _vm.JumpToSelectedClip(isTeamA: true);
        private void TeamBClips_MouseDoubleClick(object sender, MouseButtonEventArgs e) => _vm.JumpToSelectedClip(isTeamA: false);
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly MainWindow _view;
        private readonly DispatcherTimer _timer;

        public event PropertyChangedEventHandler PropertyChanged;

        // ====== UI / State ======
        public ObservableCollection<ClipItem> TeamAClips { get; } = new();
        public ObservableCollection<ClipItem> TeamBClips { get; } = new();

        private ClipItem _selectedTeamAClip;
        public ClipItem SelectedTeamAClip
        {
            get => _selectedTeamAClip;
            set { _selectedTeamAClip = value; OnPropertyChanged(); }
        }

        private ClipItem _selectedTeamBClip;
        public ClipItem SelectedTeamBClip
        {
            get => _selectedTeamBClip;
            set { _selectedTeamBClip = value; OnPropertyChanged(); }
        }

        private string _teamAName = "Home / Our Team";
        public string TeamAName
        {
            get => _teamAName;
            set { _teamAName = value; OnPropertyChanged(); }
        }

        private string _teamBName = "Away / Opponent";
        public string TeamBName
        {
            get => _teamBName;
            set { _teamBName = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> ClipFilters { get; } = new() { "All Clips" };

        private string _selectedClipFilter = "All Clips";
        public string SelectedClipFilter
        {
            get => _selectedClipFilter;
            set { _selectedClipFilter = value; OnPropertyChanged(); }
        }

        private double _timelineMax = 1;
        public double TimelineMax
        {
            get => _timelineMax;
            set { _timelineMax = value <= 0 ? 1 : value; OnPropertyChanged(); }
        }

        private double _timelineValue;
        public double TimelineValue
        {
            get => _timelineValue;
            set { _timelineValue = value; OnPropertyChanged(); OnPropertyChanged(nameof(TimelineLabel)); }
        }

        public bool IsDraggingTimeline { get; set; }

        private bool _isPlaying;
        public string PlayPauseGlyph => _isPlaying ? "\uE769" : "\uE768"; // Pause / Play

        private double _playbackSpeed = 1.0;

        // Clip range
        private double? _clipStartSec;
        private double? _clipEndSec;

        public string TimelineLabel
        {
            get
            {
                var cur = TimeSpan.FromSeconds(Math.Max(0, TimelineValue));
                var max = TimeSpan.FromSeconds(Math.Max(0, TimelineMax));
                return $"Playback Speed: {_playbackSpeed:0.##} / {cur:mm\\:ss} / {max:mm\\:ss}";
            }
        }

        // ====== Commands ======
        public ICommand LoadVideoCommand { get; }
        public ICommand TogglePlayCommand { get; }
        public ICommand SeekCommand { get; }
        public ICommand SetSpeedCommand { get; }

        public ICommand SetClipStartCommand { get; }
        public ICommand SetClipEndCommand { get; }
        public ICommand SaveTeamACommand { get; }
        public ICommand SaveTeamBCommand { get; }

        // placeholders (compile-safe)
        public ICommand ImportCsvCommand { get; }
        public ICommand ExportCsvCommand { get; }
        public ICommand ExportAllCommand { get; }
        public ICommand PreferencesCommand { get; }

        public MainWindowViewModel(MainWindow view)
        {
            _view = view;

            LoadVideoCommand = new SimpleCommand(_ => LoadVideo());
            TogglePlayCommand = new SimpleCommand(_ => TogglePlayPause());
            SeekCommand = new SimpleCommand(p => SeekBy(Convert.ToDouble(p)));
            SetSpeedCommand = new SimpleCommand(p => SetSpeed(Convert.ToDouble(p)));

            SetClipStartCommand = new SimpleCommand(_ => SetClipStart());
            SetClipEndCommand = new SimpleCommand(_ => SetClipEnd());
            SaveTeamACommand = new SimpleCommand(_ => SaveClip(isTeamA: true));
            SaveTeamBCommand = new SimpleCommand(_ => SaveClip(isTeamA: false));

            ImportCsvCommand = new SimpleCommand(_ => MessageBox.Show("Import CSV: next"));
            ExportCsvCommand = new SimpleCommand(_ => MessageBox.Show("Export CSV: next"));
            ExportAllCommand = new SimpleCommand(_ => MessageBox.Show("Export All: next"));
            PreferencesCommand = new SimpleCommand(_ => MessageBox.Show("Preferences: next"));

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += (_, __) => SyncTimelineFromPlayer();
            _timer.Start();

            // ✅ 起動直後のデフォルト速度 1x（RadioButton is checked でも来るが保険）
            _playbackSpeed = 1.0;
            ApplySpeed();
        }

        public void OnMediaOpened()
        {
            if (_view.Player.NaturalDuration.HasTimeSpan)
            {
                TimelineMax = _view.Player.NaturalDuration.TimeSpan.TotalSeconds;
            }
            else
            {
                TimelineMax = 1;
            }

            TimelineValue = 0;
            _isPlaying = false;
            OnPropertyChanged(nameof(PlayPauseGlyph));
            ApplySpeed();
        }

        public void OnMediaEnded()
        {
            _isPlaying = false;
            OnPropertyChanged(nameof(PlayPauseGlyph));
        }

        private void SyncTimelineFromPlayer()
        {
            if (IsDraggingTimeline) return;
            if (_view.Player.Source == null) return;

            try
            {
                TimelineValue = _view.Player.Position.TotalSeconds;
            }
            catch
            {
                // ignore
            }
        }

        private void LoadVideo()
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Video Files|*.mp4;*.mov;*.m4v;*.wmv;*.avi|All Files|*.*"
            };

            if (ofd.ShowDialog() != true) return;

            _view.Player.Source = new Uri(ofd.FileName);
            _view.Player.Position = TimeSpan.Zero;

            _isPlaying = false;
            OnPropertyChanged(nameof(PlayPauseGlyph));

            // 自動再生しない（Mac版の挙動に寄せるなら、ここは好みで変更OK）
            _view.Player.Stop();

            ApplySpeed();
        }

        private void TogglePlayPause()
        {
            if (_view.Player.Source == null) return;

            if (_isPlaying)
            {
                _view.Player.Pause();
                _isPlaying = false;
            }
            else
            {
                _view.Player.Play();
                _isPlaying = true;
            }

            OnPropertyChanged(nameof(PlayPauseGlyph));
        }

        private void SeekBy(double seconds)
        {
            if (_view.Player.Source == null) return;

            var next = _view.Player.Position.TotalSeconds + seconds;
            SeekTo(next);
        }

        public void SeekTo(double seconds)
        {
            if (_view.Player.Source == null) return;

            if (seconds < 0) seconds = 0;
            if (seconds > TimelineMax) seconds = TimelineMax;

            _view.Player.Position = TimeSpan.FromSeconds(seconds);
            TimelineValue = seconds;
        }

        private void SetSpeed(double speed)
        {
            _playbackSpeed = speed;
            ApplySpeed();
            OnPropertyChanged(nameof(TimelineLabel));
        }

        private void ApplySpeed()
        {
            try
            {
                _view.Player.SpeedRatio = _playbackSpeed;
            }
            catch
            {
                // some formats may not support SpeedRatio
            }
        }

        private void SetClipStart()
        {
            if (_view.Player.Source == null) return;
            _clipStartSec = _view.Player.Position.TotalSeconds;
        }

        private void SetClipEnd()
        {
            if (_view.Player.Source == null) return;
            _clipEndSec = _view.Player.Position.TotalSeconds;
        }

        private void SaveClip(bool isTeamA)
        {
            if (_view.Player.Source == null) return;
            if (_clipStartSec == null || _clipEndSec == null) return;

            var s = Math.Min(_clipStartSec.Value, _clipEndSec.Value);
            var e = Math.Max(_clipStartSec.Value, _clipEndSec.Value);

            var item = new ClipItem
            {
                StartSec = s,
                EndSec = e,
                TagsLabel = "" // 次で Current Tags と連動させる
            };

            if (isTeamA) TeamAClips.Add(item);
            else TeamBClips.Add(item);
        }

        // ✅ 次の処理：リストダブルクリックで Start にジャンプして再生
        public void JumpToSelectedClip(bool isTeamA)
        {
            var clip = isTeamA ? SelectedTeamAClip : SelectedTeamBClip;
            if (clip == null) return;

            SeekTo(clip.StartSec);
            if (_view.Player.Source != null)
            {
                _view.Player.Play();
                _isPlaying = true;
                OnPropertyChanged(nameof(PlayPauseGlyph));
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ClipItem
    {
        public double StartSec { get; set; }
        public double EndSec { get; set; }
        public string TagsLabel { get; set; } = "";

        public string StartLabel => TimeSpan.FromSeconds(StartSec).ToString(@"mm\:ss");
        public string EndLabel => TimeSpan.FromSeconds(EndSec).ToString(@"mm\:ss");
    }

    // 既存の RelayCommand と衝突しないように別名で用意
    public sealed class SimpleCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public SimpleCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
