# Release Milestones

Last Updated: 2026-08-13

現時点では、直近のバランス調整後のシミュレーション結果をもって「全章クリア可能想定」として進めます。以降は調整の深追いを避け、リリースに必要な安定化へ寄せます。

## Current Balance Snapshot

2026-08-13 に重点ステージ `1,3,5,7,8,19,20,23,25,34,35,40` を軽量シミュレーションし、全て `OK` でした。

- Stage 7 `槍の間`: 残HPが低め。手動確認では優先して見る。
- Stage 25 `遠距離の門番`: 残HPが低め。遠距離章の詰まり候補として見る。
- Stage 40 `竜の座`: 現在の竜ステータスでは勝利想定。

## Milestone 1: 仕様固定

Status: Done

- ステージ、キャラクター、施設の現行データをドキュメント化する。
- 章解放、研究所解放、編成枠、レベル上限の進行を固定する。
- 中位数字、上位数字、竜の現在仕様を明文化する。

Exit Criteria:

- `Stages.md`、`Characters.md`、`Facilities.md`、`StageProgressionSummary.md` が現行 JSON と一致している。
- 仕様変更が必要な場合は、リリース後対応かリリース前必須かを分けて判断できる。

## Milestone 2: 最低限の遊び切り確認

Status: Done

- Debug Battle Presets で章の節目を確認する。
- Stage 1、3、5、7、8、19、20、23、25、34、35、40 を重点確認する。
- Stage 7 と Stage 25 はシミュレーション上の残HPが低いため、手動確認では優先する。
- 勝敗だけでなく、操作不能、表示欠落、進行停止がないかを見る。

Automation Notes:

- 軽量シミュレーションは完了済み。
- MCP/Unity 実行チェックで重点ステージの開始導線を確認済み。
- 詳細: `/Users/yuya/Documents/Codex/2026-04-30/unity-mcp/milestone2_unity_report.md`

Exit Criteria:

- 重点ステージでクリア不能級の詰まりがない。
- 進行ロック、章解放、研究所解放が止まらない。

## Milestone 3: UI/表示の安定化

Status: Done

- 日本語フォント、召喚確率タブ、戦闘中 Lv/方向表示を確認する。
- 画面幅やタブ切り替えで文字が消えないことを確認する。
- 旧 `ApplyJapaneseFont` 系の再導入がないか確認する。

Exit Criteria:

- Top、Stage、Formation、Facility、Summon、Battle の主要表示に文字欠けがない。
- Console にリリース阻害の Error が残っていない。

Verification:

- TMP Settings の default/fallback が `NotoSansJP-Medium SDF` を参照することを確認済み。
- Play Mode 上の主要テキストで空文字、透明、極小フォントがないことを確認済み。
- 詳細: `/Users/yuya/Documents/Codex/2026-04-30/unity-mcp/milestone3_ui_report.md`

## Milestone 4: セーブ/リセット/進行状態

Status: Done

- 新規データで Stage 1 から始められることを確認する。
- 途中データで施設、所持キャラ、クリア済みステージが復元されることを確認する。
- Debug 用の進行補助が通常プレイへ漏れないことを確認する。

Exit Criteria:

- 新規開始、再起動後復帰、データリセットの基本導線が壊れていない。

Verification:

- GameManager、PlayerInventory、FacilityManager に PlayerPrefs ベースの進行保存/復元を追加。
- 施設効果は保存済み状態とランタイム再計算値を分け、レベル上限やキャラ解放の二重適用を避ける。
- DebugBattleOverlay は EditorPrefs または起動引数で明示有効化した場合のみ起動する。
- 詳細: `/Users/yuya/Documents/Codex/2026-04-30/unity-mcp/milestone4_save_report.md`

## Milestone 5: リリース前バグ潰し

Status: Done

- Missing Script、obsolete warning、TMP 関連 warning/error を確認する。
- ビルド対象外のデバッグ表示や不要ログを整理する。
- Unity の Play 中変更に由来する一時的な Missing Script は、再発性がある場合だけ修正対象にする。

Exit Criteria:

- 通常操作で再現する Error がない。
- リリースノートに既知の制限を書ける状態になっている。

Verification:

- Scene/Prefab YAML の missing script 参照なし。
- Play Mode 上の missing script 数は 0。
- `FindFirstObjectByType`、ランタイム `CreateFontAsset`、旧 `ApplyJapaneseFont` 呼び出しの残存なし。
- DebugBattleOverlay の通常起動漏れなし。
- 詳細: `/Users/yuya/Documents/Codex/2026-04-30/unity-mcp/milestone5_bug_report.md`

## Milestone 6: リリース候補ビルド

Status: Pending

- Release Candidate を作成する。
- 最小スモークテストを実施する。
- リリース後に回す改善項目を Backlog として分離する。

Exit Criteria:

- 配布可能なビルドがあり、既知の重大不具合が残っていない。
