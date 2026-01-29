using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace PlayCutWin.Services
{
    /// <summary>
    /// Loads/saves user-editable keyboard shortcuts (Mac-like workflow).
    /// Stored as a simple mapping: actionKey -> gesture string (e.g. "Ctrl+O", "Space").
    /// </summary>
    public sealed class ShortcutManager
    {
        public const string AppFolderName = "PlayCutWin";
        public const string FileName = "shortcuts.json";

        private readonly Dictionary<string, string> _actionToGestureText = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _gestureTextToAction = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, string> ActionToGestureText => _actionToGestureText;

        public static string GetConfigFolder()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(baseDir, AppFolderName);
        }

        public static string GetConfigPath() => Path.Combine(GetConfigFolder(), FileName);

        public void LoadOrCreateDefaults(Dictionary<string, string> defaults)
        {
            _actionToGestureText.Clear();
            foreach (var kv in defaults)
                _actionToGestureText[kv.Key] = kv.Value;

            var path = GetConfigPath();
            if (File.Exists(path))
            {
                try
                {
                    var json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                    foreach (var kv in loaded)
                    {
                        if (!string.IsNullOrWhiteSpace(kv.Key) && !string.IsNullOrWhiteSpace(kv.Value))
                            _actionToGestureText[kv.Key] = NormalizeGestureText(kv.Value);
                    }
                }
                catch
                {
                    // ignore; fall back to defaults
                }
            }

            RebuildReverseMap();
        }

        public void Save()
        {
            Directory.CreateDirectory(GetConfigFolder());
            var path = GetConfigPath();
            var json = JsonSerializer.Serialize(_actionToGestureText, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        public void ResetTo(Dictionary<string, string> defaults)
        {
            _actionToGestureText.Clear();
            foreach (var kv in defaults)
                _actionToGestureText[kv.Key] = NormalizeGestureText(kv.Value);

            RebuildReverseMap();
            Save();
        }

        public void SetMapping(IEnumerable<(string actionKey, string gestureText)> mappings)
        {
            _actionToGestureText.Clear();
            foreach (var (actionKey, gestureText) in mappings)
            {
                if (string.IsNullOrWhiteSpace(actionKey)) continue;
                if (string.IsNullOrWhiteSpace(gestureText)) continue;

                _actionToGestureText[actionKey] = NormalizeGestureText(gestureText);
            }

            RebuildReverseMap();
        }

        public bool TryGetAction(KeyEventArgs e, out string actionKey)
        {
            actionKey = string.Empty;

            var mods = Keyboard.Modifiers;
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // Ignore pure modifier presses
            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
                return false;

            var gestureText = NormalizeGestureText(FormatGesture(mods, key));
            if (_gestureTextToAction.TryGetValue(gestureText, out var found))
            {
                actionKey = found;
                return true;
            }
            return false;
        }

        public static string NormalizeGestureText(string s)
        {
            // Use WPF's KeyGestureConverter to normalize to a stable string.
            try
            {
                var conv = new KeyGestureConverter();
                var kg = conv.ConvertFromString(s) as KeyGesture;
                if (kg == null) return s.Trim();

                // Convert back to string
                var back = conv.ConvertToString(kg);
                return string.IsNullOrWhiteSpace(back) ? s.Trim() : back.Trim();
            }
            catch
            {
                return s.Trim();
            }
        }

        private static string FormatGesture(ModifierKeys mods, Key key)
        {
            // Keep it compatible with KeyGestureConverter.
            string k = key.ToString();
            if (key == Key.Space) k = "Space";
            if (key == Key.OemPlus) k = "OemPlus";
            if (key == Key.OemMinus) k = "OemMinus";
            if (key == Key.OemComma) k = "OemComma";
            if (key == Key.OemPeriod) k = "OemPeriod";

            // Build with "+" separators
            var parts = new List<string>();
            if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            parts.Add(k);
            return string.Join("+", parts);
        }

        private void RebuildReverseMap()
        {
            _gestureTextToAction.Clear();
            foreach (var kv in _actionToGestureText)
            {
                var action = kv.Key;
                var gesture = NormalizeGestureText(kv.Value);

                if (string.IsNullOrWhiteSpace(action) || string.IsNullOrWhiteSpace(gesture))
                    continue;

                // Only accept gestures we can parse (avoids crashes)
                try
                {
                    var conv = new KeyGestureConverter();
                    var kg = conv.ConvertFromString(gesture) as KeyGesture;
                    if (kg == null) continue;

                    var normalized = NormalizeGestureText(conv.ConvertToString(kg) ?? gesture);
                    _gestureTextToAction[normalized] = action;
                }
                catch
                {
                    // ignore invalid gesture
                }
            }
        }
    }
}
