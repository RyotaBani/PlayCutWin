using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        public TagsView()
        {
            InitializeComponent();
            DataContext = AppState.Instance;
            Loaded += (_, __) => RefreshAll();
        }

        // ---- XAMLにx:Nameが無くてもOKにする ----
        private T? Find<T>(string name) where T : class
        {
            return this.FindName(name) as T;
        }

        private void RefreshAll()
        {
            TryUpdateSelectedVideoText();
            TryBindTagsList();
        }

        private void TryUpdateSelectedVideoText()
        {
            var tb = Find<TextBlock>("SelectedVideoText");
            if (tb == null) return;

            var path = GetString(AppState.Instance, "SelectedVideoPath")
                       ?? GetString(AppState.Instance, "SelectedPath")
                       ?? "(no selected)";
            tb.Text = path;
        }

        private void TryBindTagsList()
        {
            // ListBox x:Name="TagsList" がある場合だけ反映
            var list = Find<ListBox>("TagsList");
            if (list == null) return;

            var tagsObj = GetProperty(AppState.Instance, "Tags");
            if (tagsObj is IEnumerable<string> tags)
            {
                list.ItemsSource = tags;
                return;
            }

            // 互換: IList<string> / IEnumerable でも拾う
            if (tagsObj is IEnumerable enumerable)
            {
                var items = new List<string>();
                foreach (var x in enumerable) items.Add(x?.ToString() ?? "");
                list.ItemsSource = items;
            }
        }

        // XAML: Button Click="AddTag_Click"
        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var input = Find<TextBox>("TagInput");
            var tag = (input?.Text ?? "").Trim();

            if (string.IsNullOrWhiteSpace(tag))
            {
                MessageBox.Show("タグを入力してね（仮）", "Tags",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            TryAddTag(tag);

            if (input != null) input.Text = "";

            TrySetString(AppState.Instance, "StatusMessage", $"Tag added: {tag}");
            RefreshAll();
        }

        // XAML: Button Click="ClearPending_Click"
        private void ClearPending_Click(object sender, RoutedEventArgs e)
        {
            var input = Find<TextBox>("TagInput");
            if (input != null) input.Text = "";
            TrySetString(AppState.Instance, "StatusMessage", "Tag input cleared");
        }

        // XAML: Button Click="Preset_Click"
        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            var input = Find<TextBox>("TagInput");
            if (input == null) return;

            if (sender is Button b)
            {
                var text = (b.Content?.ToString() ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    input.Text = text;
                    input.Focus();
                    input.SelectAll();
                    TrySetString(AppState.Instance, "StatusMessage", $"Preset: {text}");
                }
            }
        }

        // XAML: TextBox KeyDown="TagInput_KeyDown"（あれば）
        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // ---- AppStateへタグ追加（Tagsプロパティが無くても落ちない） ----
        private void TryAddTag(string tag)
        {
            var obj = GetProperty(AppState.Instance, "Tags");
            if (obj != null)
            {
                var add = obj.GetType().GetMethod("Add", new[] { typeof(string) });
                if (add != null)
                {
                    add.Invoke(obj, new object[] { tag });
                    return;
                }
            }

            // 互換: 何も無ければ PendingTagText / LastTag に入れる
            if (!TrySetString(AppState.Instance, "PendingTagText", tag))
                TrySetString(AppState.Instance, "LastTag", tag);
        }

        // ---- reflection helpers ----
        private static object? GetProperty(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(obj);

        private static string? GetString(object obj, string name)
            => GetProperty(obj, name) as string;

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
