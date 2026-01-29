using System;

namespace PlayCutWin.Models
{
    /// <summary>
    /// View-model row for Preferences shortcuts grid.
    /// WPF binding target used by PreferencesWindow.
    /// </summary>
    public class ShortcutRow
    {
        public ShortcutAction Action { get; }
        public string ActionLabel { get; }

        /// <summary>
        /// Human-readable gesture string (e.g. "Ctrl+S").
        /// Stored back to ShortcutManager as-is.
        /// </summary>
        public string Gesture { get; set; }

        public ShortcutRow(ShortcutAction action, string actionLabel, string gesture)
        {
            Action = action;
            ActionLabel = actionLabel ?? action.ToString();
            Gesture = gesture ?? string.Empty;
        }

        public static string Label(ShortcutAction action)
        {
            // Keep labels in one place. If you already have labels elsewhere,
            // feel free to align these to the Mac app's wording.
            return action switch
            {
                ShortcutAction.PlayPause => "Play / Pause",
                ShortcutAction.SeekMinus1 => "Seek -1s",
                ShortcutAction.SeekPlus1 => "Seek +1s",
                ShortcutAction.SeekMinus5 => "Seek -5s",
                ShortcutAction.SeekPlus5 => "Seek +5s",
                ShortcutAction.ClipStart => "Clip START",
                ShortcutAction.ClipEnd => "Clip END",
                ShortcutAction.SaveTeamA => "Save Team A",
                ShortcutAction.SaveTeamB => "Save Team B",
                ShortcutAction.ExportAll => "Export All",
                ShortcutAction.LoadVideo => "Load Video",
                ShortcutAction.OpenPreferences => "Preferences",
                _ => action.ToString()
            };
        }
    }
}
