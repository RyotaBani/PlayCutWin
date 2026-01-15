using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        public TagsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
            Loaded += (_, __) => RefreshSelectedLabel();
        }

        // XAML: 例) TextBlock x:Name="SelectedVideoText"
        private void RefreshSelectedLabel()
        {
            try
            {
                if (SelectedVideoText != null)
                {
                    var path = GetString(AppState.Instance, "SelectedVideoPath") ?? "(no selected)";
                    SelectedVideoText.Text = path;
                }
            }
            catch { }
        }

        // XAML: Button Click="AddTag_Click"
        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var tag = (TagInput?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                MessageBox.Show("タグを入力してね（仮）", "Tags",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 1) AppState.Tags があれば追加（string list想定）
            // 2) なければ AppState.PendingTagText 等に入れておく（互換）
            var added = TryAddTagToAppState(tag);

            // 入力欄クリア
            TagInput.Text = "";

            // ステータス更新
            TrySetString(AppState.Instance, "StatusMessage",
                added ? $"Tag added: {tag}" : $"Tag entered: {tag}");

            // 表示更新
            RefreshSelectedLabel();
            RefreshTagsList();
        }

        // XAML: Button Click="ClearPending_Click"
        private void ClearPending_Click(object sender, RoutedEventArgs e)
        {
            if (TagInput != null) TagInput.Text = "";
            TrySetString(AppState.Instance, "StatusMessage", "Tag input cleared");
        }

        // XAML: Button Click="Preset_Click"  （複数ボタンから来る想定）
        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            // クリックしたボタンの Content を入力欄に入れるだけ（仮）
            if (sender is Button b)
            {
                var text = (b.Content?.ToString() ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(text) && TagInput != null)
                {
                    TagInput.Text = text;
                    TagInput.Focus();
                    TagInput.SelectAll();
                    TrySetString(AppState.Instance, "StatusMessage", $"Preset: {text}");
                }
            }
        }

        // XAML: TextBox KeyDown="TagInput_KeyDown" （Enter追加したいなら）
        private void TagInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddTag_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // ---- Tags表示（あれば） ----
        private void RefreshTagsList()
        {
            // XAMLに ListBox x:Name="TagsList" がある場合だけ反映
            if (TagsList == null) return;

            // AppState.Tags (IEnumerable<string>) があれば表示
            var tagsObj = GetProperty(AppState.Instance, "Tags");
            if (tagsObj is IEnumerable<string> tags)
            {
                TagsList.ItemsSource = tags;
            }
        }

        private bool TryAddTagToAppState(string tag)
        {
            // AppState.Tags が List<string> / ObservableCollection<string> なら Addできる
            var obj = GetProperty(AppState.Instance, "Tags");
            if (obj != null)
            {
                // Add(string) があるか反射で探す
                var add = obj.GetType().GetMethod("Add", new[] { typeof(string) });
                if (add != null)
                {
                    add.Invoke(obj, new object[] { tag });
                    return true;
                }
            }

            // 互換：PendingTagText / LastTag みたいなプロパティに入れておく
            if (TrySetString(AppState.Instance, "PendingTagText", tag)) return false;
            if (TrySetString(AppState.Instance, "LastTag", tag)) return false;

            return false;
        }

        // ---- reflection helpers ----
        private static object? GetProperty(object obj, string name)
        {
            return obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);
        }

        private static string? GetString(object obj, string name)
        {
            return GetProperty(obj, name) as string;
        }

        private static bool TrySetString(object obj, string name, string value)
        {
            try
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                {
                    p.SetValue(obj, value);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
