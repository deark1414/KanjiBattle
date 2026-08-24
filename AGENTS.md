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
- The current safe fallback is to restore the original `NotoSansJP-VF.otf` contents into `Assets/Fonts/NotoSansJP-Medium.ttf`, preserving the Unity asset GUID/path, then rebuild the TMP SDF asset and WebGL output.
- `JapaneseFontProvider` should prefer `TMP_Settings.defaultFontAsset` when it is a `NotoSansJP` asset. Avoid preferring stale scene-embedded `NotoSansJP-Medium Runtime SDF` assets over the project font asset.
- `NotoSansJP-Medium SDF.asset` must keep required glyphs in the asset for WebGL. The font rebuild method should read `tmp/font/glyphs.txt`, call `TryAddCharacters`, and keep `m_ClearDynamicDataOnBuild: 0`; otherwise Unity/TMP may clear dynamic glyph data during build or editor quit.
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
