using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using PlayCutWin.Models;

namespace PlayCutWin.Services;

/// <summary>
/// Shortcut bindings manager.
/// - Stores bindings in a JSON file under %AppData%\PlayCutWin\shortcuts.json
/// - Keeps both (Action -> Gesture) and (Gesture -> Action) lookups.
/// </summary>
public sealed class ShortcutManager
{
    private readonly string _filePath;

    // Action -> Gesture (normalized)
    private Dictionary<ShortcutAction, string> _bindings = new();

    // Gesture (normalized) -> Action
    private Dictionary<string, ShortcutAction> _map = new(StringComparer.OrdinalIgnoreCase);

    public ShortcutManager()
    {
        _filePath = GetDefaultFilePath();
        LoadOrCreate();
    }

    // ------------------- Public API used by the app -------------------

    public IReadOnlyDictionary<ShortcutAction, string> GetBindings()
        => new Dictionary<ShortcutAction, string>(_bindings);

    public void SetBinding(ShortcutAction action, string gesture)
    {
        var normalized = NormalizeGesture(gesture);
        if (string.IsNullOrWhiteSpace(normalized)) return;

        _bindings[action] = normalized;
        RebuildMap();
    }

    public void RestoreDefaults()
    {
        _bindings = DefaultBindings();
        RebuildMap();
        Save();
    }

    public void Save() => Save(_bindings.Select(kv => new ShortcutItem { Action = kv.Key, Gesture = kv.Value }));

    public void Save(IEnumerable<ShortcutItem> items)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var json = JsonSerializer.Serialize(items.ToList(), new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Non-fatal. App can still run with in-memory bindings.
        }
    }

    /// <summary>
    /// Resolve from a WPF KeyEventArgs.
    /// </summary>
    public bool TryResolve(KeyEventArgs e, out ShortcutAction action)
    {
        var gesture = FromKeyEvent(e);
        return TryGetAction(gesture, out action);
    }

    /// <summary>
    /// Resolve from a normalized gesture string (used by MainWindow key handler).
    /// </summary>
    public bool TryGetAction(string gesture, out ShortcutAction action)
    {
        var normalized = NormalizeGesture(gesture);
        return _map.TryGetValue(normalized, out action);
    }

    /// <summary>
    /// Create a display string for a key event. (Static because older call sites expect it.)
    /// </summary>
    public static string ToGestureString(KeyEventArgs e) => FromKeyEvent(e);

    // ------------------------- Internal logic -------------------------

    private void LoadOrCreate()
    {
        var defaults = DefaultBindings();
        _bindings = new Dictionary<ShortcutAction, string>(defaults);

        // Try load from file and merge.
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<List<ShortcutItem>>(json) ?? new();
                foreach (var it in list)
                {
                    var g = NormalizeGesture(it.Gesture);
                    if (string.IsNullOrWhiteSpace(g)) continue;
                    _bindings[it.Action] = g;
                }
            }
        }
        catch
        {
            // ignore and keep defaults
        }

        RebuildMap();

        // Ensure file exists.
        if (!File.Exists(_filePath)) Save();
    }

    private void RebuildMap()
    {
        _map = new Dictionary<string, ShortcutAction>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in _bindings)
        {
            var g = NormalizeGesture(kv.Value);
            if (string.IsNullOrWhiteSpace(g)) continue;
            _map[g] = kv.Key;
        }
    }

    private static Dictionary<ShortcutAction, string> DefaultBindings() => new()
    {
        // Mac-like defaults on Windows:
        // - Load Video: Ctrl+O
        // - Preferences: Ctrl+,
        { ShortcutAction.LoadVideo, "Ctrl+O" },
        { ShortcutAction.OpenPreferences, "Ctrl+," },

        { ShortcutAction.PlayPause, "Space" },
        { ShortcutAction.SeekMinus5, "Left" },
        { ShortcutAction.SeekPlus5, "Right" },
        { ShortcutAction.SeekMinus1, "Shift+Left" },
        { ShortcutAction.SeekPlus1, "Shift+Right" },
        { ShortcutAction.ClipStart, "S" },
        { ShortcutAction.ClipEnd, "E" },
        { ShortcutAction.SaveTeamA, "A" },
        { ShortcutAction.SaveTeamB, "B" },
        { ShortcutAction.ExportAll, "Ctrl+Shift+E" },
        { ShortcutAction.FocusCustomTag, "T" },
    };

    private static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "PlayCutWin", "shortcuts.json");
    }

    public static string FromKeyEvent(KeyEventArgs e)
    {
        var mods = Keyboard.Modifiers;
        string key = e.Key == Key.System ? e.SystemKey.ToString() : e.Key.ToString();

        // Normalize common keys for readability.
        if (e.Key == Key.OemComma) key = ",";
        if (e.Key == Key.OemPeriod) key = ".";
        if (e.Key == Key.OemMinus) key = "-";
        if (e.Key == Key.OemPlus) key = "+";

        var parts = new List<string>();
        if (mods.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mods.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (mods.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");

        // Use key token for arrows / space.
        if (key == nameof(Key.Space)) key = "Space";

        parts.Add(key);
        return string.Join("+", parts);
    }

    public static string NormalizeGesture(string gesture)
    {
        if (string.IsNullOrWhiteSpace(gesture)) return string.Empty;
        var raw = gesture.Trim();
        raw = raw.Replace("Command", "Ctrl", StringComparison.OrdinalIgnoreCase);
        raw = raw.Replace("Cmd", "Ctrl", StringComparison.OrdinalIgnoreCase);
        raw = raw.Replace("Control", "Ctrl", StringComparison.OrdinalIgnoreCase);
        raw = raw.Replace("Option", "Alt", StringComparison.OrdinalIgnoreCase);

        // Split and re-order modifiers in a stable order.
        var tokens = raw.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList();

        bool ctrl = tokens.Any(t => string.Equals(t, "Ctrl", StringComparison.OrdinalIgnoreCase));
        bool shift = tokens.Any(t => string.Equals(t, "Shift", StringComparison.OrdinalIgnoreCase));
        bool alt = tokens.Any(t => string.Equals(t, "Alt", StringComparison.OrdinalIgnoreCase));

        // Last non-mod token is treated as key.
        var key = tokens.LastOrDefault(t =>
            !string.Equals(t, "Ctrl", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(t, "Shift", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(t, "Alt", StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        var parts = new List<string>();
        if (ctrl) parts.Add("Ctrl");
        if (shift) parts.Add("Shift");
        if (alt) parts.Add("Alt");
        if (!string.IsNullOrWhiteSpace(key)) parts.Add(key);

        return string.Join("+", parts);
    }
}
