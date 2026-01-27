# PlayCutWin 開発ログ（構成固定フェーズ）

## 2026-01-27 起動クラッシュ（XamlParseException）再発 → App.xaml を安定版へ

### 発生
- 起動直後にクラッシュ
- 例外: System.Windows.Markup.XamlParseException
- 内容: Setter.Value の Set で ArgumentException（Style 値が不正）

### 原因
- App.xaml 内の ControlTemplate / Setter.Value が環境差で解決に失敗し、Window.Show() 時点で例外化。
- 特にテンプレートの Setter.Value は、型不一致や参照リソースのズレがあると即クラッシュになる。

### 対応
- **App.xaml を “安定最優先” に全面置き換え**
  - ControlTemplate を一旦撤去（WPF既定テンプレートを使用）
  - 画面が参照する StaticResource キーを App.xaml に必ず定義
    - Bg0 / PanelBg / PanelBorderBrush / Body / H1 / H2 / Small / SubLabel
    - Panel / PanelBorder / PanelHeader / InnerBorder
    - DarkButton / TopButton / AccentButton
    - TagToggleBase / OffenseTagToggle / DefenseTagToggle
    - DarkTextBox / TeamTextBox / WatermarkTextBox
    - DarkListView

### 狙い
- まず **起動で落ちない**・**GitHub Actions が安定** を最優先で固定。
- 見た目（角丸・ホバー等のカスタムテンプレート復活）は、Actions安定後に段階的に実施する。

---

## 2026-01-27 画面サイズ調整（起動時Maximize）＋ クリップ行の Team 色アクセント

### 目的
- Mac版BBVideoTaggerに近い “見やすい” 作業領域に戻す
- 既に安定した構成（App.xaml一本化 / ControlTemplate撤去）を崩さずにUIを改善

### 対応
- MainWindow.xaml
  - 起動時に WindowState=Maximized
  - Width/Height を広めに設定し、MinWidth/MinHeight で小さすぎ防止
- App.xaml
  - Clip行（ListViewItem）に対して **テンプレート無し**の ItemContainerStyle を追加
    - Team A：青アクセント（TeamAAccent）
    - Team B：赤アクセント（TeamBAccent）
    - Hover / Selected の背景色を統一

### 結果
- 画面が狭くて見づらい問題を解消
- クリップ一覧が Team 別に視認しやすくなり、Mac版の雰囲気に近づいた

### 次にやること
- Actionsを“緑”の基準点に固定
- 角丸やホバーなどのカスタムテンプレート復活は、**1つずつ**導入して安全に戻す

