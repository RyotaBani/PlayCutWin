using System.ComponentModel;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class TagsView : UserControl
    {
        public TagsView()
        {
            InitializeComponent();
            Update();

            PlayCutWin.AppState.Instance.PropertyChanged += AppState_PropertyChanged;
        }

        private void AppState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideoPath) ||
                e.PropertyName == nameof(PlayCutWin.AppState.SelectedVideoFileName))
            {
                Dispatcher.Invoke(Update);
            }
        }

        private void Update()
        {
            var path = PlayCutWin.AppState.Instance.SelectedVideoPath;
            SelectedVideoText.Text = string.IsNullOrWhiteSpace(path) ? "(none)" : path;
        }
    }
}
