# 旧画像の整理記録

実施日: 2026-10-25。ユーザーの明示的な削除許可に基づく整理。変更はGitへ記録し、外部公開は行わない。画像34枚で約40.4MBを削減。

## 削除基準と確認

- 対象は下表の旧画像34枚と対応する同名`.png.meta`34件のみ。削除直前にGit追跡済み・HEADとの差分なしを確認。
- 全対象の画像・metaのGit blobが`3a000cc`と一致することを確認。
- C#のResources.Load、CharacterArtCatalog、PieceMotifPresentation、Unity GUID、現行カタログ、HTML、JSON、スクリプト参照を確認。現行の画像読込先・再生成入力に該当しない。
- `scripts/art/build-character-art.js`はPixelの生成一覧とRefinementsの現行`generated`項目のキーを入力に使う。旧版の入れ子・編集時参考画像の`refs`は読まない。初回`CharacterArtProduction.json`を読む現行再生成処理はない。
- 歴史JSONは改変しない。残る旧版・編集参考画像のパスは復元用の履歴であり、現在存在する画像を保証しない。

## 削除対象

以下の全行は **削除済み、Git 3a000ccから復元可能**。各画像に対応する`.png.meta`も削除済み。

### 初回非ドット版27枚

基準ディレクトリ: `Assets/Art/Generated/CharacterMotifs/`。
画像の完全パスとUnity GUIDの外部参照は検出されず、現行コードに同ディレクトリを組み立てて読む処理もない。初回制作の記録は`CharacterArtProduction.json`に残す。画像単位の対象は以下。

| ファイル | 根拠 |
|---|---|
| `Bell.png` | 現行読込・再生成参照なし |
| `Bow.png` | 現行読込・再生成参照なし |
| `Carapace.png` | 現行読込・再生成参照なし |
| `Coal.png` | 現行読込・再生成参照なし |
| `Cocoon.png` | 現行読込・再生成参照なし |
| `Dice.png` | 現行読込・再生成参照なし |
| `DualSwords.png` | 現行読込・再生成参照なし |
| `Electricity.png` | 現行読込・再生成参照なし |
| `Forge.png` | 現行読込・再生成参照なし |
| `Fortress.png` | 現行読込・再生成参照なし |
| `Grass.png` | 現行読込・再生成参照なし |
| `Iron.png` | 現行読込・再生成参照なし |
| `Lantern.png` | 現行読込・再生成参照なし |
| `Magnet.png` | 現行読込・再生成参照なし |
| `Mountain.png` | 現行読込・再生成参照なし |
| `Ox.png` | 現行読込・再生成参照なし |
| `Rain.png` | 現行読込・再生成参照なし |
| `Rooster.png` | 現行読込・再生成参照なし |
| `Sand.png` | 現行読込・再生成参照なし |
| `ScatterShot.png` | 現行読込・再生成参照なし |
| `Sentinel.png` | 現行読込・再生成参照なし |
| `Shadow.png` | 現行読込・再生成参照なし |
| `Thread.png` | 現行読込・再生成参照なし |
| `Warrior.png` | 現行読込・再生成参照なし |
| `Wave.png` | 現行読込・再生成参照なし |
| `Wind.png` | 現行読込・再生成参照なし |
| `WindChime.png` | 現行読込・再生成参照なし |

### Reviewed旧案7枚

基準ディレクトリ: `Assets/Art/Generated/CharacterMotifsReviewed/`。
いずれも現行カタログ・比較表・Unity GUID・再生成の現行採用元には参照なし。`CharacterArtRefinements.json`に旧版や生成時の参考画像としてだけ残る。

| ファイル | 現行の採用元 |
|---|---|
| `DiceReviewed.png` | `DiceDiagonal.png` |
| `DiceGeometryV2.png` | `DiceDiagonal.png` |
| `OxPlain.png` | `HolsteinPixel.png` |
| `MountainNatural.png` | `MountainTwinSummit.png` |
| `MountainOrganicPixel.png` | `MountainTwinSummit.png` |
| `FortressAlignedCannons.png` | `FortressFrontCannons.png` |
| `FortressThickCorner.png` | `FortressFrontCannons.png` |

## 復元方法

プロジェクトルートで必要な対象だけ実行する。未コミット変更を上書きしないよう、同じパスに新しいファイルがないことを先に確認する。

```sh
git restore --source=3a000cc --worktree -- Assets/Art/Generated/CharacterMotifsReviewed/DiceReviewed.png Assets/Art/Generated/CharacterMotifsReviewed/DiceReviewed.png.meta
```

他の対象も上表のパスに置き換える。画像だけでなくmetaも同じコミットから復元するとUnity GUIDを維持できる。

## 文書の整理

- `Docs/Art/CharacterArtReferenceAudit.md`を`Docs/Art/History/CharacterArtReferenceAudit.md`へ移動。当時の調査結果は保持し、現行資料への相対リンクを更新。
- `Docs/GameDesign/PresentationArtPlan.md`を`Docs/Art/History/PresentationArtPlan.md`へ移動。当時の寸法・キャラ構成・表示計画が現行ではない旨と入口リンクを追加。
- 現行入口は[README](README.md)。旧画像保管についての[制作記録](CharacterArtProduction.md)の記載を本整理に合わせて更新。
- 現行の元画像、Pixel、SupportSkillsは変更しない。既存dirty・未追跡ファイルは保全。旧仕様書6件はREADMEに保留として記載。

## 整理後の確認

- 作業開始時のdirty・未追跡161ファイルの内容ハッシュ一致を確認。
- 現行Pixel入力44枚、Reviewed入力16枚、カタログ45件の全リソースの存在を確認。
- 今回追加・移動・更新したMarkdown5件の相対リンクを確認。
- 整理そのものでは実行コード・現行リソース・再生成処理は変更していない。その後のスキル接続作業でUnityの取り込み・回帰・データ検査とWebGLビルド、Playwright24件が成功。[接続記録](SupportSkillVfxIntegration.md)を参照。
