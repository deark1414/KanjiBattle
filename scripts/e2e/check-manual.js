const assert = require("node:assert/strict");
const { chromium } = require("playwright");

async function main() {
  const browser = await chromium.launch();
  try {
    const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
    const page = await context.newPage();
    const errors = [];
    page.on("pageerror", error => errors.push(error.message));
    const base = process.env.KANJI_MANUAL_URL || "http://127.0.0.1:8796/";
    async function ready() {
      await page.waitForFunction(() => !['起動中','準備中'].includes(document.querySelector('#status').textContent), null, { timeout: 120000 });
      assert((await page.locator('#status').textContent()).startsWith('準備完了'), await page.locator('#status').textContent());
    }
    async function state() { return page.evaluate(() => command('snapshot')); }
    async function click(predicate) {
      const control = (await state()).controls.find(predicate);
      assert(control?.enabled && control.visible && control.reachable);
      const canvas = await page.frameLocator('#game').locator('#unity-canvas').boundingBox();
      await page.mouse.click(canvas.x + (control.rect.x + control.rect.w / 2) * canvas.width,
        canvas.y + (control.rect.y + control.rect.h / 2) * canvas.height);
      await page.waitForTimeout(200);
    }
    await page.goto(base + '?preset=standard'); await ready();
    assert((await state()).fusionUnlocked); assert.equal((await state()).formation.length, 6);
    await click(control => control.path.endsWith('/DeployButton'));
    assert.equal((await state()).battle.plan.groups.length, 1);
    assert.equal((await state()).battle.previewLinks, 1);
    assert(!((await state()).controls.some(control => control.path.endsWith('/FusionSelection'))));
    await click(control => control.path.endsWith('/ConfirmDeployment'));
    assert((await state()).battle.units.some(unit => unit.ally && unit.id === -1001));
    await page.locator('#formation').click(); await ready();
    assert.deepEqual((await state()).discovered, [-1001]);
    await page.locator('#preset').selectOption('locked'); await page.locator('#reset').click(); await ready();
    assert(!(await state()).fusionUnlocked);
    await page.goto(base); await ready(); assert(!(await state()).fusionUnlocked);
    await page.goto(base + '?preset=standard'); await ready();
    assert.deepEqual((await state()).discovered, []);
    await page.locator('#preset').selectOption('support'); await page.locator('#reset').click(); await ready();
    assert.deepEqual((await state()).formation, [27,28,29,10,11,20]);
    await page.goto(base); await ready();
    assert.equal(await page.locator('#preset').inputValue(), 'support');
    assert.deepEqual((await state()).formation, [27,28,29,10,11,20]);
    await page.locator('#preset').selectOption('defense'); await page.locator('#reset').click(); await ready();
    assert.deepEqual((await state()).formation, [14,13,15,14,17,20]);
    await page.goto(base); await ready();
    assert.equal(await page.locator('#preset').inputValue(), 'defense');
    await page.setViewportSize({ width: 390, height: 844 }); await page.waitForTimeout(500);
    const frame = await page.locator('#game').boundingBox();
    const canvas = await page.frameLocator('#game').locator('#unity-canvas').boundingBox();
    assert(canvas.width >= frame.width * 0.98 && canvas.height >= frame.height * 0.98);
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    assert.deepEqual(errors, []);
    await page.screenshot({ path: 'tmp/e2e/manual-defense-mobile.png' });
    console.log('Passed: fixture initialization, real fusion clicks, support/defense presets, preserve/reset, reload, mobile layout.');
    await context.close();
  } finally { await browser.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
