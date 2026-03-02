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

各データはJSONで定義され、`DataImporter` により ScriptableObject に変換される。
詳細は以下の分割ドキュメントにまとめる。

- Characters: `Docs/GameDesign/Characters.md`
- Facilities: `Docs/GameDesign/Facilities.md`
- Stages: `Docs/GameDesign/Stages.md`

Importer動作（概要）:

1. Tools > Import JSON Data を実行  
2. JSONから `.asset` を生成 / 更新  
3. 各Databaseを再構築  

============================================================

## 3. References

- Data importer: `Assets/Scripts/Editor/DataImporter.cs`
- Data JSON: `Assets/Data/*.json`
- ScriptableObjects: `Assets/ScriptableObjects/*`

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
