using System;
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
            DataContext = PlayCutWin.AppState.Instance;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            AddTagFromInput();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTagFromInput();
                e.Handled = true;
            }
        }

        private void AddTagFromInput()
        {
            var text = TagInput.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("タグを入力してね", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ✅ 現在の再生位置を Time に入れて追加する
            var pos = PlayCutWin.AppState.Instance.PlaybackPosition; // Clips側で更新される
            var time = FormatMMSS(pos);

            PlayCutWin.AppState.Instance.Tags.Add(new PlayCutWin.TagItem
            {
                Text = text,
                Time = time
            });

            PlayCutWin.AppState.Instance.StatusMessage = $"Tag added: {time} {text}";

            TagInput.Text = "";
            TagInput.Focus();
        }

        private static string FormatMMSS(TimeSpan t)
        {
            int total = (int)Math.Max(0, t.TotalSeconds);
            int mm = total / 60;
            int ss = total % 60;
            return $"{mm:00}:{ss:00}";
        }
    }
}
