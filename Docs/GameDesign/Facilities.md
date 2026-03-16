# Facilities

## Current Facilities

Id | FileName | Name | EffectType | UnlockType | RequiredStageId | UnlockCost | InitialMax | FinalMax | CapIncrease | CapUnlocks(Stage:Pts) | Description
---|---|---|---|---|---|---|---|---|---|---|---
1 | FacilityData_ResearchLab | キャラ解放施設 | CharacterUnlock | StagePoint | 1 | 1 | 1 | 26 | 1 | 2:5, 3:5, 4:5, 5:5, 6:5, 7:25, 8:25, 9:25, 11:125, 12:125, 13:125, 15:625, 16:625, 17:625, 19:3125, 20:3125, 21:3125, 22:3125, 23:15625, 24:15625, 25:15625, 27:78125, 28:78125, 29:78125, 32:390625 | キャラ解放の進行施設
2 | FacilityData_GoldWorkshop | 金貨生産施設 | GoldProduction | StagePoint | 3 | 5 | 5 | 40 | 5 | 6:5, 10:25, 14:125, 18:625, 22:3125, 26:15625, 31:78125 | ゴールド生産効率アップ
3 | FacilityData_SummonAltar | 召喚コスト軽減施設 | SummonCostDown | StagePoint | 1 | 1 | 5 | 45 | 5 | 3:5, 6:5, 10:25, 14:125, 18:625, 22:3125, 26:15625, 31:78125 | 召喚コスト軽減
4 | FacilityData_Smithy | 強化コスト軽減施設 | UpgradeCostDown | StagePoint | 3 | 5 | 5 | 40 | 5 | 6:5, 10:25, 14:125, 18:625, 22:3125, 26:15625, 31:78125 | 強化コスト軽減
5 | FacilityData_VictoryMonument | ステージポイント増加施設 | StagePointBoost | StagePoint | 1 | 1 | 5 | 45 | 5 | 3:5, 6:5, 10:25, 14:125, 18:625, 22:3125, 26:15625, 31:78125 | ステージポイント増加
6 | FacilityData_TrainingGround | 編成枠拡張施設 | FormationSlot | StagePoint | 1 | 25 | 1 | 4 | 1 | 3:5, 14:125, 22:3125 | 編成枠の増加
7 | FacilityData_MagicArchive | レベル上限解放施設 | LevelCap | StagePoint | 10 | 125 | 1 | 5 | 5 | 14:125, 18:625, 22:3125, 26:15625 | キャラレベル上限増加
8 | FacilityData_CastleGate | 章解放施設 | ChapterUnlock | StagePoint | 3 | 5 | 1 | 1 | 0 | 6:5, 10:25, 14:125, 18:625, 22:3125, 26:15625, 31:78125 | 章の解放
9 | FacilityData_SummonRateUp_Number1 | 召喚率強化・数字1 | SummonRateUp | StagePoint | 3 | 5 | 5 | 10 | 1 | 6:5, 10:25, 14:125, 18:625, 22:3125 | 数字1カテゴリの召喚率上昇
10 | FacilityData_SummonRateUp_Number2 | 召喚率強化・数字2 | SummonRateUp | StagePoint | 14 | 625 | 5 | 10 | 5 | 18:625 | 数字2カテゴリの召喚率上昇
11 | FacilityData_SummonRateUp_Number3 | 召喚率強化・数字3 | SummonRateUp | StagePoint | 31 | 390625 | 5 | 10 | 5 | 32:390625 | 数字3カテゴリの召喚率上昇
12 | FacilityData_SummonRateUp_Weapon | 召喚率強化・武器 | SummonRateUp | StagePoint | 6 | 25 | 5 | 10 | 1 | 10:25, 14:125, 18:625, 22:3125, 26:15625 | 武器カテゴリの召喚率上昇
13 | FacilityData_SummonRateUp_Defense | 召喚率強化・防御 | SummonRateUp | StagePoint | 10 | 125 | 5 | 10 | 1 | 14:125, 18:625, 22:3125, 26:15625, 31:78125 | 防御カテゴリの召喚率上昇
14 | FacilityData_SummonRateUp_Ranged | 召喚率強化・遠隔 | SummonRateUp | StagePoint | 18 | 3125 | 5 | 10 | 5 | 22:3125 | 遠隔カテゴリの召喚率上昇
15 | FacilityData_SummonRateUp_Nature | 召喚率強化・自然 | SummonRateUp | StagePoint | 22 | 15625 | 5 | 10 | 5 | 26:15625 | 自然カテゴリの召喚率上昇
16 | FacilityData_SummonRateUp_Animal | 召喚率強化・動物 | SummonRateUp | StagePoint | 26 | 78125 | 5 | 10 | 5 | 31:78125 | 動物カテゴリの召喚率上昇

## Effect Types

GoldProduction: ゴールド生産効率を上げる。
SummonCostDown: 召喚コストを下げる。
UpgradeCostDown: 強化コストを下げる。
StagePointBoost: ステージポイント獲得を増やす。
FormationSlot: 編成枠を増やす。
LevelCap: キャラクターのレベル上限を引き上げる。
CharacterUnlock: キャラを解放する。
ChapterUnlock: 章を解放する。
BossUnlock: ボス解放（召喚不可想定）。
SummonRateUp: 指定カテゴリの召喚率を上げる。

## Unlock Types

Free: 初期から使用可能。
StagePoint: ステージポイントで解放。

## Unlock Design (Draft)

- 重要施設（キャラ解放/編成枠/レベル上限/章解放）はStagePointで解放。
- それ以外の補助系もStagePointで解放し、必要ステージを通過した後にポイント消費で解放する。
- Free枠を作る場合は初期施設（キャラ解放/召喚コスト軽減/ステージポイント増加）を候補にする。
