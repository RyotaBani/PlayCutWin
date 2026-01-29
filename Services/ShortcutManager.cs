using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using PlayCutWin.Models;

namespace PlayCutWin.Services
{
    /// <summary>
    /// ショートカット設定（Mac版の「単独キー中心」の操作感を意識）
    /// - JSONへ保存: %AppData%\PlayCutWin\shortcuts.json
    /// - 文字列表現 gesture 例: "Space", "Left", "Ctrl+S"
    /// </summary>
    public sealed class ShortcutManager
    {
        private readonly string _filePath;
        private List<ShortcutItem> _items = new();
        private Dictionary<string, ShortcutAction> _lookup = new(StringComparer.OrdinalIgnoreCase);

        public ShortcutManager()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlayCutWin");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "shortcuts.json");
        }

        /// <summary>現在のバインド一覧</summary>
        public IReadOnlyList<ShortcutItem> GetBindings() => _items;

        /// <summary>
        /// 起動時に呼ぶ。保存が無ければデフォルトを作って保存。
        /// 保存があれば読み込み、足りないアクションはデフォルトで補完。
        /// </summary>
        public List<ShortcutItem> LoadOrCreateDefaults()
        {
            var defaults = CreateDefaults();

            if (File.Exists(_filePath))
            {
                try
                {
                    var json = File.ReadAllText(_filePath);
                    var loaded = JsonSerializer.Deserialize<List<ShortcutItem>>(json) ?? new List<ShortcutItem>();

                    // 正規化 & 重複排除（後勝ち）
                    var mergedByAction = new Dictionary<ShortcutAction, ShortcutItem>();
                    foreach (var it in loaded)
                    {
                        if (!Enum.IsDefined(typeof(ShortcutAction), it.Action)) continue;
                        var g = NormalizeGestureString(it.Gesture);
                        if (string.IsNullOrWhiteSpace(g)) continue;
                        mergedByAction[it.Action] = new ShortcutItem { Action = it.Action, Gesture = g };
                    }

                    // 足りないアクションを追加
                    foreach (var d in defaults)
                    {
                        if (!mergedByAction.ContainsKey(d.Action))
                            mergedByAction[d.Action] = d;
                    }

                    // gesture重複を解消（同一gestureは最初のものを残し、それ以外は空にする）
                    var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var finalList = new List<ShortcutItem>();
                    foreach (var it in mergedByAction.Values.OrderBy(v => (int)v.Action))
                    {
                        if (string.IsNullOrWhiteSpace(it.Gesture))
                        {
                            finalList.Add(it);
                            continue;
                        }
                        if (used.Add(it.Gesture))
                        {
                            finalList.Add(it);
                        }
                        else
                        {
                            // 競合したら未割当扱い
                            finalList.Add(new ShortcutItem { Action = it.Action, Gesture = string.Empty });
                        }
                    }

                    _items = finalList;
                    RebuildLookup();
                    Save(_items);
                    return _items;
                }
                catch
                {
                    // 壊れていたらデフォルトに戻す
                }
            }

            _items = defaults;
            RebuildLookup();
            Save(_items);
            return _items;
        }

        public void Save()
        {
            Save(_items);
        }

        public void Save(IEnumerable<ShortcutItem> items)
        {
            _items = items
                .Select(i => new ShortcutItem { Action = i.Action, Gesture = NormalizeGestureString(i.Gesture) })
                .ToList();

            // gesture重複は落とす（後勝ち→先勝ちに整える）
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int idx = 0; idx < _items.Count; idx++)
            {
                var g = _items[idx].Gesture;
                if (string.IsNullOrWhiteSpace(g)) continue;
                if (!seen.Add(g)) _items[idx].Gesture = string.Empty;
            }

            RebuildLookup();

            var json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public void RestoreDefaults()
        {
            _items = CreateDefaults();
            RebuildLookup();
            Save(_items);
        }

        public void SetBinding(ShortcutAction action, string gesture)
        {
            var g = NormalizeGestureString(gesture);
            var found = _items.FirstOrDefault(i => i.Action == action);
            if (found == null)
            {
                _items.Add(new ShortcutItem { Action = action, Gesture = g });
            }
            else
            {
                found.Gesture = g;
            }

            // 競合解消（同じgestureが他にあれば空にする）
            if (!string.IsNullOrWhiteSpace(g))
            {
                foreach (var it in _items)
                {
                    if (it.Action != action && string.Equals(it.Gesture, g, StringComparison.OrdinalIgnoreCase))
                        it.Gesture = string.Empty;
                }
            }

            RebuildLookup();
        }

        public bool TryGetAction(string gesture, out ShortcutAction action)
        {
            action = ShortcutAction.None;
            var g = NormalizeGestureString(gesture);
            if (string.IsNullOrWhiteSpace(g)) return false;
            return _lookup.TryGetValue(g, out action);
        }

        private void RebuildLookup()
        {
            _lookup = new Dictionary<string, ShortcutAction>(StringComparer.OrdinalIgnoreCase);
            foreach (var it in _items)
            {
                if (it.Action == ShortcutAction.None) continue;
                var g = NormalizeGestureString(it.Gesture);
                if (string.IsNullOrWhiteSpace(g)) continue;
                _lookup[g] = it.Action; // 後勝ち
            }
        }

        public static string ToGestureString(KeyEventArgs e)
        {
            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // 修飾キー単体は無視
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin)
            {
                return string.Empty;
            }

            var parts = new List<string>(4);
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) parts.Add("Ctrl");
            if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) parts.Add("Alt");
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) parts.Add("Shift");

            // Mac版に寄せたいので、Ctrl無しの単独キーが基本
            parts.Add(KeyToString(key));
            return NormalizeGestureString(string.Join("+", parts));
        }

        private static string KeyToString(Key key)
        {
            return key switch
            {
                Key.Space => "Space",
                Key.Return => "Enter",
                Key.Escape => "Esc",
                Key.Back => "Backspace",
                Key.Delete => "Delete",
                Key.Left => "Left",
                Key.Right => "Right",
                Key.Up => "Up",
                Key.Down => "Down",
                _ => key.ToString()
            };
        }

        public static string NormalizeGestureString(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            // 例: "ctrl +  s" を "Ctrl+S" に揃える
            var rawParts = s.Split('+', StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .ToList();

            var mods = new List<string>(3);
            string? key = null;

            foreach (var p in rawParts)
            {
                var lower = p.ToLowerInvariant();
                if (lower == "ctrl" || lower == "control") { if (!mods.Contains("Ctrl")) mods.Add("Ctrl"); continue; }
                if (lower == "alt" || lower == "option") { if (!mods.Contains("Alt")) mods.Add("Alt"); continue; }
                if (lower == "shift") { if (!mods.Contains("Shift")) mods.Add("Shift"); continue; }

                key = CanonicalKeyName(p);
            }

            // 修飾キーだけは不許可
            if (key == null) return string.Empty;

            // Ctrl/Alt/Shift は順序固定
            var orderedMods = new List<string>();
            if (mods.Contains("Ctrl")) orderedMods.Add("Ctrl");
            if (mods.Contains("Alt")) orderedMods.Add("Alt");
            if (mods.Contains("Shift")) orderedMods.Add("Shift");

            orderedMods.Add(key);
            return string.Join("+", orderedMods);
        }

        private static string CanonicalKeyName(string p)
        {
            // ちょい揺れ吸収
            var lower = p.Trim().ToLowerInvariant();
            return lower switch
            {
                "enter" or "return" => "Enter",
                "esc" or "escape" => "Esc",
                "space" => "Space",
                "left" => "Left",
                "right" => "Right",
                "up" => "Up",
                "down" => "Down",
                "back" or "backspace" => "Backspace",
                "del" or "delete" => "Delete",
                _ => p.Length == 1 ? p.ToUpperInvariant() : char.ToUpperInvariant(p[0]) + p.Substring(1)
            };
        }

        private static List<ShortcutItem> CreateDefaults()
        {
            // Mac版の操作感に寄せる（Ctrlなし優先）
            return new List<ShortcutItem>
            {
                new ShortcutItem { Action = ShortcutAction.LoadVideo, Gesture = "O" },
                new ShortcutItem { Action = ShortcutAction.PlayPause, Gesture = "Space" },

                // コマ送り（左右）
                new ShortcutItem { Action = ShortcutAction.FrameBack, Gesture = "Left" },
                new ShortcutItem { Action = ShortcutAction.FrameForward, Gesture = "Right" },

                // 秒単位（Shift+左右）
                new ShortcutItem { Action = ShortcutAction.SeekMinus1, Gesture = "Shift+Left" },
                new ShortcutItem { Action = ShortcutAction.SeekPlus1, Gesture = "Shift+Right" },
                new ShortcutItem { Action = ShortcutAction.SeekMinus5, Gesture = "Alt+Left" },
                new ShortcutItem { Action = ShortcutAction.SeekPlus5, Gesture = "Alt+Right" },

                new ShortcutItem { Action = ShortcutAction.ClipStart, Gesture = "S" },
                new ShortcutItem { Action = ShortcutAction.ClipEnd, Gesture = "E" },
                new ShortcutItem { Action = ShortcutAction.SaveTeamA, Gesture = "A" },
                new ShortcutItem { Action = ShortcutAction.SaveTeamB, Gesture = "B" },

                new ShortcutItem { Action = ShortcutAction.AddCustomTag, Gesture = "Enter" },
                new ShortcutItem { Action = ShortcutAction.ClearTags, Gesture = "C" },

                new ShortcutItem { Action = ShortcutAction.ExportAll, Gesture = "X" },
                new ShortcutItem { Action = ShortcutAction.ExportCsv, Gesture = "V" },
                new ShortcutItem { Action = ShortcutAction.ImportCsv, Gesture = "I" },

                new ShortcutItem { Action = ShortcutAction.OpenPreferences, Gesture = "P" },
            };
        }
    }
}
