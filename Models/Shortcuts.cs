using System;
using System.Collections.Generic;

namespace PlayCutWin.Models
{
    public enum ShortcutAction
    {
        PlayPause,
        SeekMinus5,
        SeekMinus1,
        SeekPlus1,
        SeekPlus5,
        ClipStart,
        ClipEnd,
        SaveTeamA,
        SaveTeamB,
        ExportAll,
        FocusCustomTag,
    }

    public sealed class ShortcutItem
    {
        public ShortcutAction Action { get; set; }
        public string Gesture { get; set; } = ""; // e.g. "Ctrl+Shift+P"

        public string Label => Action switch
        {
            ShortcutAction.PlayPause => "Play / Pause",
            ShortcutAction.SeekMinus5 => "Seek -5s",
            ShortcutAction.SeekMinus1 => "Seek -1s",
            ShortcutAction.SeekPlus1 => "Seek +1s",
            ShortcutAction.SeekPlus5 => "Seek +5s",
            ShortcutAction.ClipStart => "Clip START",
            ShortcutAction.ClipEnd => "Clip END",
            ShortcutAction.SaveTeamA => "Save Team A",
            ShortcutAction.SaveTeamB => "Save Team B",
            ShortcutAction.ExportAll => "Export All",
            ShortcutAction.FocusCustomTag => "Focus Custom Tag",
            _ => Action.ToString()
        };
    }
}
