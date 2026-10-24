# キャラクターのコマ・モチーフ制作

対象: 通常26系列、通常融合17種、敵専用の竜、砂人形。未実装の進化・超融合・究極融合は対象外。

## 表現方針

- コマは既存の五角形と筆文字を継承。味方は木地と墨、敵は朱塗りと白文字で識別する。融合は二重の縁を加える。
- 漢字の形と意味が主役。一文字は大きく、二文字は縦組み。HP・向き・レベル表示の場所を確保する。
- モチーフは和風の透過ドット調。通常はコマ、召喚時は大きく出現して消え、軍勢・編成では選択時のみ表示する。
- 既存の確認済みモチーフは継承。甲・弓・酉は現行名簿に合わせて差し替え、新7系列と融合17種を追加する。
- 新規生成は内蔵image_genを使用。原画を保存し、透過の内容領域を固定512px枠へ収めて余白・中心を揃える。原画は上書きしない。コマは512pxで描画して256pxへ縮小する。
- コマの文字は画像生成に任せず、既存と同じYuji Boku書体で正確に描画する。ライセンスを同梱する。

## 保存先

- 生成プロンプトと原画の記録: `CharacterArtProduction.json`
- 原画: `Assets/Art/Generated/CharacterMotifs/`
- 実行用のモチーフ: `Assets/Resources/UI/PieceMotifs/`
- 実行用のコマ: `Assets/Resources/UI/CharacterPieces/`
- 一覧確認: `Docs/Art/character-art-gallery.html`、ローカルURL `http://127.0.0.1:8797/`
- 検証記録: `tmp/character-art/`、ゲーム内シナリオ: `tmp/e2e/character-art-final/`

未発見融合の完成形は開戦前に見せない。モチーフ追加によって発見条件や加入条件、保存形式を変えない。

## 再生成・確認

1. `node scripts/art/build-character-art.js`で原画からコマ・モチーフを再生成する。原画27点はプロジェクト内へ保存しているため、通常の再生成に画像APIは不要。
2. Unityの`KanjiBattle.Editor.CharacterArtImporter.ImportProduction`で取込・参照を確認する。
3. `node scripts/art/check-character-art.js`で135画像の透過・余白・寸法と、一覧の全読込・分類・拡大・スマホ表示を確認する。
4. `node scripts/art/serve-gallery.js`で一覧を開く。

ゲーム内確認はE2Eの`art-standard`、`art-dice`、`art-sand-mobile`、`art-selection`。一覧と編成枠の同じキャラは同時に召喚されることがあるため、表示数を1に限定せず、選択したキャラ以外のモチーフが出ていないことを確認する。召喚後は元の漢字コマへ戻る。

繭の保護表示は漢字へ文字を重ねない金色の枠へ変更。敵用コマは朱塗り・白文字で、通常コマと融合コマの両方を用意した。

## 確認結果（2026-10-24）

| 対象 | 結果 |
|---|---|
| 画像 | 45組・135画像の寸法、実透過、非空、端の余白に成功 |
| 一覧 | 全読込、通常26／融合17／その他2の分類、拡大と閉じる、PCとスマホで成功 |
| Unity取込 | `CharacterArtImporter.ImportProduction`成功。全45組と基本名簿の参照を確認 |
| Unity回帰 | `BattleRuleRegression.RunFromCommandLine`と`GameDataValidator.ValidateFromCommandLine`がPassed、終了コード0 |
| ゲーム内 | 追加4件と既存8件の計12/12成功。召喚・元に戻る・賽子・砂人形・選択限定・支援系能力・縦長の融合操作を確認 |
| 通常版 | PC・縦長・横長で非空の描画、画面内の収まり、実行時例外なしを確認 |
| テスト基盤 | 9/9成功 |

テスト登録総数は60件だが、今回は画像表示に関係する12件を選んで実行した。前工程の56件全通過とは分けて記録する。通常版も`tmp/deployment-preview/game`へ更新し、デバッグ版は`tmp/e2e/game`へ分離した。外部公開はしていない。

画像制作は内蔵image_genで実施。全プロンプトは同名JSONの`generated[].prompt`に保存。コマは同梱のYuji Boku書体を使う再現可能な描画で、実行中のゲームに画像生成APIは不要。
