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

            // 初期表示
            TagsList.ItemsSource = AppState.Current.Tags;
            RefreshHeader();

            // 選択動画が変わったら表示更新
            AppState.Current.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppState.SelectedVideoPath) ||
                    e.PropertyName == nameof(AppState.StatusMessage))
                {
                    Dispatcher.Invoke(RefreshHeader);
                }

                if (e.PropertyName == nameof(AppState.SelectedVideoPath) ||
                    e.PropertyName == nameof(AppState.Tags))
                {
                    Dispatcher.Invoke(RefreshCount);
                }
            };

            RefreshCount();
        }

        private void RefreshHeader()
        {
            SelectedVideoText.Text = AppState.Current.SelectedVideoPath ?? "(none)";
            RefreshCount();
        }

        private void RefreshCount()
        {
            CountText.Text = $"Count: {AppState.Current.Tags.Count}";
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            AddTagFromInput();
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            AppState.Current.ClearTagsForSelected();
            TagInput.Focus();
            RefreshCount();
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
            var text = TagInput.Text;
            AppState.Current.AddTag(text);

            TagInput.Text = "";
            TagInput.Focus();
            RefreshCount();
        }
    }
}
