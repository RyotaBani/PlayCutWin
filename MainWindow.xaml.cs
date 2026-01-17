using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PlayCutWin
{
    public partial class MainWindow : Window
    {
        private AppState S => AppState.Instance;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = S;

            // 起動直後にWindowへフォーカス（キーを拾うため）
            Loaded += (_, __) => Focus();
        }

        // 重要：TextBox入力中はショートカットを奪わない（タグ入力など）
        private bool IsTyping()
        {
            return Keyboard.FocusedElement is TextBox
                   || Keyboard.FocusedElement is PasswordBox
                   || Keyboard.FocusedElement is RichTextBox;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // タグ入力中は基本スルー（←→/J/K/Lが文字編集を壊すため）
            if (IsTyping())
                return;

            // クリップ未選択なら再生系は無効（←→のシークだけは許可してもいいが、今回は安全優先）
            bool hasClip = !string.IsNullOrWhiteSpace(S.SelectedVideoPath);

            // ====== 基本：← / → で ±0.5s ======
            if (e.Key == Key.Left)
            {
                S.PlaybackSeconds = System.Math.Max(0, S.PlaybackSeconds - 0.5);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Right)
            {
                S.PlaybackSeconds = S.PlaybackSeconds + 0.5;
                e.Handled = true;
                return;
            }

            // ====== Space：Play/Pause トグル ======
            if (e.Key == Key.Space)
            {
                if (!hasClip) return;
                S.IsPlaying = !S.IsPlaying;
                S.StatusMessage = S.IsPlaying ? "Play" : "Pause";
                e.Handled = true;
                return;
            }

            // ====== J / K / L ======
            // J: 巻き戻し（押すたびに -2.0s）
            if (e.Key == Key.J)
            {
                if (!hasClip) return;
                S.PlaybackSeconds = System.Math.Max(0, S.PlaybackSeconds - 2.0);
                e.Handled = true;
                return;
            }

            // K: 停止（Pause相当）
            if (e.Key == Key.K)
            {
                if (!hasClip) return;
                S.IsPlaying = false;
                S.StatusMessage = "Pause";
                e.Handled = true;
                return;
            }

            // L: 早送り（押すたびに +2.0s）
            if (e.Key == Key.L)
            {
                if (!hasClip) return;
                S.PlaybackSeconds = S.PlaybackSeconds + 2.0;
                e.Handled = true;
                return;
            }

            // ====== S/E：START/END を打てると便利 ======
            if (e.Key == Key.S)
            {
                if (!hasClip) return;
                S.ClipStart = S.PlaybackSeconds;
                S.StatusMessage = "Start set";
                e.Handled = true;
                return;
            }

            if (e.Key == Key.E)
            {
                if (!hasClip) return;
                S.ClipEnd = S.PlaybackSeconds;
                S.StatusMessage = "End set";
                e.Handled = true;
                return;
            }

            // ====== R：Range reset ======
            if (e.Key == Key.R)
            {
                if (!hasClip) return;
                S.ResetRange();
                e.Handled = true;
                return;
            }
        }
    }
}
