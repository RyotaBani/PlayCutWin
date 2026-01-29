using System;
using System.Collections.Generic;
using System.Linq;

namespace PlayCutWin.Models
{
    public enum ShortcutAction
    {
        LoadVideo,
        PlayPause,

        // frame step
        FrameBack,
        FrameForward,

        // seconds seek
        SeekMinus1,
        SeekPlus1,
        SeekMinus5,
        SeekPlus5,

        ClipStart,
        ClipEnd,
        SaveTeamA,
        SaveTeamB,

        AddCustomTag,
        ClearTags,

        ImportCsv,
        ExportCsv,
        ExportAll,

        OpenPreferences,
    }

    public sealed class ShortcutItem
    {
        public ShortcutAction Action { get; set; }
        public string Gesture { get; set; } = "";
    }

    public sealed class ShortcutRow
    {
        public ShortcutAction Action { get; set; }
        public string Title { get; set; } = "";
        public string Gesture { get; set; } = "";
    }

    public static class ShortcutCatalog
    {
        public static IReadOnlyList<ShortcutRow> DefaultRows => new List<ShortcutRow>
        {
            Row(ShortcutAction.LoadVideo, "Load Video"),
            Row(ShortcutAction.PlayPause, "Play / Pause"),
            Row(ShortcutAction.FrameBack, "Frame Back"),
            Row(ShortcutAction.FrameForward, "Frame Forward"),
            Row(ShortcutAction.SeekMinus1, "Seek -1s"),
            Row(ShortcutAction.SeekPlus1, "Seek +1s"),
            Row(ShortcutAction.SeekMinus5, "Seek -5s"),
            Row(ShortcutAction.SeekPlus5, "Seek +5s"),
            Row(ShortcutAction.ClipStart, "Clip START"),
            Row(ShortcutAction.ClipEnd, "Clip END"),
            Row(ShortcutAction.SaveTeamA, "Save Team A"),
            Row(ShortcutAction.SaveTeamB, "Save Team B"),
            Row(ShortcutAction.AddCustomTag, "Add Custom Tag"),
            Row(ShortcutAction.ClearTags, "Clear Tags"),
            Row(ShortcutAction.ImportCsv, "Import CSV"),
            Row(ShortcutAction.ExportCsv, "Export CSV"),
            Row(ShortcutAction.ExportAll, "Export All"),
            Row(ShortcutAction.OpenPreferences, "Preferences"),
        };

        private static ShortcutRow Row(ShortcutAction action, string title)
            => new ShortcutRow { Action = action, Title = title };

        public static IReadOnlyList<ShortcutRow> MergeGestures(IEnumerable<ShortcutItem> items)
        {
            var map = items
                .Where(i => !string.IsNullOrWhiteSpace(i.Gesture))
                .GroupBy(i => i.Action)
                .ToDictionary(g => g.Key, g => g.Last().Gesture);

            var merged = DefaultRows
                .Select(r => new ShortcutRow
                {
                    Action = r.Action,
                    Title = r.Title,
                    Gesture = map.TryGetValue(r.Action, out var g) ? g : ""
                })
                .ToList();

            return merged;
        }
    }
}
