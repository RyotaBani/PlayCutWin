using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PlayCutWin.Models;
using PlayCutWin.Services;

namespace PlayCutWin.Views
{
    public partial class PreferencesWindow : Window
    {
        private readonly ShortcutManager _manager;
        private readonly TagCatalog _tagCatalog;

        public ObservableCollection<ShortcutRow> Items { get; } = new();
        public ObservableCollection<TagDefinition> TagItems { get; } = new();

        public PreferencesWindow(ShortcutManager manager, TagCatalog tagCatalog)
        {
            InitializeComponent();
            _manager = manager;
            _tagCatalog = tagCatalog;

            foreach (var kv in _manager.GetBindings().OrderBy(k => k.Key.ToString()))
            {
                Items.Add(new ShortcutRow(kv.Key, ShortcutRow.Label(kv.Key), kv.Value));
            }

            foreach (var td in _tagCatalog.All
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
            {
                TagItems.Add(new TagDefinition { Category = td.Category, Name = td.Name, Comment = td.Comment });
            }

            DataContext = this;
        }

        private void GestureBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb || tb.DataContext is not ShortcutRow row)
                return;

            // Prevent DataGrid navigation keys from stealing the input
            e.Handled = true;

            // Allow Backspace/Delete to clear
            if (e.Key == Key.Back || e.Key == Key.Delete)
            {
                row.Gesture = string.Empty;
                return;
            }

            var gesture = ShortcutManager.ToGestureString(e);
            if (string.IsNullOrWhiteSpace(gesture))
                return;

            row.Gesture = gesture;
        }

        private void RestoreDefaults_Click(object sender, RoutedEventArgs e)
        {
            _manager.RestoreDefaults();
            Items.Clear();
            foreach (var kv in _manager.GetBindings().OrderBy(k => k.Key.ToString()))
                Items.Add(new ShortcutRow(kv.Key, ShortcutRow.Label(kv.Key), kv.Value));
        }

        private void RestoreTagDefaults_Click(object sender, RoutedEventArgs e)
        {
            _tagCatalog.RestoreDefaults();
            TagItems.Clear();
            foreach (var td in _tagCatalog.All
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase))
            {
                TagItems.Add(new TagDefinition { Category = td.Category, Name = td.Name, Comment = td.Comment });
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Convert and validate (avoid duplicate gestures)
            var gestureMap = Items
                .Where(i => !string.IsNullOrWhiteSpace(i.Gesture))
                .GroupBy(i => i.Gesture.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();

            var dup = gestureMap.FirstOrDefault(g => g.Count() > 1);
            if (dup != null)
            {
                MessageBox.Show(this, $"Duplicate shortcut: {dup.Key}", "Preferences", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var item in Items)
            {
                _manager.SetBinding(item.Action, item.Gesture);
            }

            _manager.Save();

            // Save tag comments
            _tagCatalog.SetAll(TagItems);
            _tagCatalog.Save();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Close_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
