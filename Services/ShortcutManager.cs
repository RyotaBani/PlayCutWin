using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using PlayCutWin.Models;

namespace PlayCutWin.Services
{
    public sealed class ShortcutManager
    {
        private readonly string _filePath;

        // In-memory map: gesture string (normalized) -> action
        private Dictionary<string, ShortcutAction> _map = new(StringComparer.OrdinalIgnoreCase);

        public ShortcutManager(string appName = "PlayCutWin")
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                appName);
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "shortcuts.json");
        }

        public IReadOnlyDictionary<string, ShortcutAction> Map => _map;

        public List<ShortcutItem> LoadOrCreateDefaults()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var json = File.ReadAllText(_filePath);
                    var items = JsonSerializer.Deserialize<List<ShortcutItem>>(json) ?? new List<ShortcutItem>();
                    Apply(items);
                    return items;
                }
            }
            catch
            {
                // fall back to defaults
            }

            var defaults = GetDefaults();
            Apply(defaults);
            Save(defaults);
            return defaults;
        }

        public void Save(IEnumerable<ShortcutItem> items)
        {
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public void Apply(IEnumerable<ShortcutItem> items)
        {
            _map = new Dictionary<string, ShortcutAction>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in items)
            {
                var key = NormalizeGesture(it.Gesture);
                if (string.IsNullOrWhiteSpace(key)) continue;
                _map[key] = it.Action;
            }
        }

        public bool TryResolve(KeyEventArgs e, out ShortcutAction action)
        {
            action = default;
            var gesture = FromKeyEvent(e);
            var norm = NormalizeGesture(gesture);
            return !string.IsNullOrEmpty(norm) && _map.TryGetValue(norm, out action);
        }

        public static string FromKeyEvent(KeyEventArgs e)
        {
            // ignore modifier-only
            if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
                return "";

            var parts = new List<string>();
            var mods = Keyboard.Modifiers;
            if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");

            // Prefer SystemKey when Alt is involved
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            parts.Add(KeyToText(key));
            return string.Join("+", parts);
        }

        public static string NormalizeGesture(string? gesture)
        {
            if (string.IsNullOrWhiteSpace(gesture)) return "";

            // Normalize separators/spaces and order modifiers
            var tokens = gesture
                .Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            bool ctrl = tokens.Any(t => t.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || t.Equals("Control", StringComparison.OrdinalIgnoreCase));
            bool shift = tokens.Any(t => t.Equals("Shift", StringComparison.OrdinalIgnoreCase));
            bool alt = tokens.Any(t => t.Equals("Alt", StringComparison.OrdinalIgnoreCase));

            var keyToken = tokens.FirstOrDefault(t =>
                !t.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) &&
                !t.Equals("Control", StringComparison.OrdinalIgnoreCase) &&
                !t.Equals("Shift", StringComparison.OrdinalIgnoreCase) &&
                !t.Equals("Alt", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(keyToken)) return "";

            var parts = new List<string>();
            if (ctrl) parts.Add("Ctrl");
            if (shift) parts.Add("Shift");
            if (alt) parts.Add("Alt");
            parts.Add(keyToken);
            return string.Join("+", parts);
        }

        public static string KeyToText(Key key)
        {
            // Make a few keys nicer
            return key switch
            {
                Key.Space => "Space",
                Key.OemPlus => "+",
                Key.OemMinus => "-",
                Key.Return => "Enter",
                Key.Escape => "Esc",
                _ => key.ToString()
            };
        }

        private static List<ShortcutItem> GetDefaults() => new()
        {
            new ShortcutItem { Action = ShortcutAction.PlayPause, Gesture = "Space" },
            new ShortcutItem { Action = ShortcutAction.SeekMinus5, Gesture = "Left" },
            new ShortcutItem { Action = ShortcutAction.SeekPlus5, Gesture = "Right" },
            new ShortcutItem { Action = ShortcutAction.SeekMinus1, Gesture = "Ctrl+Left" },
            new ShortcutItem { Action = ShortcutAction.SeekPlus1, Gesture = "Ctrl+Right" },
            new ShortcutItem { Action = ShortcutAction.ClipStart, Gesture = "S" },
            new ShortcutItem { Action = ShortcutAction.ClipEnd, Gesture = "E" },
            new ShortcutItem { Action = ShortcutAction.SaveTeamA, Gesture = "A" },
            new ShortcutItem { Action = ShortcutAction.SaveTeamB, Gesture = "B" },
            new ShortcutItem { Action = ShortcutAction.ExportAll, Gesture = "Ctrl+E" },
            new ShortcutItem { Action = ShortcutAction.FocusCustomTag, Gesture = "Ctrl+L" },
        };
    }
}
