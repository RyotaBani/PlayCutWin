using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayCutWin.Models;

public sealed class Clip
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public double StartSeconds { get; set; }
    public double EndSeconds { get; set; }
    public ClipTeam Team { get; set; } = ClipTeam.TeamA;
    public List<string> Tags { get; set; } = new();

    public double DurationSeconds => Math.Max(0, EndSeconds - StartSeconds);

    public string TagsDisplay => string.Join(", ", Tags);

    public string TimeRangeDisplay => $"{FormatTime(StartSeconds)} → {FormatTime(EndSeconds)} ({DurationSeconds:0.00}s)";

    public static string FormatTime(double seconds)
    {
        if (seconds < 0) seconds = 0;
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.TotalHours >= 1
            ? ts.ToString(@"hh\:mm\:ss\.ff")
            : ts.ToString(@"mm\:ss\.ff");
    }

    public Clip Clone()
    {
        return new Clip
        {
            Id = this.Id,
            StartSeconds = this.StartSeconds,
            EndSeconds = this.EndSeconds,
            Team = this.Team,
            Tags = this.Tags.ToList()
        };
    }
}
