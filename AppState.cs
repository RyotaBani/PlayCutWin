using System.Collections.ObjectModel;

namespace PlayCutWin
{
    public sealed class AppState
    {
        // どこからでも参照できる簡易シングルトン
        public static AppState Instance { get; } = new AppState();

        // Import済み動画の一覧（フルパス）
        public ObservableCollection<string> ImportedVideos { get; } = new ObservableCollection<string>();

        private AppState() { }
    }
}
