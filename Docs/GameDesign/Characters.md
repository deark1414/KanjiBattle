# Characters

Last Updated: 2026-09-21

## Current Characters

Id | FileName | Name | Category | SkillType | BaseHP | HPGrowth | BaseAtk | AtkGrowth | Def | SkillPower | SkillChance | Boss
---|---|---|---|---|---|---|---|---|---|---|---|---
1 | CharacterData_1 | 一 | Number1 | NumberPassive | 120 | 8 | 20 | 2 | 10 | 1.0 | 0 | 
2 | CharacterData_2 | 二 | Number1 | NumberPassive | 100 | 5 | 25 | 4 | 8 | 1.0 | 10 | 
3 | CharacterData_3 | 三 | Number1 | NumberPassive | 160 | 10 | 15 | 2 | 12 | 1.2 | 15 | 
10 | CharacterData_Sword | 剣 | Weapon | Slash | 120 | 5 | 40 | 4 | 8 | 1.3 | 10 | 
11 | CharacterData_Spear | 槍 | Weapon | Spear | 130 | 6 | 35 | 5 | 9 | 1.4 | 15 | 
12 | CharacterData_Hammer | 槌 | Weapon | StunBlow | 160 | 7 | 30 | 3 | 12 | 1.5 | 10 | 
13 | CharacterData_Shield | 盾 | Defense | Counter | 180 | 9 | 20 | 2 | 10 | 1.0 | 15 | 
14 | CharacterData_Armor | 鎧 | Defense | Armor | 150 | 10 | 25 | 3 | 15 | 1.0 | 0 | 
15 | CharacterData_Wall | 壁 | Defense | AreaCounter | 220 | 10 | 10 | 1 | 20 | 1.0 | 0 | 
16 | CharacterData_Stone | 石 | Ranged | Stone | 130 | 6 | 28 | 3 | 6 | 1.1 | 10 | 
17 | CharacterData_Arrow | 矢 | Ranged | Arrow | 120 | 5 | 35 | 4 | 6 | 1.2 | 10 | 
18 | CharacterData_Gun | 銃 | Ranged | Gun | 110 | 5 | 45 | 5 | 5 | 1.5 | 10 | 
19 | CharacterData_Fire | 火 | Nature | Fireball | 120 | 7 | 30 | 4 | 8 | 1.4 | 15 | 
20 | CharacterData_Water | 水 | Nature | WaterHeal | 150 | 8 | 25 | 3 | 10 | 1.3 | 15 | 
21 | CharacterData_Wood | 木 | Nature | WoodPush | 160 | 9 | 22 | 3 | 10 | 1.2 | 15 | 
22 | CharacterData_Soil | 土 | Nature | Soil | 180 | 10 | 18 | 2 | 12 | 1.0 | 0 | 
23 | CharacterData_Horse | 馬 | Animal | HorseCharge | 140 | 6 | 38 | 4 | 8 | 1.5 | 15 | 
24 | CharacterData_Bird | 鳥 | Animal | BirdRetreat | 130 | 5 | 33 | 3 | 6 | 1.3 | 15 | 
25 | CharacterData_Tiger | 虎 | Animal | TigerTwinClaw | 120 | 6 | 42 | 5 | 8 | 1.6 | 15 | 
4 | CharacterData_4 | 四 | Number2 | NumberPassive | 130 | 7 | 28 | 3 | 10 | 1.2 | 10 | 
5 | CharacterData_5 | 五 | Number2 | NumberPassive | 100 | 5 | 35 | 5 | 10 | 1.0 | 15 | 
6 | CharacterData_6 | 六 | Number2 | NumberPassive | 180 | 10 | 18 | 2 | 12 | 1.5 | 20 | 
7 | CharacterData_7 | 七 | Number3 | NumberPassive | 130 | 8 | 40 | 5 | 12 | 1.3 | 10 | 
8 | CharacterData_8 | 八 | Number3 | NumberPassive | 110 | 6 | 50 | 5 | 10 | 1.2 | 15 | 
9 | CharacterData_9 | 九 | Number3 | NumberPassive | 200 | 10 | 25 | 3 | 14 | 1.2 | 10 | 
26 | CharacterData_Dragon | 竜 | Boss | Dragon | 1300 | 15 | 115 | 2 | 35 | 1.4 | 15 | Yes

## Research Unlock Plan

初期召喚対象は `一` です。以降は研究所で、下記の必要ステージをクリアしたキャラクターから順に解放します。

Character | Required Stage
---|---
二 | 2
三 | 3
剣 | 6
槍 | 7
槌 | 8
盾 | 11
鎧 | 12
壁 | 13
四 | 16
五 | 17
六 | 18
石 | 21
矢 | 22
銃 | 23
火 | 26
水 | 27
木 | 28
土 | 29
馬 | 31
鳥 | 32
虎 | 33
七 | 36
八 | 37
九 | 38

## Categories

- Number1: 低位数字。一二三。味方ターン開始時、自分以外の生存数字の種類数に応じて攻撃力が `+15% / +30% / +50%` になる。単独では発動しない。生存する味方全員が一の場合は隠し編成となり、2〜5体で `+15% / +30% / +150% / +300%` になる。4体・5体の大幅強化は一染めだけで有効。
- Number2: 中位数字。四五六。味方ターン開始時、自分以外の生存数字の種類数に応じて各自が最大HPの `5% / 10% / 15%` を自動回復する。低位・上位との組み合わせも数字の種類数に含める。
- Number3: 上位数字。七八九。味方ターン開始時、自分以外の生存数字の種類数に応じて、経過ラウンドごとの攻撃倍率が `1.03^n / 1.05^n / 1.10^n` になる。攻撃力増加の上限は `+100% / +200% / +300%`。編成が崩れた場合は次の味方ターンに現行構成で再計算する。
- Weapon: 剣、槍、槌。
- Defense: 盾、鎧、壁。
- Ranged: 石、矢、銃。
- Nature: 火、水、木、土。
- Animal: 馬、鳥、虎。
- Boss: 竜。召喚不可想定。

## Number Passive Resolution

数字パッシブは通常攻撃時には発動しない。各陣営のターン開始時に、生存中の同じ陣営だけを数えて効果を確定する。通常編成では自分以外の数字の種類数を低・中・上共通で使うため、低位と中位、上位の組み合わせも相互に強化へ寄与する。確定した強さに応じて、低位・中位・上位それぞれの弱・中・強オーラを表示する。上位は初回ターンでは0ラウンドとして開始し、以後の生存ラウンドで複利的に強くなる。
