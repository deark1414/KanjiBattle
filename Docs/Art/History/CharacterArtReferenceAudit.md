# 実装画像の参照監査（2026-10-25）

この文書は差し替え原因を調べた時点の履歴。後続のユーザー指示により、以前のモチーフがある20組は元画像へ接続し直した。現行の比較表は[画像比較表](../character-art-status.html)、運用方針は[制作記録](../CharacterArtProduction.md)を参照。以下の「現」は監査当時を指す。

## 結論

- ドット絵対応を既存画像の全面再制作に広げたのは実装側の判断であり、既存の採用済み画像まで差し替える必要はなかった。
- 現在のゲームも一覧と同じ新規生成品を参照している。一覧だけの表示違いでもキャッシュでもない。
- 今回は参照監査のみ。画像の復元・再生成・差し替え、ゲームの再ビルド、外部公開は実施していない。
- 既存の元画像は残っている。今後の修正は採用済みの既存画像を基準とし、新キャラ・融合など不足分と明示的な修正対象を分ける。

## 確認方法

Unityの`CharacterArtImporter.ExportReferenceAudit`で通常キャラ27体のCharacterDatabase実参照を取得した。融合17種は実際の`CharacterData.CreateBattleFusion`、砂の分身は`CreateBattleCopy`を呼び出して参照を取得した。45組ともカタログとの参照一致を確認。これはデザインの採用承認を意味しない。今回、戦闘の全パターン再生は実施していない。

軍勢・編成は`data.icon`、戦場は味方に`data.icon`、敵に`data.enemyIcon`を使用。召喚モチーフは`PieceMotifPresentation`がカタログの`resource`をResourcesから読み込む。`source`は制作元の記録であり、実行時の読込先ではない。

## 差し替え範囲

- `54f285e`: コマの参照を従来の`Assets/Art/Generated/CharacterIcons/`から`Assets/Resources/UI/CharacterPieces/`へ変更。
- `56143ad`: それまで継承していたモチーフ17種も新規ドット絵に変更。
- 該当17種: 一、二、三、剣、槍、槌、盾、壁、石、銃、火、水、木、土、午（旧・馬）、寅（旧・虎）、竜。
- 攻撃エフェクトは別系統。剣は`Assets/Resources/VFX/Weapons/SwordMotion.png`、槌は同`HammerMotion.png`、槍は`Assets/Resources/VFX/Animated/Spear/frame_03.png`を継続使用。召喚と攻撃で武器デザインが食い違う。

## 具体例：剣

- 旧コマ: `Assets/Art/Generated/CharacterIcons/icon_10_Sword.png`
- 現コマ: `Assets/Resources/UI/CharacterPieces/Piece_10.png`
- 旧モチーフ: `Assets/Resources/UI/PieceMotifs/Motif_10.png`（当時のUV範囲で表示）
- 現モチーフ: `Assets/Resources/UI/PieceMotifs/Ready/Motif_10.png`
- 原画像を直接開いて外観も照合した。旧モチーフは青緑の刀身、現モチーフは銀色の刀身・金色の鍔へ変わっている。

## 現在の全参照

パスはプロジェクトルート基準。敵コマは味方コマの`.png`を`_Enemy.png`へ置換したパス。全行で実参照とカタログの一致、モチーフの読込を確認。

| ID | キャラ | 取得元 | 味方コマ | モチーフ |
|---|---|---|---|---|
| 1 | 一 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_1.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_1.png` |
| 2 | 二 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_2.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_2.png` |
| 3 | 三 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_3.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_3.png` |
| 10 | 剣 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_10.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_10.png` |
| 11 | 槍 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_11.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_11.png` |
| 12 | 槌 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_12.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_12.png` |
| 13 | 盾 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_13.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_13.png` |
| 14 | 甲 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_14.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_14.png` |
| 15 | 壁 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_15.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_15.png` |
| 16 | 石 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_16.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_16.png` |
| 17 | 弓 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_17.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_17.png` |
| 18 | 銃 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_18.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_18.png` |
| 19 | 火 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_19.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_19.png` |
| 20 | 水 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_20.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_20.png` |
| 21 | 木 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_21.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_21.png` |
| 22 | 土 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_22.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_22.png` |
| 23 | 午 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_23.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_23.png` |
| 24 | 酉 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_24.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_24.png` |
| 25 | 寅 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_25.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_25.png` |
| 26 | 竜 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_26.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_26.png` |
| 27 | 鈴 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_27.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_27.png` |
| 28 | 糸 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_28.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_28.png` |
| 29 | 影 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_29.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_29.png` |
| 30 | 丑 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_30.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_30.png` |
| 31 | 電 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_31.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_31.png` |
| 32 | 風 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_32.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_32.png` |
| 33 | 鉄 | CharacterDatabase | `Assets/Resources/UI/CharacterPieces/Piece_33.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_33.png` |
| -1001 | 双剣 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1001.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1001.png` |
| -1002 | 散弾 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1002.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1002.png` |
| -1003 | 戦士 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1003.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1003.png` |
| -1004 | 衛兵 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1004.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1004.png` |
| -1005 | 鍛冶 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1005.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1005.png` |
| -1006 | 波 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1006.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1006.png` |
| -1007 | 磁石 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1007.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1007.png` |
| -1008 | 山 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1008.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1008.png` |
| -1009 | 炭 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1009.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1009.png` |
| -1010 | 要塞 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1010.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1010.png` |
| -1011 | 賽子 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1011.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1011.png` |
| -1012 | 雨 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1012.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1012.png` |
| -1013 | 砂 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1013.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1013.png` |
| -1014 | 草 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1014.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1014.png` |
| -1015 | 繭 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1015.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1015.png` |
| -1016 | 風鈴 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1016.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1016.png` |
| -1017 | 灯 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F1017.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F1017.png` |
| -20000 | 砂 | 戦闘用生成処理 | `Assets/Resources/UI/CharacterPieces/Piece_F20000.png` | `Assets/Resources/UI/PieceMotifs/Ready/Motif_F20000.png` |

機械可読の実行結果: `tmp/character-art/reference-audit.json`。
