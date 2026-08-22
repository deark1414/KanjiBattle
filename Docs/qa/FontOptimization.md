# Font Optimization

Current risk: `Assets/Fonts/NotoSansJP-Medium SDF.asset` contains an 8192x8192 TMP atlas and is much larger than the source font. This directly increases WebGL `game.data` size and mobile load time.

## Audit

```bash
npm run font:glyphs
```

This writes:

- `tmp/font/glyphs.txt`
- `tmp/font/glyphs-report.json`

Use the collected glyph list as a starting point when regenerating a smaller TMP font asset in Unity.

## Recommended Safe Path

1. Keep the original `NotoSansJP-Medium.ttf`.
2. Regenerate `NotoSansJP-Medium SDF.asset` in Unity with a smaller atlas, using `tmp/font/glyphs.txt` plus any planned future Japanese text.
3. Prefer a static atlas for release if all in-game text is known.
4. Verify Japanese text in Top, Battle, Formation, Facilities, summon category tabs, battle log, and result panels.
5. Rebuild WebGL and compare `Docs/game/Build/game.data`.

Do not manually edit the serialized SDF asset. Missing glyphs are easy to introduce and hard to notice without visual checks.
