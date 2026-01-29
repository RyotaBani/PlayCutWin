using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace PlayCutWin.Services
{
    public sealed class ShortcutManager
    {
        public sealed class BindingItem
        {
            public string ActionId { get; set; } = "";
            public string Gesture { get; set; } = "";
            public string Title { get; set; } = "";
            public string Category { get; set; } = "";
        }

        public static ShortcutManager Instance { get; } = new ShortcutManager();

        private readonly KeyGestureConverter _converter = new KeyGestureConverter();
        private readonly Dictionary<string, KeyGesture> _gestureByAction = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<BindingItem> _items = new();

        public IReadOnlyList<BindingItem> Items => _items;

        private ShortcutManager()
        {
            Load();
        }

        public string ConfigPath
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "PlayCutWin");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "shortcuts.json");
            }
        }

        public void Load()
        {
            _items.Clear();
            _gestureByAction.Clear();

            var defaults = DefaultBindings();

            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var loaded = JsonSerializer.Deserialize<List<BindingItem>>(json) ?? new List<BindingItem>();

                    // Merge: loaded overrides defaults by ActionId
                    var map = defaults.ToDictionary(x => x.ActionId, StringComparer.OrdinalIgnoreCase);
                    foreach (var it in loaded)
                    {
                        if (string.IsNullOrWhiteSpace(it.ActionId)) continue;
                        map[it.ActionId] = it;
                    }
                    defaults = map.Values.OrderBy(x => x.Category).ThenBy(x => x.Title).ToList();
                }
            }
            catch
            {
                // If broken JSON, fall back to defaults
            }

            foreach (var item in defaults)
            {
                _items.Add(item);
                var g = TryParseGesture(item.Gesture);
                if (g != null)
                {
                    _gestureByAction[item.ActionId] = g;
                }
            }
        }

        public void Save(IEnumerable<BindingItem> items)
        {
            var list = items
                .Where(x => !string.IsNullOrWhiteSpace(x.ActionId))
                .Select(x => new BindingItem
                {
                    ActionId = x.ActionId.Trim(),
                    Gesture = (x.Gesture ?? "").Trim(),
                    Title = x.Title ?? "",
                    Category = x.Category ?? ""
                })
                .ToList();

            var json = JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);

            Load();
        }

        public string? FindActionId(KeyEventArgs e)
        {
            // Convert KeyEventArgs to a KeyGesture-like signature
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;
            if (key == Key.None) return null;

            var modifiers = Keyboard.Modifiers;

            foreach (var item in _items)
            {
                var g = TryParseGesture(item.Gesture);
                if (g == null) continue;

                if (g.Key == key && g.Modifiers == modifiers)
                {
                    return item.ActionId;
                }
            }

            return null;
        }

        public KeyGesture? TryParseGesture(string gesture)
        {
            if (string.IsNullOrWhiteSpace(gesture)) return null;
            try
            {
                return _converter.ConvertFromString(gesture) as KeyGesture;
            }
            catch
            {
                return null;
            }
        }

        private static List<BindingItem> DefaultBindings()
        {
            return new List<BindingItem>
            {
                new() { Category="Playback", Title="Play/Pause", ActionId="play_pause", Gesture="Space" },
                new() { Category="Playback", Title="Seek -1s", ActionId="seek_minus_1", Gesture="Left" },
                new() { Category="Playback", Title="Seek +1s", ActionId="seek_plus_1", Gesture="Right" },
                new() { Category="Playback", Title="Seek -5s", ActionId="seek_minus_5", Gesture="Shift+Left" },
                new() { Category="Playback", Title="Seek +5s", ActionId="seek_plus_5", Gesture="Shift+Right" },

                new() { Category="Clips", Title="Mark Clip Start", ActionId="clip_start", Gesture="I" },
                new() { Category="Clips", Title="Mark Clip End", ActionId="clip_end", Gesture="O" },
                new() { Category="Clips", Title="Save Team A Clip", ActionId="save_team_a", Gesture="A" },
                new() { Category="Clips", Title="Save Team B Clip", ActionId="save_team_b", Gesture="B" },
                new() { Category="Clips", Title="Jump to Selected Clip", ActionId="jump_selected", Gesture="Enter" },
                new() { Category="Clips", Title="Delete Selected Clip", ActionId="delete_clip", Gesture="Delete" },

                new() { Category="File", Title="Load Video", ActionId="load_video", Gesture="Ctrl+L" },
                new() { Category="File", Title="Import CSV", ActionId="import_csv", Gesture="Ctrl+I" },
                new() { Category="File", Title="Export CSV", ActionId="export_csv", Gesture="Ctrl+E" },
                new() { Category="File", Title="Export All Clips", ActionId="export_all", Gesture="Ctrl+Shift+E" },

                new() { Category="UI", Title="Open Preferences", ActionId="open_preferences", Gesture="Ctrl+Comma" },
            };
        }
    }
}
