using System.Windows;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        private AppState State => AppState.Instance;

        public ClipsView()
        {
            InitializeComponent();
            DataContext = State;
        }

        private void Clip_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox lb && lb.SelectedItem is VideoItem v)
            {
                State.SelectedVideo = v;

                // 選択時は再生位置を先頭へ
                State.PlaybackSeconds = 0;

                // Range も初期化（事故防止）
                State.ClipStart = 0;
                State.ClipEnd = 0;
            }
        }
    }
}
