using CommunityToolkit.Mvvm.ComponentModel;
using PlayCutWin.Models;

namespace PlayCutWin.ViewModels;

public partial class ClipViewModel : ObservableObject
{
    public Clip Model { get; }

    public ClipViewModel(Clip model)
    {
        Model = model;
    }

    public Guid Id => Model.Id;

    public string TimeRangeDisplay => Model.TimeRangeDisplay;
    public string TagsDisplay => Model.TagsDisplay;
    public ClipTeam Team => Model.Team;

    public string TeamDisplay => Team == ClipTeam.TeamA ? "A" : "B";
}
