using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PlayCutWin.Services;

namespace PlayCutWin.Views
{
    public partial class PreferencesWindow : Window
    {
        public sealed class ShortcutRow
        {
            public string ActionKey { get; set; } = "";
            public string Display { get; set; } = "";
            public string Gesture { get; set; } = "";
        }

        private readonly Dictionary<string, string> _defaults;
        private readonly ShortcutManager _manager;

        private ShortcutRow? _recordTarget;
        private bool _isRecording;

        public ObservableCollection<ShortcutRow> Rows { get; } = new();

        public PreferencesWindow(ShortcutManager manager, Dictionary<string, string> defaults)
        {
            InitializeComponent();

            _manager = manager;
            _defaults = defaults;

            foreach (var kv in defaults)
            {
                var action = kv.Key;
                var display = GetDisplayName(action);

                var current = manager.ActionToGestureText.TryGetValue(action, out var g) ? g : kv.Value;
                Rows.Add(new ShortcutRow
                {
                    ActionKey = action,
                    Display = display,
                    Gesture = current
                });
            }

            GridShortcuts.ItemsSource = Rows;
            TextStatus.Text = $"Config: {ShortcutManager.GetConfigPath()}";
        }

        private static string GetDisplayName(string key) => key switch
        {
            "TogglePlayPause" => "Play / Pause",
            "SeekMinus1" => "Seek -1s",
            "SeekPlus1" => "Seek +1s",
            "SeekMinus5" => "Seek -5s",
            "SeekPlus5" => "Seek +5s",
            "LoadVideo" => "Load Video",
            "ImportCsv" => "Import CSV",
            "ExportCsv" => "Export CSV",
            "ExportAll" => "Export All",
            "ClipStart" => "Clip START",
            "ClipEnd" => "Clip END",
            "SaveTeamA" => "Save Team A",
            "SaveTeamB" => "Save Team B",
            _ => key
        };

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            if (btn.DataContext is not ShortcutRow row) return;

            _recordTarget = row;
            _isRecording = true;

            TextStatus.Text = $"Recording… Press a key for “{row.Display}” (Esc to cancel)";
            this.PreviewKeyDown += PreferencesWindow_PreviewKeyDown;
        }

        private void PreferencesWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!_isRecording || _recordTarget == null) return;

            var key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            if (key == Key.Escape)
            {
                StopRecording("Recording cancelled.");
                e.Handled = true;
                return;
            }

            // Ignore modifiers alone
            if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
            {
                e.Handled = true;
                return;
            }

            var mods = Keyboard.Modifiers;
            var gesture = ShortcutManager.NormalizeGestureText($"{(mods.HasFlag(ModifierKeys.Control) ? "Ctrl+" : "")}{(mods.HasFlag(ModifierKeys.Shift) ? "Shift+" : "")}{(mods.HasFlag(ModifierKeys.Alt) ? "Alt+" : "")}{key}");

            // Normalize using converter
            gesture = ShortcutManager.NormalizeGestureText(gesture);

            _recordTarget.Gesture = gesture;
            GridShortcuts.Items.Refresh();

            StopRecording($"Recorded: {_recordTarget.Display} = {gesture}");
            e.Handled = true;
        }

        private void StopRecording(string status)
        {
            _isRecording = false;
            this.PreviewKeyDown -= PreferencesWindow_PreviewKeyDown;
            TextStatus.Text = status;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in Rows)
            {
                if (_defaults.TryGetValue(row.ActionKey, out var g))
                    row.Gesture = g;
            }
            GridShortcuts.Items.Refresh();
            TextStatus.Text = "Reset to defaults (not saved yet).";
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate
            var cleaned = new List<(string actionKey, string gestureText)>();
            var used = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in Rows)
            {
                var g = (row.Gesture ?? "").Trim();
                if (string.IsNullOrWhiteSpace(g)) continue;

                g = ShortcutManager.NormalizeGestureText(g);

                // Must be parsable
                try
                {
                    var conv = new KeyGestureConverter();
                    var kg = conv.ConvertFromString(g) as KeyGesture;
                    if (kg == null)
                        throw new Exception();
                    g = ShortcutManager.NormalizeGestureText(conv.ConvertToString(kg) ?? g);
                }
                catch
                {
                    MessageBox.Show(this, $"Invalid shortcut: “{g}” for {row.Display}", "Preferences", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (used.TryGetValue(g, out var other))
                {
                    MessageBox.Show(this, $"Duplicate shortcut: “{g}”\n\n- {other}\n- {row.Display}", "Preferences", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                used[g] = row.Display;
                cleaned.Add((row.ActionKey, g));
            }

            _manager.SetMapping(cleaned);
            _manager.Save();

            DialogResult = true;
            Close();
        }
    }
}
