using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        private readonly AppState _state = AppState.Current;

        public TagsView()
        {
            InitializeComponent();
            DataContext = _state;

            TagsList.ItemsSource = _state.Tags;
            RefreshSelectedText();

            _state.PropertyChanged += State_PropertyChanged;
        }

        private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppState.SelectedVideoPath))
            {
                Dispatcher.Invoke(RefreshSelectedText);
            }
        }

        private void RefreshSelectedText()
        {
            SelectedVideoText.Text = string.IsNullOrWhiteSpace(_state.SelectedVideoPath)
                ? "(none)"
                : _state.SelectedVideoPath;
        }

        private void AddTag_Click(object sender, RoutedEventArgs e)
        {
            var text = TagInput.Text;
            _state.AddTag(text);
            TagInput.Text = "";
            TagInput.Focus();
        }

        private void TagInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddTag_Click(sender, e);
                e.Handled = true;
            }
        }

        private void Preset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string tag)
            {
                _state.AddTag(tag);
                TagInput.Text = "";
                TagInput.Focus();
            }
        }

        private void ClearTagsForSelected_Click(object sender, RoutedEventArgs e)
        {
            _state.ClearTagsForSelected();
        }
    }
}
