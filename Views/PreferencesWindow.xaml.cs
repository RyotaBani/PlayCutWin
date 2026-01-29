using PlayCutWin.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace PlayCutWin.Views
{
    public partial class PreferencesWindow : Window
    {
        private ObservableCollection<ShortcutManager.BindingItem> _items = new();

        public PreferencesWindow()
        {
            InitializeComponent();

            LoadFromManager();
        }

        private void LoadFromManager()
        {
            _items = new ObservableCollection<ShortcutManager.BindingItem>(
                ShortcutManager.Instance.Items.Select(x => new ShortcutManager.BindingItem
                {
                    ActionId = x.ActionId,
                    Gesture = x.Gesture,
                    Title = x.Title,
                    Category = x.Category
                })
            );

            GridShortcuts.ItemsSource = _items;
            TxtStatus.Text = $"Config: {ShortcutManager.Instance.ConfigPath}";
        }

        private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            // Delete config file and reload
            try
            {
                var path = ShortcutManager.Instance.ConfigPath;
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
            catch { }

            ShortcutManager.Instance.Load();
            LoadFromManager();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Basic validation: duplicates & parseability
            var mgr = ShortcutManager.Instance;

            var parsed = new List<(string actionId, string gesture, string? err)>();
            foreach (var it in _items)
            {
                var g = (it.Gesture ?? "").Trim();
                if (string.IsNullOrEmpty(g))
                {
                    parsed.Add((it.ActionId, g, null));
                    continue;
                }

                var kg = mgr.TryParseGesture(g);
                if (kg == null)
                {
                    MessageBox.Show($"Invalid shortcut: \"{g}\" for {it.Title}", "Preferences",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                parsed.Add((it.ActionId, g, null));
            }

            var dup = parsed
                .Where(x => !string.IsNullOrWhiteSpace(x.gesture))
                .GroupBy(x => x.gesture)
                .FirstOrDefault(g => g.Count() > 1);

            if (dup != null)
            {
                var names = _items.Where(x => (x.Gesture ?? "").Trim() == dup.Key).Select(x => x.Title).ToList();
                MessageBox.Show($"Duplicate shortcut \"{dup.Key}\" used by:\n- {string.Join("\n- ", names)}",
                    "Preferences", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            mgr.Save(_items);
            DialogResult = true;
            Close();
        }
    }
}
