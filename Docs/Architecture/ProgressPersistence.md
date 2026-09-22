# Progress Persistence

## Storage

Progress is stored in browser-local `PlayerPrefs` for WebGL. The saved state is split between these runtime singletons:

- `GameManager`: stage points, highest cleared stage, unlocked chapter, and the selected battle-speed tier.
- `PlayerInventory`: owned characters, character levels, and accumulated experience.
- `FacilityManager`: unlocked facilities, facility levels, and each facility's completed level-cap unlocks.
- `ResearchBondService`: 撃破済み・未加入の仲間候補IDと、各候補の縁の値。

Gold、召喚プール、召喚カテゴリ、キャラクター重複数は廃止済みである。進行データには形式バージョンを持たせ、現行形式と一致しない場合は移行せず全体を新規セーブとして初期化する。

## Character restoration rule

`PlayerInventory` resolves saved character IDs through the serialized `CharacterDatabase` reference on `SampleScene`. This reference must remain assigned when editing the scene. The `Resources/CharacterDatabase` lookup is only a fallback for compatibility; WebGL builds must not depend on that fallback because the database asset is not under `Resources`.

When adding a character, update the character data, `CharacterDatabase.asset`, and the data importer inputs together. Current-format saves restore owned characters by ID.

## Verification checklist

1. Obtain at least one character through bond progression.
2. 加入可能な敵を倒して勝利し、トップの仲間一覧に撃破済み候補と縁の進行が表示されることを確認する。
3. Reload the WebGL page and confirm the character、level、experience、撃破済み候補、縁の進行が残る。
4. Confirm the formation list contains the restored character.
5. Use the reset-data action only for an intentional clean-save test.
6. 施設が現在の上限へ到達した後、指定ステージをクリアすると、Stage Pointを消費せず次の上限を解放できることを確認する。
