using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        private readonly AppState _state = AppState.Instance;
        private readonly DispatcherTimer _uiTimer;

        public TagsView()
        {
            InitializeComponent();

            // 一旦はコードビハインドで繋ぐ（小さく進めるため）
            TagList.ItemsSource = _state.Tags;

            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _uiTimer.Tick += (_, __) => RefreshHeader();
            _uiTimer.Start();

            RefreshHeader();
        }

        private void RefreshHeader()
        {
            SelectedVideoText.Text = string.IsNullOrWhiteSpace(_state.SelectedVideoPath)
                ? "(none)"
                : _state.SelectedVideoPath;

            TimeText.Text = _state.PlaybackPositionText;

            // 選択動画がないと Add を無効化
            AddTagButton.IsEnabled = !string.IsNullOrWhiteSpace(_state.SelectedVideoPath);
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_state.SelectedVideoPath))
            {
                MessageBox.Show("先に Clips で動画を選択してね。", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var text = TagInput.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("タグを入力してね（仮）", "Tags", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _state.AddTag(text);
            TagInput.Text = "";

            // 今は「積めた！」が目的なので、スクロールして見える化
            TagList.ScrollIntoView(_state.Tags.LastOrDefault());
        }
    }
}
