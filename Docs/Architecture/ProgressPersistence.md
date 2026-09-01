# Progress Persistence

## Storage

Progress is stored in browser-local `PlayerPrefs` for WebGL. The saved state is split between these runtime singletons:

- `GameManager`: gold, stage points, highest cleared stage, unlocked chapter, and active summon category.
- `PlayerInventory`: owned characters, character levels/counts, summonable character IDs, and the global level-cap bonus.
- `FacilityManager`: unlocked facilities, facility levels, and research level-cap unlocks.

## Character restoration rule

`PlayerInventory` resolves saved character IDs through the serialized `CharacterDatabase` reference on `SampleScene`. This reference must remain assigned when editing the scene. The `Resources/CharacterDatabase` lookup is only a fallback for compatibility; WebGL builds must not depend on that fallback because the database asset is not under `Resources`.

When adding a character, update the character data, `CharacterDatabase.asset`, and the data importer inputs together. Existing saves should continue to restore characters by ID even when they are no longer in the initial summonable list.

## Verification checklist

1. Obtain at least one character beyond the initial summonable list.
2. Reload the WebGL page and confirm the character, level, count, and summon unlock remain.
3. Confirm the formation list contains the restored character.
4. Use the reset-data action only for an intentional clean-save test.

