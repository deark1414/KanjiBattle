# 初回導入の自動確認

更新日: 2026-10-25

## 実行

Unityの`TutorialRegression.BuildReview`で、既存の戦闘ルール・データ検証と専用WebGLビルドを行う。ビルド中は同じ出力先を使うブラウザテストを走らせない。

`node scripts/e2e/run.js --only tutorial-first-1280,tutorial-first-390,tutorial-first-844,tutorial-cancel,tutorial-settings,tutorial-features,tutorial-records,tutorial-defeat,tutorial-repeat,tutorial-gallery-390,tutorial-gallery-844 --output tmp/e2e/tutorial-results`

全件のテスト一覧は`node scripts/e2e/run.js --list`、通常の回帰確認は`node scripts/e2e/run.js`。基盤は`node --test scripts/e2e/harness.test.js`。

## 11シナリオ

| ケース | 確認すること |
|---|---|
| tutorial-first-1280/390/844 | 実際の第1局を、初期3体で配置変更・開戦・勝利し、戦果6で槍を迎える。全画面の入力遮断、文字の収まり、既読を確認 |
| tutorial-cancel | 未読の説明で画面離脱した場合は保存せず、次回に再表示。既読後は再表示しない。開いたままの縦横切替 |
| tutorial-settings | オープニング非表示・再表示の保存、手動再生、説明一覧、再読込、既読維持。見直しで進行を変えない |
| tutorial-features | 融合・プレイヤースキル・図鑑の初回説明。二回目は操作を遮らず、説明を読むだけでスキル設定を変えない |
| tutorial-records | 成績の初回説明と、閉じて再び開いた場合の一回制限 |
| tutorial-defeat | 敗北時の説明・報酬なし・再戦での非表示 |
| tutorial-repeat | 結果の説明中は3秒を過ぎても連戦を保留し、閉じたら再開する |
| tutorial-gallery-390/844 | 全9種の機能説明を設定から見直し、縦長・横長で文字が収まることと進行が変わらないことを確認 |

画面・状態・操作記録は出力ディレクトリに保存する。初回3ケースは通常の第1局と実際の戦闘処理を使い、勝敗をmockしない。成績のケースのみ、既存の成績専用プローブで境界値を用意する。その他の高度な機能は固定プリセットで解放状態を用意し、数十局分の通常進行そのものを自動でプレイするわけではない。

## 手動

`http://127.0.0.1:8796/?preset=tutorial`でデバッグ用進行を初期化して確認できる。オープニング設定を変更した後に保持して再起動したい場合は、プリセット指定なしの`http://127.0.0.1:8796/`を使う。明示的なプリセット指定は確認のため初期化する。

手動確認用ページ自体の接続・初期3体・説明・配置開始は`node scripts/e2e/check-tutorial-manual.js`でも確認する。

## 確認記録

2026-10-25: 専用11シナリオと、城下・プレイヤースキル・成績・融合の既存12シナリオを合わせた23件が成功。結果は`tmp/e2e/tutorial-final/index.html`に保存。縦長390×844、横長844×390、デスクトップ1280×800の画面も確認した。

手動確認ページのチュートリアル・城下の接続確認、テスト基盤の9件、Unity側のチュートリアル保存・戦闘ルール・データ検証も成功。外部サービスへの公開は行っていない。
