# KanjiBattle Game Design Specification

Version: 1.1
Last Updated: 2026-08-13
Engine: Unity 2022+
Language: C# / JSON + ScriptableObject Based Architecture

## 1. Overview

KanjiBattle は、漢字をモチーフとした戦略 RPG です。プレイヤーは漢字キャラクターを召喚し、成長、編成、施設強化を通して全8章40ステージを攻略します。

Core Loop:

1. ゴールドを得る
2. キャラクターを召喚する
3. 編成してステージへ挑む
4. クリア報酬の Stage Point を得る
5. 施設でキャラクター解放、編成枠、レベル上限、章解放を進める
6. 次ステージへ進む

## 2. Release Baseline

- 章構成: 全8章、各5ステージ、合計40ステージ。
- 最終ステージ: Stage 40 `竜の座`。
- 初期キャラクター: `一`。
- 研究所解放: Stage 2 以降、固定順でキャラクターを追加。
- 初期レベル上限: 5。
- 編成枠: ステージ `slotCount` と訓練場の進行で制御。
- バランス方針: 現時点では全章クリア可能想定として、追加調整は重大詰まりのみ対象にする。

## 3. Data Architecture

各データは JSON で定義し、`DataImporter` により ScriptableObject に変換します。

- Characters: `Docs/GameDesign/Characters.md`
- Facilities: `Docs/GameDesign/Facilities.md`
- Stages: `Docs/GameDesign/Stages.md`
- Progression Summary: `Docs/GameDesign/StageProgressionSummary.md`
- Release Milestones: `Docs/GameDesign/ReleaseMilestones.md`

Importer:

1. Unity Editor で `Tools > Import JSON Data` を実行する。
2. `Assets/Data/*.json` から `.asset` を生成または更新する。
3. Character / Facility / Stage の各 Database を再構築する。

## 4. Current Release Risks

- Play 中の実装変更に由来する Missing Script は、通常手順で再現しない限り低優先とする。
- UI 表示は日本語フォントと TMP のフォールバックに依存するため、主要画面の目視確認をリリース前必須にする。
- バランスはシミュレーションと Debug Presets で確認済み想定だが、最終的な手触り確認は Milestone 2 で行う。

## 5. References

- Data importer: `Assets/Scripts/Editor/DataImporter.cs`
- Data JSON: `Assets/Data/*.json`
- ScriptableObjects: `Assets/ScriptableObjects/*`
- Balance helper skill: `~/.codex/skills/kanji-battle-balance`
