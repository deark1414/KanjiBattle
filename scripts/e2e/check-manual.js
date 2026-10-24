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
    await page.locator('#preset').selectOption('animals'); await page.locator('#reset').click(); await ready();
    assert.deepEqual((await state()).formation, [30,23,24,25,10,20]);
    await page.goto(base); await ready();
    assert.equal(await page.locator('#preset').inputValue(), 'animals');
    await page.locator('#preset').selectOption('elements'); await page.locator('#reset').click(); await ready();
    assert.deepEqual((await state()).formation, [31,32,33,23,10,20]);
    await page.goto(base); await ready();
    assert.equal(await page.locator('#preset').inputValue(), 'elements');
    for (const [preset,ids] of [['soldiers',[10,13,11,13,1,20]],['forge',[19,12,2,10,20,14]]]) {
      await page.locator('#preset').selectOption(preset); await page.locator('#reset').click(); await ready();
      assert.deepEqual((await state()).formation, ids);
      await click(control => control.path.endsWith('/DeployButton'));
      assert.equal((await state()).battle.plan.groups.length, 2);
      assert.equal((await state()).battle.conflicts, 0);
      await click(control => control.path.endsWith('/CancelDeployment'));
      await page.goto(base); await ready();
      assert.equal(await page.locator('#preset').inputValue(), preset);
    }
    for (const key of ['wave','magnet','mountain','coal','fortress','dice','rain','sand','grass','cocoon','windchime','lantern']) {
      await page.locator('#preset').selectOption('fusion-'+key); await page.locator('#reset').click(); await ready();
      await click(control => control.path.endsWith('/DeployButton'));
      const current=await state();
      assert.equal(current.battle.plan.groups.length,1);
      assert.equal(current.battle.plan.groups[0].members.length,key==='dice'?3:2);
      assert.equal(current.battle.conflicts,0);
      await click(control => control.path.endsWith('/CancelDeployment'));
    }
    await page.goto(base); await ready();
    assert.equal(await page.locator('#preset').inputValue(),'fusion-lantern');
    await page.setViewportSize({ width: 390, height: 844 }); await page.waitForTimeout(500);
    const frame = await page.locator('#game').boundingBox();
    const canvas = await page.frameLocator('#game').locator('#unity-canvas').boundingBox();
    assert(canvas.width >= frame.width * 0.98 && canvas.height >= frame.height * 0.98);
    assert(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth));
    assert.deepEqual(errors, []);
    await page.screenshot({ path: 'tmp/e2e/manual-fusions-mobile.png' });
    console.log('Passed: fixture initialization, fusion/element/animal/support/defense/soldier/forge presets, preserve/reset, reload, mobile layout.');
    await context.close();
  } finally { await browser.close(); }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
