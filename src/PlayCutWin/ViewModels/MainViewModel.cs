using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PlayCutWin.Models;
using PlayCutWin.Services;

namespace PlayCutWin.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IFileDialogService _fileDialogs;
    private readonly ICsvClipService _csv;
    private readonly IMessageBoxService _msg;

    public MainViewModel()
    {
        _fileDialogs = new FileDialogService();
        _csv = new CsvClipService();
        _msg = new MessageBoxService();

        OffenseTags = new ObservableCollection<string>(new[]
        {
            "P&R", "ISO", "Cut", "Post", "Transition", "SpotUp", "KickOut", "3PT", "Drive", "PutBack"
        });

        DefenseTags = new ObservableCollection<string>(new[]
        {
            "Man", "Zone", "Switch", "Hedge", "Drop", "Help", "CloseOut", "Steal", "Block", "Rebound"
        });

        Rates = new ObservableCollection<double>(new[] { 0.25, 0.5, 0.75, 1.0, 1.25, 1.5, 2.0 });
        SelectedRate = 1.0;
        SelectedTeam = ClipTeam.TeamA;
    }

    // --- Player state (set from View code-behind) ---
    public Action<string>? RequestLoadVideo { get; set; }
    public Func<double>? QueryCurrentTimeSeconds { get; set; }
    public Action? RequestPlay { get; set; }
    public Action? RequestPause { get; set; }
    public Action<double>? RequestSetRate { get; set; }
    public Action<double>? RequestSeekSeconds { get; set; }

    [ObservableProperty] private string? videoPath;
    [ObservableProperty] private bool isPlaying;
    [ObservableProperty] private double selectedRate;
    [ObservableProperty] private ClipTeam selectedTeam;

    [ObservableProperty] private double? pendingStartSeconds;
    [ObservableProperty] private ClipViewModel? selectedClip;

    public ObservableCollection<ClipViewModel> Clips { get; } = new();

    public ObservableCollection<string> OffenseTags { get; }
    public ObservableCollection<string> DefenseTags { get; }
    public ObservableCollection<double> Rates { get; }

    partial void OnSelectedRateChanged(double value)
    {
        RequestSetRate?.Invoke(value);
    }

    [RelayCommand]
    private void LoadVideo()
    {
        var path = _fileDialogs.PickVideoFile();
        if (string.IsNullOrWhiteSpace(path)) return;

        VideoPath = path;
        RequestLoadVideo?.Invoke(path);
        IsPlaying = false;

        // reset state for new video
        PendingStartSeconds = null;
        Clips.Clear();
        SelectedClip = null;
    }

    [RelayCommand]
    private void PlayPause()
    {
        if (IsPlaying)
        {
            RequestPause?.Invoke();
            IsPlaying = false;
        }
        else
        {
            RequestPlay?.Invoke();
            IsPlaying = true;
        }
    }

    [RelayCommand]
    private void MarkStart()
    {
        var t = QueryCurrentTimeSeconds?.Invoke();
        if (t is null) return;
        PendingStartSeconds = t.Value;
    }

    [RelayCommand]
    private void MarkEnd()
    {
        var t = QueryCurrentTimeSeconds?.Invoke();
        if (t is null) return;

        if (PendingStartSeconds is null)
        {
            // If start is not set, treat current time as start, too.
            PendingStartSeconds = t.Value;
        }

        var start = PendingStartSeconds.Value;
        var end = t.Value;

        if (end < start)
        {
            (start, end) = (end, start);
        }

        var clip = new Clip
        {
            StartSeconds = start,
            EndSeconds = end,
            Team = SelectedTeam
        };

        var vm = new ClipViewModel(clip);
        Clips.Add(vm);
        SelectedClip = vm;

        PendingStartSeconds = null;
    }

    [RelayCommand]
    private void SetTeamA() => SelectedTeam = ClipTeam.TeamA;

    [RelayCommand]
    private void SetTeamB() => SelectedTeam = ClipTeam.TeamB;

    [RelayCommand]
    private void AddTag(string? tag)
    {
        if (SelectedClip is null) return;
        if (string.IsNullOrWhiteSpace(tag)) return;

        var model = SelectedClip.Model;
        if (!model.Tags.Contains(tag))
        {
            model.Tags.Add(tag);
            // refresh selected to update displays
            OnPropertyChanged(nameof(SelectedClip));
            RefreshClipInList(SelectedClip);
        }
    }

    [RelayCommand]
    private void RemoveSelectedClip()
    {
        if (SelectedClip is null) return;
        Clips.Remove(SelectedClip);
        SelectedClip = null;
    }

    [RelayCommand]
    private void JumpToSelected()
    {
        if (SelectedClip is null) return;
        RequestSeekSeconds?.Invoke(SelectedClip.Model.StartSeconds);
        RequestPlay?.Invoke();
        IsPlaying = true;
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var path = _fileDialogs.PickCsvToExport(defaultFileName: "clips.csv");
        if (string.IsNullOrWhiteSpace(path)) return;

        var clips = Clips.Select(vm => vm.Model.Clone()).ToList();
        await _csv.ExportAsync(path, clips);
    }

    [RelayCommand]
    private async Task ImportCsvAsync()
    {
        var path = _fileDialogs.PickCsvToImport();
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            var clips = await _csv.ImportAsync(path);

            Clips.Clear();
            foreach (var c in clips)
            {
                Clips.Add(new ClipViewModel(c));
            }

            SelectedClip = Clips.FirstOrDefault();

            if (Clips.Count == 0)
            {
                _msg.ShowInfo("CSVを読み込みましたが、クリップ行が見つかりませんでした。

・ヘッダー名（Start/End/Team/Tags など）
・Start/Endの形式（秒 or mm:ss）
を確認してください。", "Import CSV");
            }
        }
        catch (Exception ex)
        {
            _msg.ShowError("CSVの読み込みに失敗しました。

" + ex.Message, "Import CSV");
        }
    }

        SelectedClip = Clips.FirstOrDefault();
    }

    private void RefreshClipInList(ClipViewModel clipVm)
    {
        // crude refresh: replace item to force UI update when underlying model changes
        var idx = Clips.IndexOf(clipVm);
        if (idx < 0) return;
        Clips[idx] = new ClipViewModel(clipVm.Model);
        SelectedClip = Clips[idx];
    }
}
