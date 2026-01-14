using System;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace PlayCutWin.Views
{
    public partial class ClipsView : UserControl
    {
        public ClipsView()
        {
            InitializeComponent();

            // 初期表示
            Refresh();

            // 追加/削除を反映
            PlayCutWin.AppState.Instance.ImportedVideos.CollectionChanged += (_, __) =>
            {
                // UI更新
                Dispatcher.Invoke(Refresh);
            };
        }

        private void Refresh()
        {
            // ListBoxにはファイル名を表示（裏ではフルパス）
            var items = PlayCutWin.AppState.Instance.ImportedVideos
                .Select(p => $"{Path.GetFileName(p)}   |   {p}")
                .ToList();

            VideoList.ItemsSource = items;
            CountText.Text = $"Count: {items.Count}";
        }
    }
}
