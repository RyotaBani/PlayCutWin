# PlayCutWin 開発ログ（note用）

## 2026-01-27 — Jump後の自動再生を安定化

### 背景
- クリップ一覧ダブルクリック → StartへSeek → 必ず再生開始（Player.Play）という仕様を確定済み。
- WPF MediaElement は **Position更新直後の同一tickで Play()** を呼ぶと、環境によって再生開始が不安定になることがある。

### 対応（完全置き換え）
- `MainWindow.xaml.cs` の `SeekToSeconds(seconds, autoPlay)` を修正
  - Seek（Player.Position）とUI同期（CurrentSeconds/TimelineSlider）を行った後、
  - `Dispatcher.BeginInvoke(..., DispatcherPriority.ContextIdle)` で **1ターン遅らせて Play()** を実行
  - `VM.IsPlaying = true` を確実に同期

### 期待する挙動
- クリップ一覧ダブルクリック時：
  - Start秒へSeek
  - 必ず再生開始
  - IsPlaying（VM.IsPlaying）が true

### 次の作業
- `MainWindow.xaml` を「最小構成」に固定したまま、BBVideoTagger準拠の見た目を段階的に復元
  - Team A/B 色分け
  - タグトグルの配色
  - パネル余白・角丸・ホバーなど（Actions安定後）
