// Views/TagsView.xaml.cs （完全置き換え）
using System;
using System.Collections;
using System.Globalization;
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

            // 他Viewと同様に AppState をDataContextにする前提
            DataContext = AppState.Instance;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            TryAddTagFromInput();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                TryAddTagFromInput();
                e.Handled = true;
            }
        }

        private void TryAddTagFromInput()
        {
            var text = (TagInput?.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("タグを入力してね（仮）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var app = AppState.Instance;

            // まず SelectedVideoPath があるか確認（なければ追加しない）
            var selectedPath = GetStringProperty(app, "SelectedVideoPath");
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                MessageBox.Show("先に Clips で動画を選択してね（仮）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 再生位置の表示文字列を作る（PlaybackPositionText があればそれ優先）
            var timeText = GetStringProperty(app, "PlaybackPositionText");
            if (string.IsNullOrWhiteSpace(timeText))
            {
                var pos = GetDoubleProperty(app, "PlaybackPosition", fallback: 0.0);
                timeText = FormatTime(pos);
            }

            // AppState.Tags に追加（型が違っても reflection で合わせる）
            var ok = TryAppendToTags(app, timeText, text);

            if (!ok)
            {
                // Tags が見つからない/追加できない場合は、いったんメッセージだけ出す
                MessageBox.Show($"(placeholder) Tag added: [{timeText}] {text}\n※AppState.Tags に追加できませんでした", "Tags",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // 入力クリア
                TagInput.Text = "";

                // ステータス更新（あれば）
                TrySetStringProperty(app, "StatusMessage", $"Tag added: [{timeText}] {text}");
            }
        }

        // -------- helpers --------

        private static bool TryAppendToTags(object appState, string time, string text)
        {
            // 1) AppState に AddTag(string time, string text) があれば呼ぶ
            var addTag = appState.GetType().GetMethod("AddTag", BindingFlags.Public | BindingFlags.Instance);
            if (addTag != null)
            {
                var ps = addTag.GetParameters();
                try
                {
                    if (ps.Length == 2 && ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(string))
                    {
                        addTag.Invoke(appState, new object[] { time, text });
                        return true;
                    }
                    if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
                    {
                        addTag.Invoke(appState, new object[] { text });
                        return true;
                    }
                }
                catch { /* fallthrough */ }
            }

            // 2) AppState.Tags (IList/ObservableCollection) に直接追加
            var tagsObj = GetPropertyValue(appState, "Tags");
            if (tagsObj is not IList list) return false;

            // list の要素型を推定
            var itemType = GetListItemType(tagsObj.GetType());
            if (itemType == null || itemType == typeof(object))
            {
                // object なら Expando 的なものは不要。単純に文字列で入れる（Time/Text列は表示されない可能性あり）
                try
                {
                    list.Add(new SimpleTagRow { Time = time, Text = text });
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            // itemType に Time/Text プロパティがあれば、それをセットして追加
            try
            {
                var item = Activator.CreateInstance(itemType);
                if (item == null) return false;

                SetPropertyIfExists(item, "Time", time);
                SetPropertyIfExists(item, "Text", text);

                list.Add(item);
                return true;
            }
            catch
            {
                // itemType が new() できない等
                try
                {
                    // 最後の手段: 自前クラスで追加（List<T> だと型が合わず失敗するが、IList<object> なら通る）
                    list.Add(new SimpleTagRow { Time = time, Text = text });
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static Type? GetListItemType(Type listType)
        {
            // ObservableCollection<T> / List<T> など
            if (listType.IsGenericType)
            {
                var args = listType.GetGenericArguments();
                if (args.Length == 1) return args[0];
            }

            // IList<T> を探す
            foreach (var it in listType.GetInterfaces())
            {
                if (it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    var args = it.GetGenericArguments();
                    if (args.Length == 1) return args[0];
                }
            }

            return typeof(object);
        }

        private static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            var ts = TimeSpan.FromSeconds(seconds);
            if (ts.TotalHours >= 1)
                return ts.ToString(@"h\:mm\:ss", CultureInfo.InvariantCulture);
            return ts.ToString(@"m\:ss", CultureInfo.InvariantCulture);
        }

        private static object? GetPropertyValue(object obj, string name)
        {
            var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            return p?.GetValue(obj);
        }

        private static string? GetStringProperty(object obj, string name)
        {
            var v = GetPropertyValue(obj, name);
            return v as string;
        }

        private static double GetDoubleProperty(object obj, string name, double fallback)
        {
            var v = GetPropertyValue(obj, name);
            if (v == null) return fallback;
            try
            {
                return Convert.ToDouble(v, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static void TrySetStringProperty(object obj, string name, string value)
        {
            try
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanWrite && p.PropertyType == typeof(string))
                {
                    p.SetValue(obj, value);
                }
            }
            catch { }
        }

        private static void SetPropertyIfExists(object obj, string name, object value)
        {
            var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanWrite) return;

            if (value != null && p.PropertyType.IsAssignableFrom(value.GetType()))
            {
                p.SetValue(obj, value);
                return;
            }

            // 文字列→型変換など
            try
            {
                var converted = Convert.ChangeType(value, p.PropertyType, CultureInfo.InvariantCulture);
                p.SetValue(obj, converted);
            }
            catch
            {
                // ignore
            }
        }

        // AppState.Tags が object/list で受けられる場合の保険
        private class SimpleTagRow
        {
            public string Time { get; set; } = "";
            public string Text { get; set; } = "";
        }
    }
}
