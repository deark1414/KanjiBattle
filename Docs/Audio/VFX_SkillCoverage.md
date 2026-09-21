# KanjiBattle スキル別VFX対応表

更新日: 2026-09-21

本番VFXは `Assets/Resources/VFX/` にのみ配置する。確認用HTML、生成下書き、素材の複製はリリース対象に含めない。

| スキル | キャラクター | 本番VFX | 表示方法 |
| --- | --- | --- | --- |
| Slash | 剣 | `Weapons/SwordMotion` | 発動者中心の120度薙ぎ払い |
| StunBlow | 槌 | `Weapons/HammerMotion` | 発動者中心の70度振り下ろし |
| Spear | 槍 | `Animated/Spear/frame_03` | 根本固定で前方2マスへ突き出し |
| Arrow / Gun / Stone | 矢 / 銃 / 石 | 各 `Animated` 素材 | 直線飛翔、石のみ放物線 |
| Fireball / WaterHeal | 火 / 水 | 各 `Animated` 素材 | 対象セルの上方から落下 |
| WoodPush / Soil | 木 / 土 | 各 `Animated` 素材 | 根の前方スイング、罠の局所発動 |
| HorseCharge / BirdRetreat | 馬 / 鳥 | `Static/HorseChargeForkedWind`、`Static/BirdFeathersOnly` | 移動に追従する土煙、出発・到着時の羽 |
| TigerTwinClaw | 虎 | `Animated/TigerTwinClaw` | 反対向きに交差する二連爪撃 |
| Counter / AreaCounter / Armor | 盾 / 壁 / 鎧 | 各 `Animated` 素材 | 所有キャラクターの前面に短く表示 |
| NumberPassive | 一〜九 | `Animated/NumberAura_*` | ターン開始時の結束強度に応じて回転・拡縮 |
| Dragon | 竜 | `Animated/DragonBreath`、`Animated/DragonRoar` | 竜を起点にしたブレス、咆哮 |
| Boss summon | ボス | `Animated/BossSummon` | ボス出現時のみ表示 |

## 実装方針

- 通常VFXは、レビュー済みの不透明な代表スプライト1枚を、コードで位置・回転・拡縮・フェードする。
- 剣・槌・槍は、方向・支点・到達距離を専用ルーチンで管理する。
- 防御VFXは所有キャラクターの直接の子にし、移動時に追従して死亡時に消去する。
- テスト用の強制発動は `SkillExecutor.ForceSkillActivationForVfxReview` に限定し、通常ビルドでは必ず `false` とする。
