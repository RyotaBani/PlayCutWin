# PlayCutWin 開発ログ（note用）

## 2026-01-27 — ジャンプ後の自動再生を「確実に」する + UIリソース統一

### できたこと

#### 1) ジャンプ後の自動再生を完成形へ
- クリップ一覧のダブルクリックで `Start秒へSeek → 必ず再生開始 → IsPlaying=true 同期` を保証
- WPF `MediaElement` のクセ（Seek直後に `Play()` が効かないことがある）対策として、
  - `Seek` を先に実行
  - **Dispatcherで1tick遅らせて `Play()`**（`DispatcherPriority.ContextIdle`）
  - UI（Slider/Time/IsPlaying）を即同期

#### 2) Style/Resource を App.xaml に一本化
- `MainWindow.xaml` の `Window.Resources` を撤去
- 参照キー（`Panel` / `DarkButton` / `DarkListView` / `OffenseTagToggle` / `DefenseTagToggle` など）は **すべて App.xaml に集約**
- 以前クラッシュ原因になった `ControlTemplate.Triggers` 内の `TemplateBinding` を回避し、
  - `RelativeSource TemplatedParent` の Binding で安全に置換

### 目的（なぜこうしたか）
- GitHub Actions が落ちない「基準点」を作る
- UI（Mac版BBVideoTagger準拠）を戻していく前に、WPFで壊れやすい要因（Resource散在・Trigger内TemplateBinding）を先に排除

### 次にやること
1. Actionsが緑のコミットを基準化（ここが“安全地帯”）
2. 角丸/ホバー/タグ色分けを段階的に復活（1コミット=1変更）
3. エクスポート周りは ffmpeg の検出/エラー文言を整えて現場運用へ
