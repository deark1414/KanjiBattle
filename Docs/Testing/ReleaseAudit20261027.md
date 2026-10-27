# 2026-10-27 試遊版の公開・不具合確認

## 公開範囲

- [公開ゲーム](https://deark1414.github.io/KanjiBattle/game/index.html?v=20261027-release)。通常26種・通常融合17種・表40局。
- 通常のWebGLビルドを配信。検査用ブリッジ、強制スキル発動、デバッグ用プリセットは含めない。
- 以前の公開版の保存形式4は、現行の形式5で初期化される。形式5内の更新すべてで初期化する仕様ではない。
- GitHub Pagesはmainから公開。ゲーム以外の開発資料・Assetsへのレビュー用リンクは配布しない。使用素材のライセンス表示は同梱する。

## 今回の修正

|区分|内容|
|---|---|
|配布の不具合|HTMLの更新番号がローダーだけでなく、圧縮済みデータ・JavaScript・Wasmにも付くよう修正。古いキャッシュの混在を防ぐ|
|配布設定|Docs全体のアップロードを廃止し、ゲーム・入口・ライセンスだけをステージング。開発ソースの配信URLが404になることを確認|
|古いテスト期待値|丑の強化前効果を旧50%ではなく現行80%で検証。ゲームの効果自体は変更していない|
|短い演出の検査|糸の選択直後の状態を捨てずに確認。鍛冶は検査ビルドだけ実際の打撃フレームで停止し、ハンマー・火花・赤熱を確認。公開版の時間や演出は変更していない|

## 検査結果

詳細シナリオ147件は、再確認を含めてすべて最終成功。今回の確認範囲では、新たなゲーム進行の不具合は見つからなかった。

|検査|結果と範囲|
|---|---|
|詳細シナリオ|147/147件成功。初回案内・全ノード配置・加入/強化/振り直し・融合・スキル境界条件・実戦・再戦/連戦・図鑑・成績・保存/再読込・縦横画面・演出画像を確認|
|公開用WebGL・ローカル|1280×800、390×844、844×390の3サイズで起動・実描画・画面内への収まり・4ファイルの更新番号・実行時例外なしを確認|
|公開用WebGL・公開URL|同じ3サイズで上記を再確認。ローカルHTMLと公開HTMLも一致|
|補助ツールの自動テスト|30件成功。バランス比較条件、操作座標、例外捕捉、配布対象、検査集計等|
|Unityのルール検査|BattleRuleRegression成功。融合・成績・プレイヤースキル・城下・チュートリアル・強化を含む|
|Unityのデータ検査|GameDataValidator成功|
|仕様の整合検査|正本3冊、26キャラ、17レシピ、40局、ローカルリンクを実装と照合して成功|

詳細シナリオは、検査専用WebGLをPlaywrightのChromiumで操作する。初期進行・所持キャラ等を専用の保存状態で用意し、画面操作と実際の戦闘処理、能力の境界条件を確認する。147件すべてを公開版で最初から手動進行したという意味ではない。検査は無音で実行した。

元の失敗を消して成功だけを数えない。シナリオごとに最後の再確認を採用し、中断・古い期待値・演出撮影の失敗も試行履歴に残す。全件がそろわない場合、集計自体が失敗する。

## 再確認の入口

- 詳細シナリオ一覧: `scripts/e2e/scenarios.js`
- 詳細検査の統合結果: `tmp/e2e/release-audit-combined-20261027/index.html` と `report.json`
- 公開用WebGLの証跡: `tmp/release-smoke-local-20261027/`、`tmp/release-smoke-public-20261027/`
- Unityログ: `/tmp/kanji-release-audit-rules.log`、`/tmp/kanji-release-audit-data.log`

```sh
node scripts/e2e/run.js --build
node --test scripts/visual/release-shell.test.js scripts/balance/*.test.js scripts/e2e/harness.test.js scripts/e2e/summarize-audit.test.js
node scripts/visual/check-release-smoke.js --url 'https://deark1414.github.io/KanjiBattle/game/index.html?v=20261027-release'
node scripts/docs/check-game-spec.js
```

検査用ビルドは`--build-root tmp/任意の専用ディレクトリ`で分離できる。同時検査中のビルドを上書きしない。証跡はローカルの一時ファイルであり、Gitや公開サイトには含めない。

## 残る確認

- Safari、iOS/Android実機、タッチ操作、端末固有の発熱・メモリ、回線の遅い環境は未確認。Chromiumの画面サイズ変更は実機検証の代わりではない。
- 初回配信データは約79MB（10進表記）。スマートフォンでの待ち時間と容量削減は今後の課題。
- 全キャラ・全編成の勝率、全分岐の初見体験、音量の聴感評価は今回の不具合検査の合格範囲に含めない。不具合ゼロを保証するものではない。

## 仕様書の整理

旧GameDesign内の仕様書は削除し、[ゲームルール](../GameDesign/GameRules.md)・[コンテンツ一覧](../GameDesign/ContentCatalog.md)・[今後の構想](../GameDesign/FutureDesign.md)の3冊に書き直した。実装済みと未実装を混在させない。技術メモ・日付付き検査記録・素材の出典/ライセンスは仕様書の別版ではないため保持する。
