using System;

namespace PlayCutWin.Models
{
    public enum ClipTeam
    {
        TeamA,
        TeamB
    }

    public class Clip
    {
        public ClipTeam Team { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public string TagsText { get; set; } = "";
        public string RangeText => $"{Start:mm\\:ss} - {End:mm\\:ss}";
    }
}
