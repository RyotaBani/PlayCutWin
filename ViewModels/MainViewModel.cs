using Microsoft.Win32;
using PlayCutWin.Helpers;
using PlayCutWin.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace PlayCutWin.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // ====== 状態 ======
        // 上部ヘッダーに表示するタイトル
        // 未読み込み時は「Video (16:9)」、読み込み後はファイル名に切り替える
        private string _videoTitle = "";
        public string VideoTitle
        {
            get => _videoTitle;
            set { _videoTitle = value; OnPropertyChanged(nameof(VideoTitle)); }
        }

        // ビデオ表示領域の中央に出す案内文（Mac版の挙動に合わせる）
        public string VideoOverlayText => "Load video from the button →";

        private string _teamAName = "";
        public string TeamAName
        {
            get => _teamAName;
            set { _teamAName = value; OnPropertyChanged(nameof(TeamAName)); }
        }

        private string _teamBName = "";
        public string TeamBName
        {
            get => _teamBName;
            set { _teamBName = value; OnPropertyChanged(nameof(TeamBName)); }
        }

        private bool _isVideoLoaded;
        public bool IsVideoLoaded
        {
            get => _isVideoLoaded;
            set
            {
                _isVideoLoaded = value;
                OnPropertyChanged(nameof(IsVideoLoaded));
                UpdateCommandStates();
            }
        }

        private double _speedRatio = 1.0;
        public double SpeedRatio
        {
            get => _speedRatio;
            set { _speedRatio = value; OnPropertyChanged(nameof(SpeedRatio)); }
        }

        private TimeSpan? _clipStart;
        public TimeSpan? ClipStart
        {
            get => _clipStart;
            set { _clipStart = value; OnPropertyChanged(nameof(ClipStart)); OnPropertyChanged(nameof(ClipStartText)); }
        }

        private TimeSpan? _clipEnd;
        public TimeSpan? ClipEnd
        {
            get => _clipEnd;
            set { _clipEnd = value; OnPropertyChanged(nameof(ClipEnd)); OnPropertyChanged(nameof(ClipEndText)); }
        }

        public string ClipStartText => ClipStart.HasValue ? ClipStart.Value.ToString(@"mm\:ss") : "----";
        public string ClipEndText => ClipEnd.HasValue ? ClipEnd.Value.ToString(@"mm\:ss") : "----";

        private string _customTag = "";
        public string CustomTag
        {
            get => _customTag;
            set { _customTag = value; OnPropertyChanged(nameof(CustomTag)); }
        }

        // ====== Clips ======
        public ObservableCollection<Clip> TeamAClips { get; } = new();
        public ObservableCollection<Clip> TeamBClips { get; } = new();

        // ====== Tags ======
        public ObservableCollection<TagItem> Tags { get; } = new();

        // ====== コマンド ======
        public RelayCommand LoadVideoCommand { get; }
        public RelayCommand ImportCsvCommand { get; }
        public RelayCommand ExportCsvCommand { get; }
        public RelayCommand ExportAllCommand { get; }

        public RelayCommand SetSpeed025Command { get; }
        public RelayCommand SetSpeed05Command { get; }
        public RelayCommand SetSpeed1Command { get; }
        public RelayCommand SetSpeed2Command { get; }

        public RelayCommand SeekMinus5Command { get; }
        public RelayCommand SeekMinus1Command { get; }
        public RelayCommand SeekPlus1Command { get; }
        public RelayCommand SeekPlus5Command { get; }

        public RelayCommand ClipStartCommand { get; }
        public RelayCommand ClipEndCommand { get; }
        public RelayCommand SaveTeamACommand { get; }
        public RelayCommand SaveTeamBCommand { get; }
        public RelayCommand ClearTagsCommand { get; }
        public RelayCommand AddCustomTagCommand { get; }

        // MainWindow側で注入される（Video操作）
        public Action<string>? RequestLoadVideo { get; set; }
        public Func<TimeSpan>? GetCurrentPosition { get; set; }
        public Action<TimeSpan>? SeekTo { get; set; }

        public MainViewModel()
        {
            // 初期タグ（Mac例に寄せた）
            Tags.Add(new TagItem("Transition", TagGroup.Offense));
            Tags.Add(new TagItem("Set", TagGroup.Offense));
            Tags.Add(new TagItem("PnR", TagGroup.Offense));
            Tags.Add(new TagItem("BLOB", TagGroup.Offense));
            Tags.Add(new TagItem("SLOB", TagGroup.Offense));
            Tags.Add(new TagItem("vs M/M", TagGroup.Offense));
            Tags.Add(new TagItem("vs Zone", TagGroup.Offense));
            Tags.Add(new TagItem("2nd Attack", TagGroup.Offense));
            Tags.Add(new TagItem("3rd Attack more", TagGroup.Offense));

            Tags.Add(new TagItem("M/M", TagGroup.Defense));
            Tags.Add(new TagItem("Zone", TagGroup.Defense));
            Tags.Add(new TagItem("Rebound", TagGroup.Defense));
            Tags.Add(new TagItem("Steal", TagGroup.Defense));

            LoadVideoCommand = new RelayCommand(LoadVideo);
            ImportCsvCommand = new RelayCommand(() => MessageBox.Show("Import CSV（仮）", "Play Cut"));
            ExportCsvCommand = new RelayCommand(() => MessageBox.Show("Export CSV（仮）", "Play Cut"), () => IsVideoLoaded);
            ExportAllCommand = new RelayCommand(() => MessageBox.Show("Export All（仮）", "Play Cut"), () => IsVideoLoaded);

            SetSpeed025Command = new RelayCommand(() => SpeedRatio = 0.25);
            SetSpeed05Command = new RelayCommand(() => SpeedRatio = 0.5);
            SetSpeed1Command = new RelayCommand(() => SpeedRatio = 1.0);
            SetSpeed2Command = new RelayCommand(() => SpeedRatio = 2.0);

            SeekMinus5Command = new RelayCommand(() => SeekRelative(-5), () => IsVideoLoaded);
            SeekMinus1Command = new RelayCommand(() => SeekRelative(-1), () => IsVideoLoaded);
            SeekPlus1Command = new RelayCommand(() => SeekRelative(1), () => IsVideoLoaded);
            SeekPlus5Command = new RelayCommand(() => SeekRelative(5), () => IsVideoLoaded);

            ClipStartCommand = new RelayCommand(() => ClipStart = GetCurrentPosition?.Invoke(), () => IsVideoLoaded);
            ClipEndCommand = new RelayCommand(() => ClipEnd = GetCurrentPosition?.Invoke(), () => IsVideoLoaded);

            SaveTeamACommand = new RelayCommand(() => SaveClip(ClipTeam.TeamA), CanSaveClip);
            SaveTeamBCommand = new RelayCommand(() => SaveClip(ClipTeam.TeamB), CanSaveClip);

            ClearTagsCommand = new RelayCommand(() =>
            {
                foreach (var t in Tags) t.IsSelected = false;
            });

            AddCustomTagCommand = new RelayCommand(() =>
            {
                var name = (CustomTag ?? "").Trim();
                if (string.IsNullOrWhiteSpace(name)) return;

                // 追加はOffense扱い（必要ならUIで切替にできる）
                Tags.Add(new TagItem(name, TagGroup.Offense));
                CustomTag = "";
            });
        }

        private void LoadVideo()
        {
            var ofd = new OpenFileDialog
            {
                Title = "Select a video",
                Filter = "Video Files|*.mp4;*.mov;*.m4v|All Files|*.*"
            };

            if (ofd.ShowDialog() != true) return;

            VideoTitle = System.IO.Path.GetFileName(ofd.FileName);
            ClipStart = null;
            ClipEnd = null;

            RequestLoadVideo?.Invoke(ofd.FileName);
        }

        private void SeekRelative(int seconds)
        {
            if (GetCurrentPosition == null || SeekTo == null) return;
            var now = GetCurrentPosition();
            var next = now + TimeSpan.FromSeconds(seconds);
            if (next < TimeSpan.Zero) next = TimeSpan.Zero;
            SeekTo(next);
        }

        private bool CanSaveClip()
        {
            if (!IsVideoLoaded) return false;
            if (!ClipStart.HasValue || !ClipEnd.HasValue) return false;
            if (ClipEnd.Value <= ClipStart.Value) return false;
            return true;
        }

        private void SaveClip(ClipTeam team)
        {
            if (!CanSaveClip()) return;

            var tags = string.Join(", ", Tags.Where(t => t.IsSelected).Select(t => t.Name));
            var clip = new Clip
            {
                Team = team,
                Start = ClipStart!.Value,
                End = ClipEnd!.Value,
                TagsText = tags
            };

            if (team == ClipTeam.TeamA) TeamAClips.Add(clip);
            else TeamBClips.Add(clip);

            // 連続作業しやすいようにEndだけ残す/StartをEndに寄せる、なども可能
            ClipStart = ClipEnd;
            ClipEnd = null;
        }

        private void UpdateCommandStates()
        {
            ExportCsvCommand.RaiseCanExecuteChanged();
            ExportAllCommand.RaiseCanExecuteChanged();
            SeekMinus5Command.RaiseCanExecuteChanged();
            SeekMinus1Command.RaiseCanExecuteChanged();
            SeekPlus1Command.RaiseCanExecuteChanged();
            SeekPlus5Command.RaiseCanExecuteChanged();
            ClipStartCommand.RaiseCanExecuteChanged();
            ClipEndCommand.RaiseCanExecuteChanged();
            SaveTeamACommand.RaiseCanExecuteChanged();
            SaveTeamBCommand.RaiseCanExecuteChanged();
        }

        public void NotifyVideoLoaded(bool loaded)
        {
            IsVideoLoaded = loaded;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
