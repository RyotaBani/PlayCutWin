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
            DataContext = AppState.Instance;

            // DataGridでDeleteキー削除を許可
            TagsGrid.PreviewKeyDown += TagsGrid_PreviewKeyDown;
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddFromInput();
                e.Handled = true;
            }
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            AddFromInput();
        }

        private void ClearTags_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AppState.Instance.ClearTagsForSelected();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tags", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TagsGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;

            try
            {
                if (TagsGrid.SelectedItem is AppState.TagEntry te)
                {
                    AppState.Instance.RemoveSelectedTag(te);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tags", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddFromInput()
        {
            try
            {
                var text = (TagInput.Text ?? "").Trim();
                if (text.Length == 0) return;

                AppState.Instance.AddTagToSelected(text);

                TagInput.Text = "";
                TagInput.Focus();

                // 末尾にスクロール
                if (AppState.Instance.Tags.Count > 0)
                {
                    var last = AppState.Instance.Tags[^1];
                    TagsGrid.SelectedItem = last;
                    TagsGrid.ScrollIntoView(last);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Tags", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
