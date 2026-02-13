# PlayCutWin (Clean Rebuild)

WPF / .NET 7 / MVVM の最小構成で **PlayCutWin をゼロから再構築**したベースです。

## 目的
- Mac版 BBVideoTagger と **UI・操作感を揃える**ための安定基盤
- 断片修正で崩壊しないように、**フォルダ/責務/依存関係を明確化**

## 要件対応
- 動画読み込み ✅ (LibVLCSharp)
- 再生 / 速度変更 ✅
- Clip START / END ✅
- Team A / B 保存 ✅
- ダブルクリックでジャンプ＆再生 ✅
- CSV Import / Export ✅
- タグUI（オフェンス/ディフェンス）✅

## 必要環境
- Visual Studio 2022
- .NET SDK 7.x (net7.0-windows)
- NuGet restore (初回ビルド時に自動で入ります)

## 使い方
1. `PlayCutWin.sln` を開く
2. NuGet Restore
3. 実行 (F5)
4. 「動画読み込み」→ MP4等を指定
5. START / END → クリップ追加
6. Clips の項目をダブルクリック → ジャンプして再生
7. CSV Import/Export で保存・復元

## 次にやること（ここから Mac版寄せ）
- Mac版のキーバインド（左右キー=コマ送り / 単独ショートカット）
- クリップのトリム（Start/Endの再調整）
- Tagごとコメント
- UIの色/余白/フォントを Mac版に寄せる
