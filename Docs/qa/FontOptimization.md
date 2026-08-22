# Font Optimization

Current state: `Assets/Fonts/NotoSansJP-Medium.ttf` is subset for the game's current text, and `Assets/Fonts/NotoSansJP-Medium SDF.asset` is regenerated as a small dynamic TMP font asset. The SDF asset is force-tracked because `Assets/Fonts/*.asset` is ignored by default.

## Audit

```bash
npm run font:glyphs
```

This writes:

- `tmp/font/glyphs.txt`
- `tmp/font/glyphs-report.json`

Use the collected glyph list as a starting point when updating the subset source font.

## Recommended Safe Path

1. Update `tmp/font/glyphs.txt` with `npm run font:glyphs`.
2. Rebuild the subset `NotoSansJP-Medium.ttf` with `fonttools`.
3. Regenerate `NotoSansJP-Medium SDF.asset` in Unity:

```bash
/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -quit \
  -projectPath /Users/yuya/UnityProjects/KanjiBattle \
  -executeMethod KanjiBattle.Editor.FontAssetMaintenance.RebuildJapaneseTmpFontAsset \
  -logFile /tmp/kanjibattle_rebuild_font.log
```

4. Force-add the regenerated SDF asset because `Assets/Fonts/*.asset` is ignored:

```bash
git add -f Assets/Fonts/NotoSansJP-Medium\ SDF.asset Assets/Fonts/NotoSansJP-Medium\ SDF.asset.meta
```

5. Verify Japanese text in Top, Battle, Formation, Facilities, summon category tabs, battle log, and result panels.
6. Rebuild WebGL and compare `Docs/game/Build/game.data`.

Do not manually edit the serialized SDF asset. Missing glyphs are easy to introduce and hard to notice without visual checks.

## Current Release Approach

The source TTF can be subset with `fonttools` using the generated glyph list while keeping the same Unity asset GUID:

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

Replacing `Assets/Fonts/NotoSansJP-Medium.ttf` with this subset keeps existing Unity references intact. After doing so, regenerate the TMP SDF asset, rebuild WebGL, and run `npm run qa:visual`.
