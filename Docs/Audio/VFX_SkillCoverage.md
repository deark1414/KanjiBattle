# KanjiBattle Skill VFX Coverage

Updated: 2026-09-04

This table tracks whether each runtime skill has a dedicated visual asset. Existing catalog cells are the current single-frame atlas effects. Generated sheets are visual prototypes only until real alpha, fixed-cell slicing, and Unity/WebGL review are complete.

| Skill | Character | Current runtime VFX | Generated prototype | Dedicated animated sprite status | Priority |
| --- | --- | --- | --- | --- | --- |
| Slash | 剣 | Catalog cell 22 + slash routine | Greatsword slash | Prototype reviewed; double-edged greatsword direction is good, alpha cleanup pending | High |
| StunBlow | 槌 | Generic trail / stun text | None | Missing dedicated hammer swing, impact, and stun ring | High |
| WaterHeal | 水 | Generic heal burst | None | Missing dedicated water ring, droplets, and upward particles | Medium |
| Fireball | 火 | Catalog cell 28 + falling fire routine | Fireball sheet | Prototype reviewed; grounded fire/stone direction is good, alpha cleanup pending | High |
| Arrow | 矢 | Catalog cell 16 + projectile routine | Arrow-only sheet | Prototype reviewed; bow removed, alpha cleanup pending | High |
| Spear | 槍 | Catalog cell 17 + projectile routine | None | Missing dedicated spear thrust/throw sheet | High |
| Gun | 銃 | Catalog cell 18 + line projectile routine | None | Missing dedicated muzzle flash and projectile sheet | High |
| Stone | 石 | Catalog cell 19 + projectile routine | None | Missing dedicated lift, arc, impact, and debris sheet | Medium |
| WoodPush | 木 | Catalog cell 20 + trail routine | None | Missing dedicated roots/timber push sheet | Medium |
| Soil | 土 | Generic trail plus trap logic | None | Missing dedicated soil trap activation sheet; must not imply all highlighted cells are damaged | Medium |
| BirdRetreat | 鳥 | Generic trail / retreat logic | None | Missing dedicated wing streak and backward motion sheet | Low |
| TigerTwinClaw | 虎 | Catalog cell 22 + double slash routine | None | Missing dedicated two-claw slash sheet; current catalog cell is shared with 剣 | Medium |
| HorseCharge | 馬 | Generic trail / charge logic | None | Missing dedicated charge dust and rush sheet | Medium |
| Counter | 盾 | Generic counter effect | None | Missing dedicated shield deflection and reflected spark sheet | High |
| AreaCounter | 壁 | Generic area counter effect | None | Missing dedicated wall rise, block flash, and shockwave sheet | High |
| Armor | 鎧 | Generic defense effect | None | Missing dedicated armor shimmer and plate impact sheet | Medium |
| NumberPassive | 一〜九 | Persistent number aura / text | Existing aura atlas | Covered by 3x3 aura atlas; verify intensity and loop separately | High |
| Dragon | 竜 | Catalog cell 35 + breath/range logic | Effect-only breath sheet | Prototype reviewed; dragon removed and area cone is good, alpha cleanup pending | High |

## Coverage Summary

- Runtime skill types: 18.
- Dedicated catalog mappings: 8 skill types, with `Slash` and `TigerTwinClaw` currently sharing one cell.
- Dedicated animated prototypes: 4 core skills, all still awaiting alpha cleanup and Unity import verification.
- Number passive: covered by the separate 3x3 aura atlas.
- Skills requiring new dedicated animated sheets: StunBlow, WaterHeal, Spear, Gun, Stone, WoodPush, Soil, BirdRetreat, TigerTwinClaw, HorseCharge, Counter, AreaCounter, Armor.

## Production Order

1. Finish and import the four reviewed core sheets: Arrow, Fireball, Greatsword Slash, and Dragon Breath Area.
2. Produce the high-impact missing set: Hammer/StunBlow, Spear, Gun, Shield/Counter, and Wall/AreaCounter.
3. Produce elemental and movement set: WaterHeal, Stone, WoodPush, Soil, HorseCharge, and BirdRetreat.
4. Produce the dedicated TigerTwinClaw sheet and replace the shared Slash catalog mapping.
5. Verify every skill in the VFX gallery and at least one real WebGL battle before wiring the final assets into runtime.

## Naming Rules

Use explicit names and never replace another skill's asset:

- `arrow_attack_sheet`
- `fireball_attack_sheet`
- `sword_greatsword_slash_sheet`
- `dragon_breath_area_sheet`
- `hammer_stunblow_sheet`
- `shield_counter_sheet`
- `wall_area_counter_sheet`

