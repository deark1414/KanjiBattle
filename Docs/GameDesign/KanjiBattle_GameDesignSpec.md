# KanjiBattle Game Design Specification (Full Version)

Version: 1.0  
Last Updated: 2026-01-24  
Author: Yuya Koyama + ChatGPT Design Support  
Engine: Unity 2022+  
Language: C# / ScriptableObject Based Architecture  

============================================================

## 1. Overview

KanjiBattle は、漢字をモチーフとした戦略RPG × 放置ハクスラゲーム。
プレイヤーは「漢字キャラクター」を召喚し、成長・合成・配置・施設強化を通して
全10ステージ＋ボス（竜）を攻略する。

------------------------------------------------------------

### 🎮 Core Loop

ゴールド生産 → キャラ召喚 → 戦闘 → ステージクリア報酬（SP） →  
施設強化 / 新キャラ解放 → 次ステージ挑戦

#### 各要素の役割

要素 | 概要  
------|------
召喚 | ゴールドを消費してキャラを呼び出す。召喚ごとにコスト上昇。  
戦闘 | ターン制のグリッドバトル。敵味方交互行動。  
施設 | ゴールド生産・召喚コスト軽減・ステージポイント増加などの恒常効果。  
ステージ | 各章で敵構成・罠・報酬が変化。最終章でボス戦（竜）。  

============================================================

## 2. Data Architecture

### 2.1 JSON ⇄ ScriptableObject構造

各データはJSONで定義され、DataImporter により自動的に ScriptableObject に変換される。  
この構造により、バランス調整や大量データ管理を容易に行う。

データ種別 | JSONファイル | ScriptableObject | Database Asset  
------------|--------------|------------------|----------------  
キャラクター | Characters.json | CharacterData | CharacterDatabase.asset  
施設 | Facilities.json | FacilityData | FacilityDatabase.asset  
ステージ | Stages.json | StageData | StageDatabase.asset  

------------------------------------------------------------

### 2.2 ディレクトリ構成（推奨）

Assets/  
 ├─ Scripts/  
 │   ├─ Game/  
 │   │   ├─ Battle/  
 │   │   ├─ Manager/  
 │   │   └─ Data/  
 │   ├─ Editor/  
 │   │   └─ DataImporter.cs  
 │   └─ UI/  
 ├─ ScriptableObjects/  
 │   ├─ Characters/  
 │   ├─ Facilities/  
 │   ├─ Stages/  
 │   └─ Databases/  
 └─ Data/  
     ├─ Characters.json  
     ├─ Facilities.json  
     └─ Stages.json  

------------------------------------------------------------

### 2.3 Importer動作フロー

1. Tools > Import JSON Data 実行  
2. 各JSONをロードし、対応する .asset を生成 / 更新  
3. Database再構築：  
   - CharacterDatabase.asset  
   - FacilityDatabase.asset  
   - StageDatabase.asset  
4. Editor Logで完了メッセージ表示  
   ✅ すべてのJSONデータをScriptableObjectに反映しました。  

============================================================

## 3. Character System

### 3.1 概要

漢字キャラは6カテゴリに分類される。

カテゴリ | 代表例 | 特徴  
-----------|---------|------  
Number1–3 | 一〜九 | バランス・攻撃・HP特化（3区分）  
Weapon | 剣・槍・槌 | 攻撃重視・序中盤の主力  
Defense | 盾・鎧・壁 | HP・防御高め。鎧は軽減スキル所持  
Ranged | 石・矢・銃 | 遠距離攻撃・安定火力  
Nature | 火・水・木・土 | 特殊効果（回復・防御無視など）  
Animal | 馬・鳥・虎 | 終盤解放。範囲・突撃型  
Boss | 竜 | 最終ボス（召喚不可）  

------------------------------------------------------------

### 3.2 ステータス成長ルール

ステータス | 計算式 | 上限例  
-------------|---------|--------  
攻撃力 | baseAttack + attackGrowth × (Lv - 1) | 約300  
HP | baseHP + hpGrowth × (Lv - 1) | 約500  
防御力 | baseDefense + defenseGrowth × (Lv - 1) | 約60（竜除く）  

------------------------------------------------------------

### 3.3 スキル定義

SkillType | 効果 | 備考  
------------|------|------  
Slash / StunBlow / Counter | 通常攻撃系 | ダメージ倍率1.0〜1.5  
Armor | 受けるダメージを軽減 | Lv×2軽減 or 20%減少  
Heal / WaterHeal | 味方HP回復 | 基本HPの20〜30%  
Fireball | 防御無視攻撃 | 威力50%＋防御無視  
NumberPassive | 数字コンボ時バフ | 全体攻撃+防御無視強化  
HorseCharge / BirdRetreat / TigerTwinClaw | 動物特有スキル | 移動・多段・後退型  
Dragon | ブレス攻撃（防御無視） | ボス専用  

------------------------------------------------------------

### 3.4 JSON構造例

{
  "id": 1,
  "fileName": "CharacterData_1",
  "characterName": "一",
  "category": "Number1",
  "baseHP": 120,
  "hpGrowth": 8,
  "baseAttack": 20,
  "attackGrowth": 2,
  "baseDefense": 10,
  "defenseGrowth": 1,
  "skillType": "NumberPassive",
  "skillPower": 1.0,
  "skillChance": 0,
  "isBoss": false
}

============================================================

## 4. Facility System

### 4.1 概要

施設はプレイヤーの進行・経済・成長を支える基盤システム。  
施設レベルは上限解放（SP消費）で段階的に成長。

------------------------------------------------------------

### 4.2 施設一覧（MVP）

名称 | EffectType | 効果内容 | 備考  
------|-------------|-----------|------  
研究所 | CharacterUnlock | 新キャラ解放 | 進行型  
金貨工房 | GoldProduction | ゴールド生産+10%/Lv | 初期施設  
召喚祭壇 | SummonCostDown | 召喚コスト減少 | 進行加速  
鍛冶屋 | UpgradeCostDown | 強化コスト減少 | 強化支援  
勝利の碑 | StagePointBoost | ステージポイント+20%/Lv | 周回報酬向上  
訓練場 | FormationSlot | 編成枠+1/Lv | 編成拡張  
魔導書庫 | LevelCap | キャラ上限+5/Lv | キャラ育成支援  
城門 | ChapterUnlock | 新章解放 | 終盤施設  
召喚祭壇（カテゴリ別） | SummonRateUp | 特定カテゴリ召喚率上昇 | 各カテゴリ1種  

------------------------------------------------------------

### 4.3 JSON構造例

{
  "id": 2,
  "fileName": "FacilityData_GoldFactory",
  "facilityName": "金貨工房",
  "effectType": "GoldProduction",
  "unlockType": "Free",
  "initialMaxLevel": 5,
  "finalMaxLevel": 10,
  "effectPerLevel": 0.1,
  "levelCapIncreasePerUnlock": 2,
  "baseCost": 100,
  "growthFactor": 1.2,
  "requiredStageId": 1,
  "unlockStagePointCost": 0
}

============================================================

## 5. Stage System

### 5.1 ステージ構成概要

章 | ステージ範囲 | 敵カテゴリ | プレイヤー上限Lv | 備考  
----|----------------|--------------|------------------|------  
第1章 | 1〜3 | 数字・武器・防御 | 〜Lv5 | チュートリアル範囲  
第2章 | 4〜5 | 飛び道具 | 〜Lv10 | 召喚コスト上昇帯  
第3章 | 6〜7 | 自然 | 〜Lv20 | 属性コンボ導入  
第4章 | 8〜9 | 動物 | 〜Lv30 | 高コスト召喚帯  
第5章 | 10 | ボス（竜） | 〜Lv50 | 最終決戦  

------------------------------------------------------------

### 5.2 JSON構造例

{
  "stageId": 1,
  "fileName": "Stage_1_1",
  "stageName": "試練の草原",
  "chapterId": 1,
  "enemyIds": [1, 2, 3],
  "rewardStagePoints": 1,
  "slotCount": 3,
  "trapDamage": 10,
  "trapCount": 2,
  "reinforcementEnemyIds": [],
  "reinforcementInterval": 0,
  "reinforcementCount": 0,
  "reinforcementLimit": 0,
  "isBossStage": false,
  "enemyLevel": 1,
  "prerequisite": ""
}

============================================================

## 6. Database Integration

### 6.1 自動再構築仕様

データベース | 対応フォルダ | 含まれる要素  
---------------|--------------|---------------  
CharacterDatabase | ScriptableObjects/Characters | 全CharacterData  
FacilityDatabase | ScriptableObjects/Facilities | 全FacilityData  
StageDatabase | ScriptableObjects/Stages | 全StageData  

更新手順:  
1. JSON更新 → Import  
2. 各Database再構築  
3. Editorで整合性確認（Missingなし）

============================================================

## 7. Future Extensions

### 7.1 Chapter拡張
- 章を増やし、自然→機械→神話など新カテゴリを導入可能。

### 7.2 バランス調整方針
- Lv50時点で攻撃300 / HP500前後が標準。
- 防御軽減式:  
  damage = attack × (1 - (defense / (defense + 100)))

### 7.3 データ拡張ポリシー
- JSONはID基準で参照統一。  
- fileName・idは変更不可。  
- 追加項目は必ず後方互換に配慮。

============================================================

## 8. Appendix

### Unity上での操作例

Tools > Import JSON Data  
→ Character / Facility / Stage 各JSONを自動反映  
→ 各.assetが生成・更新  
→ Databases再構築  

============================================================

End of Document