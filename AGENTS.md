# AGENTS.md

## Project Context

- This repository is the Unity project for KanjiBattle.
- Prefer repository-specific notes here when the rule only applies to this project.
- Use general Codex skills only when the workflow should be reusable outside this repository.

## Unity MCP

- Use Unity MCP when Unity state, scenes, play mode, console logs, assets, or GameObject inspection are relevant.
- Prefer MCP inspection over guessing from files when the user asks to verify the current Unity screen or runtime behavior.
- If MCP is unavailable, first confirm whether Unity is open and the MCP server is running.
- The user previously re-set up MCP using this reference and asked to be reminded of it when MCP is stopped:
  `https://note.com/npaka/n/n19e5132847c6#81f3759e-63e1-4757-826e-923e02b8291c`
- Unity may need to be foregrounded by the user for compilation or editor refresh to proceed. If changes appear not to compile, ask the user to bring Unity to the front before assuming the code is broken.
- For UI or font issues, verify the result in Unity Play mode or through the visible game screen when possible.

## Communication

- User-facing reports, plans, review results, and project documents should be written in Japanese by default. Keep English only for code identifiers, asset filenames, API names, and required technical terms.

## Generated VFX Workflow

- When the user requests new VFX or changes the VFX art direction, update `Docs/Audio/VFX_AnimatedAssetBrief.md` and continue directly into asset generation in the same run.
- Do not stop after writing the brief: generate at least one representative sprite sequence, inspect its visual result, and verify transparency, fixed-cell layout, safe padding, stable origin, and intended direction.
- Keep generated drafts separate from production assets until they pass review. Only then copy them into `Assets/Resources/VFX/` and wire them into Unity/gallery playback.
- For animated VFX, prefer grounded shogi-fantasy materials such as wood, iron, dust, smoke, sparks, embers, and water. Avoid futuristic beams, neon outlines, and holographic effects unless explicitly requested.
- Runtime default (2026-09-20): use one reviewed, opaque representative sprite and animate its position, rotation, scale, and fade in code. This is the preferred presentation for projectiles, elemental effects, defense, auras, animals, and dragon effects because it avoids WebGL sprite-sequence visibility and atlas-seam regressions. Sword, Hammer, and Spear retain bespoke single-sprite motion paths, but no normal battle VFX may reintroduce frame-by-frame switching without WebGL proof.

### WebGL Weapon VFX Visibility

- Sword and hammer motion VFX must be parented under `WeaponVfxOverlay`, a regular final child of `BattlePanel` with no nested Canvas. Position each VFX with the caster's world position. Do not parent a long weapon directly under the caster (later grid cells will cover the extended blade), and do not use `BattleVfxOverlay` or another sibling Canvas (WebGL UI batching can draw the weapon body behind the board, leaving only a faint residual shadow visible).
- Keep the weapon art on the direct `RawImage` path. `Assets/Resources/VFX/Weapons/SwordMotion.png` and `HammerMotion.png` must remain `Default` textures, not Sprite assets, and use WebGL `RGBA32` with compression disabled. `GeneratedVfxImporter.ConfigureWeaponMotionTextures()` validates this during every WebGL build.
- When a weapon appears faint in battle but clear in an HTML preview, check parent hierarchy and draw order before regenerating art or changing texture alpha. The primary regression check is that `WeaponMotionVfxRoutine` and `HammerSwingVfxRoutine` attach to `EnsureWeaponVfxOverlay()`, never directly to `casterRect` or `EnsureBattleVfxOverlay()`.
- Derive weapon rotation from the resolved grid direction (`target.gridPos - caster.gridPos`), never from temporary `RectTransform.position` differences. Grid Y grows downward while UI rotation grows upward, so convert with `(gridX, -gridY)` before calling `Atan2`. This keeps each of the eight attack directions distinct even while a layout rebuild or hit shake is occurring.
- Treat the source-art tip angle and the swing animation as separate values. For the sword, the resolved grid direction is the center of a sweep, not its endpoint: the visible blade crosses the two diagonal side cells of the forward wedge in the first roughly 35% of the short effect, then holds its lower-side end pose. For a right attack this is upper-right to lower-right; for a left attack it is upper-left to lower-left.
- Use a cubic (`progress^3`) sweep curve for the sword's visible 90-degree cross-wedge motion so it accelerates toward the lower-side cell. Keep the short pre-wind smooth and inside the caster cell.
- The hammer is caster-centered: use the source art's handle-end pivot, never the hammer head, and keep that pivot in the caster cell. Rotate the whole hammer roughly 70 degrees with a quadratic (`progress^2`) curve so the hammer head swings from the upper side into the selected target cell; do not place the pivot above the target.
- Current reviewed motion targets are a 120-degree sword sweep and a caster-pivoted overhead hammer strike. Keep those values synchronized between `BattleManager` and `Docs/vfx-weapon-motion-review.html`.
- A wide sword sweep must not show a full-length blade in its 30-degree pre-wind pose. Begin at about 28% reach inside the caster cell, then extend it by the upper-side cell of the forward wedge; otherwise the start appears to attack cells outside the effect range.
- Do not fade a weapon through the attack pose. Keep alpha at `1` until the final few percent of the short VFX duration, then remove it quickly. A long tail makes the material look like a transparent shadow in WebGL captures. For a delayed fade, normalize `progress` with `Mathf.InverseLerp(fadeStart, 1f, progress)` before `SmoothStep(0f, 1f, normalized)`: passing `fadeStart` directly as the first `SmoothStep` argument produces a near-transparent image from frame one.
- Defensive VFX (`Shield`, `Armor`, `Wall`) must be direct final children of their owner character, so they stay in front of that character, follow movement, and disappear on death. Keep them behind weapon and global attack VFX by not putting them on the global attack overlay. For externally positioned target VFX, add `BattleVfxFollowTarget` so they follow movement and self-remove when the target is destroyed.
- Trigger landing-trap VFX and damage only after the movement animation reaches the landing cell. Remove the trap occupancy immediately to prevent a duplicate trigger, then resolve damage through `ResolveTrapAfterMovement`.
- The current local VFX review build enables `SkillExecutor.ForceSkillActivationForVfxReview`, which forces regular skills, counters, and number-passive rolls to 100% without changing balance data. It also treats sword and hammer hits as counter-eligible solely to review Shield and Wall. Disable it before balance verification or release builds.

## Deployment Policy

- GitHub Pages deployment is based on `main`.
- When the user asks to deploy, complete the flow through the `main` merge and deployment verification in the same run.
- Do not stop after creating a PR or merging only to `develop` unless the user explicitly asks to stop there.
- Standard deployment flow:
  1. Ensure the target changes are committed and pushed.
  2. Merge the PR or working branch into `develop`.
  3. Update local `main` from `origin/main`.
  4. Merge latest `origin/develop` into `main`.
  5. Push `main`.
  6. Check the GitHub Pages workflow result with `gh run list`, `gh run watch`, or equivalent.
- Keep `develop` as the integration branch. Do not change GitHub Pages to deploy from `develop` unless the user explicitly requests a release policy change.

## Balance Tuning

- Use the `kanji-battle-balance` Codex skill for stage difficulty, character stats, boss values, chapter pacing, and balance regression work.
- Prefer running the lightweight simulator before editing balance data so changes are based on repeatable numbers.
- Focused simulation example:

```bash
ruby ~/.codex/skills/kanji-battle-balance/scripts/simulate_balance.rb --project /Users/yuya/UnityProjects/KanjiBattle --focus 7,8,19,20,23,25,34,35,40
```

- Full simulation example:

```bash
ruby ~/.codex/skills/kanji-battle-balance/scripts/simulate_balance.rb --project /Users/yuya/UnityProjects/KanjiBattle --all
```

- The simulator reads `Assets/Data/characters.json` and `Assets/Data/stages.json`.
- Treat simulator output as a risk detector, not as final proof. Important changes still need Unity Play mode verification.
- When editing stage balance, keep these in sync:
  1. `Assets/Data/stages.json`
  2. matching `Assets/ScriptableObjects/Stages/Stage_*.asset`
- When editing character or boss balance, keep these in sync:
  1. `Assets/Data/characters.json`
  2. matching `Assets/ScriptableObjects/Characters/CharacterData_*.asset`
- Pay special attention to boss data. Bosses may be balanced at level 1, so high `enemyLevel` values can make them overpowered.
- After a tuning change, rerun the simulator and summarize before/after values.

## Battle Refactor Checks

- Battle geometry and passive formulas live in `BattleRangeService`, `BattleMovementRules`, and `NumberPassiveRules`. Do not reintroduce independent range calculations in BattleManager, previews, or VFX code.
- Before accepting battle-rule or data changes, run the two Unity batch checks below. They must both log `Passed.` and exit successfully:

```bash
/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath /Users/yuya/UnityProjects/KanjiBattle \
  -executeMethod BattleRuleRegression.RunFromCommandLine \
  -logFile /tmp/kanji-battle-rule-regression.log

/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -quit -projectPath /Users/yuya/UnityProjects/KanjiBattle \
  -executeMethod GameDataValidator.ValidateFromCommandLine \
  -logFile /tmp/kanji-battle-data-validation.log
```

- `SkillExecutor.TryExecute` remains the compatibility API. New callers that need failure diagnostics should use `TryExecuteDetailed`; do not treat unsupported data-driven effects as a successful activation.
- `PlayerProgressStore` is the only PlayerPrefs gateway. Preserve current `KanjiBattle.*` save keys unless an explicit save migration is added.

## WebGL Visual QA

- Use Playwright-managed Chromium for repeatable WebGL layout screenshots.
- UI prefabs are mostly structural templates. Before changing prefab visual values, check runtime layout owners documented in `Docs/Architecture/UIRuntimeLayout.md`.
- Use the local .NET SDK at `/Users/yuya/.dotnet/dotnet` for quick compile checks.
- `KanjiBattle.slnx` is not supported by the installed .NET 8 SDK, so build the Unity-generated project files directly:

```bash
DOTNET_CLI_TELEMETRY_OPTOUT=1 /Users/yuya/.dotnet/dotnet restore Assembly-CSharp.csproj
DOTNET_CLI_TELEMETRY_OPTOUT=1 /Users/yuya/.dotnet/dotnet restore Assembly-CSharp-Editor.csproj
DOTNET_CLI_TELEMETRY_OPTOUT=1 /Users/yuya/.dotnet/dotnet build Assembly-CSharp.csproj --no-restore
DOTNET_CLI_TELEMETRY_OPTOUT=1 /Users/yuya/.dotnet/dotnet build Assembly-CSharp-Editor.csproj --no-restore
```

- The current C# quick build emits Unity serialization warnings such as CS0649 for inspector-assigned fields; treat zero errors as the pass condition.
- Setup:

```bash
npm install
npm run install:browsers
```

- Run:

```bash
npm run qa:visual
```

- Screenshots and the JSON report are written under `tmp/playwright-screenshots/`.
- On macOS inside Codex, browser launch may need sandbox escalation.

## Font Optimization

- Correct Japanese and Latin glyph rendering is higher priority than font size.
- Do not replace `Assets/Fonts/NotoSansJP-Medium.ttf` with a subset font unless the resulting WebGL build has been visually verified in the actual browser game screen.
- A previous subset font caused incorrect TMP rendering in WebGL: `HP` appeared as `GP`, `ATK` appeared as `ASK`, some digits such as `0` looked wrong or disappeared, and some character-list status text was missing.
- If that kind of one-character shift, missing digit, or strange bold digit appears, suspect the font file / TMP atlas first, not the UI text strings.
- The current safe fallback is the official static `NotoSansJP-Medium.ttf` contents at `Assets/Fonts/NotoSansJP-Medium.ttf`, preserving the Unity asset GUID/path, then rebuild the TMP SDF asset and WebGL output. Do not restore the previous variable-font copy whose default weight was Thin.
- `JapaneseFontProvider` should prefer `TMP_Settings.defaultFontAsset` when it is a `NotoSansJP` asset. Avoid preferring stale scene-embedded `NotoSansJP-Medium Runtime SDF` assets over the project font asset.
- `NotoSansJP-Medium SDF.asset` must keep required glyphs in the asset for WebGL. The font rebuild method should read `tmp/font/glyphs.txt`, call `TryAddCharacters`, and keep `m_ClearDynamicDataOnBuild: 0`; otherwise Unity/TMP may clear dynamic glyph data during build or editor quit.
- Keep the Japanese SDF asset as one 4096px atlas with multi-atlas disabled. Multiple dynamic atlas textures caused WebGL glyphs to be read from the wrong atlas. Scene text must reference the project SDF asset rather than the obsolete embedded `NotoSansJP-Medium Runtime SDF`; repair existing references with `KanjiBattle.Editor.FontAssetMaintenance.RepairSceneJapaneseFontReferences`.
- Before regenerating the TMP font asset, collect the in-project glyph set:

```bash
npm run font:glyphs
```

- Rebuild the TMP SDF asset through Unity after collecting glyphs:

```bash
/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath /Users/yuya/UnityProjects/KanjiBattle \
  -executeMethod KanjiBattle.Editor.FontAssetMaintenance.RebuildJapaneseTmpFontAsset \
  -logFile /tmp/kanjibattle_rebuild_font.log
```

- If Unity-generated YAML has trailing spaces, remove trailing whitespace from `Assets/Fonts/NotoSansJP-Medium SDF.asset` before committing.
- After rebuilding the TMP SDF asset, verify that important glyphs were persisted:

```bash
rg -n "m_Unicode: 48|m_Unicode: 72|m_Unicode: 84|m_Unicode: 19968|m_ClearDynamicDataOnBuild" "Assets/Fonts/NotoSansJP-Medium SDF.asset"
```

- Expected examples: `m_Unicode: 48` for `0`, `72` for `H`, `84` for `T`, `19968` for `一`, and `m_ClearDynamicDataOnBuild: 0`.
- Only attempt subset regeneration as a separate optimization task, and keep this command as a reference rather than the default workflow:

```bash
python3 -m fontTools.subset Assets/Fonts/NotoSansJP-Medium.ttf \
  --text-file=tmp/font/glyphs.txt \
  --layout-features='*' \
  --glyph-names \
  --symbol-cmap \
  --legacy-cmap \
  --notdef-glyph \
  --notdef-outline \
  --recommended-glyphs \
  --name-IDs='*' \
  --name-legacy \
  --name-languages='*' \
  --output-file=/tmp/NotoSansJP-Medium.subset.ttf
```

- If a subset font is tested again, check browser-rendered `Gold`, `召喚 100G`, `HP`, `ATK`, `DEF`, `所持 x1`, and several digits in the in-app browser or Chrome before accepting it.
- After font changes, run:

```bash
npm run font:glyphs
DOTNET_CLI_TELEMETRY_OPTOUT=1 /Users/yuya/.dotnet/dotnet restore Assembly-CSharp.csproj
DOTNET_CLI_TELEMETRY_OPTOUT=1 /Users/yuya/.dotnet/dotnet restore Assembly-CSharp-Editor.csproj
DOTNET_CLI_TELEMETRY_OPTOUT=1 /Users/yuya/.dotnet/dotnet build Assembly-CSharp.csproj --no-restore
DOTNET_CLI_TELEMETRY_OPTOUT=1 /Users/yuya/.dotnet/dotnet build Assembly-CSharp-Editor.csproj --no-restore
npm run qa:visual
```

- `npm run qa:visual` is useful but not sufficient for font regressions by itself. Also inspect an actual rendered WebGL screen and confirm visible text, because Playwright layout screenshots can pass while glyph mapping is wrong.
