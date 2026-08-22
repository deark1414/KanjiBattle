# Visual QA

This project uses a small Playwright smoke check for the WebGL output under `Docs`.

## Setup

```bash
npm install
npm run install:browsers
```

## Run

```bash
npm run qa:visual
```

The script starts a local static server for `Docs`, opens Playwright-managed Chromium, and captures:

- `tmp/playwright-screenshots/mobile-game.png`
- `tmp/playwright-screenshots/desktop-game.png`
- `tmp/playwright-screenshots/mobile-embedded.png`
- `tmp/playwright-screenshots/desktop-embedded.png`
- `tmp/playwright-screenshots/report.json`

Unity canvas clicks are based on the canvas bounding box, not viewport coordinates. This keeps desktop checks stable when the canvas is centered inside the page.

The desktop direct-game check performs a light stage-screen flow. The mobile direct-game check currently captures the loaded top screen because headless input can differ from real browser input for Unity WebGL. Use it to catch canvas sizing, top layout, font, and bottom-tab visibility regressions.

The script fails if the direct-game canvas is still loading, unexpectedly small, horizontally overflowing, or outside the viewport. Screenshots remain the final visual review layer for detailed UI overlap.

## Notes

- Run this after a fresh WebGL build when checking deployment-bound changes.
- Browser launch may need to run outside the Codex sandbox on macOS.
- The battle smoke flow is intentionally light: it only confirms that the battle tab, stage select, formation, and battle layout can be reached and screenshotted.
