using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ExportsView : UserControl
    {
        public ExportsView()
        {
            InitializeComponent();

            // Preview は「選択中動画のタグ(AppState.Tags)」をそのまま表示
            TagsGrid.ItemsSource = AppState.Current.Tags;

            RefreshHeader();

            AppState.Current.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppState.SelectedVideoPath) ||
                    e.PropertyName == nameof(AppState.Tags))
                {
                    Dispatcher.Invoke(RefreshHeader);
                }
            };
        }

        private void RefreshHeader()
        {
            SelectedVideoText.Text = AppState.Current.SelectedVideoPath ?? "(none)";
            CountText.Text = $"Tags: {AppState.Current.Tags.Count}";
        }

        private void ExportSelectedCsv_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AppState.Current.SelectedVideoPath))
            {
                MessageBox.Show("No video selected.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var tags = AppState.Current.Tags.ToList();
            if (tags.Count == 0)
            {
                MessageBox.Show("No tags to export for selected video.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var defaultName = SafeFileName(Path.GetFileNameWithoutExtension(AppState.Current.SelectedVideoPath)) + "_tags.csv";
            var path = AskSaveCsvPath(defaultName);
            if (path == null) return;

            WriteTagsCsv(path, tags);

            AppState.Current.StatusMessage = $"Exported CSV: {Path.GetFileName(path)}";
            MessageBox.Show($"Exported:\n{path}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExportAllCsv_Click(object sender, RoutedEventArgs e)
        {
            // AppState に「全タグ取得」が無い場合でも、最低限「今選択中だけ」は出せる。
            // でも “All” は意味が薄いので、ここでは AppState の内部ストアがある前提で呼ぶ。
            // もしメソッドが無ければ、下の追記（③）を AppState に入れてね。
            var all = AppState.Current.GetAllTagsSnapshot();
            if (all.Count == 0)
            {
                MessageBox.Show("No tags to export.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var path = AskSaveCsvPath("all_tags.csv");
            if (path == null) return;

            WriteTagsCsv(path, all);

            AppState.Current.StatusMessage = $"Exported CSV: {Path.GetFileName(path)}";
            MessageBox.Show($"Exported:\n{path}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static string? AskSaveCsvPath(string defaultFileName)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Save CSV",
                Filter = "CSV (*.csv)|*.csv",
                FileName = defaultFileName,
                AddExtension = true,
                DefaultExt = ".csv"
            };

            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        private static void WriteTagsCsv(string path, List<TagEntry> tags)
        {
            var sb = new StringBuilder();

            // header
            sb.AppendLine("VideoPath,TimeText,Seconds,Tag,CreatedAt");

            foreach (var t in tags)
            {
                sb.Append(EscapeCsv(t.VideoPath)).Append(",");
                sb.Append(EscapeCsv(t.TimeText)).Append(",");
                sb.Append(EscapeCsv(t.Seconds.ToString("0.###"))).Append(",");
                sb.Append(EscapeCsv(t.Tag)).Append(",");
                sb.Append(EscapeCsv(t.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                sb.AppendLine();
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        private static string EscapeCsv(string s)
        {
            s ??= "";
            var needQuote = s.Contains(",") || s.Contains("\"") || s.Contains("\n") || s.Contains("\r");
            if (!needQuote) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private static string SafeFileName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }
    }
}
