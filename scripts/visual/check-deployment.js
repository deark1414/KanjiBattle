const assert = require("node:assert/strict");
const fs = require("node:fs");
const path = require("node:path");
const { chromium } = require("playwright");
const { PNG } = require(path.join(path.dirname(require.resolve("playwright-core/package.json")), "lib/utilsBundle.js"));

const base = process.env.KANJI_DEPLOYMENT_URL || "http://127.0.0.1:8795/game/index.html";
const output = path.resolve(__dirname, "../../tmp/deployment-preview/qa");

function allyCenter(buffer, bounds = { left: 470, right: 810, top: 100, bottom: 490 }) {
  const png = PNG.sync.read(buffer);
  let xSum = 0, ySum = 0, count = 0;
  for (let y = bounds.top; y < bounds.bottom; y++)
  for (let x = bounds.left; x < bounds.right; x++) {
    const index = (y * png.width + x) * 4;
    const [r, g, b] = png.data.subarray(index, index + 3);
    if (b > r + 30 && g > r + 20 && b > g * 0.9) {
      xSum += x; ySum += y; count++;
    }
  }
  assert(count > 100, "The ally piece must be visible on the board.");
  return { x: xSum / count, y: ySum / count, count };
}

async function boot(page) {
  // Expose only the test page's Unity instance; do not add a production debug API.
  await page.route("**/game/index.html", async route => {
    const response = await route.fetch();
    const body = (await response.text()).replace(
      ".then((unityInstance) => {",
      ".then((unityInstance) => { window.unityInstance = unityInstance;"
    );
    await route.fulfill({ response, body });
  });
  await page.goto(base);
  await page.waitForFunction(() => !!window.unityInstance, null, { timeout: 120000 });
  await page.waitForTimeout(4500);
  await page.evaluate(() => window.unityInstance.SendMessage("UIManager", "ShowStageSelect"));
  await page.waitForTimeout(500);
  await page.mouse.click(640, 230);
  await page.waitForTimeout(500);
  await page.mouse.click(520, 230);
  await page.waitForTimeout(600);
}

async function main() {
  fs.mkdirSync(output, { recursive: true });
  const browser = await chromium.launch({ headless: true });
  const errors = [];
  const report = { checks: [], errors };
  try {
    const context = await browser.newContext({ viewport: { width: 1280, height: 800 } });
    const page = await context.newPage();
    page.on("pageerror", error => errors.push(error.message));
    page.on("console", message => {
      if (/NullReferenceException|ArgumentException|IndexOutOfRangeException|RuntimeError/.test(message.text()))
        errors.push(message.text());
    });
    await boot(page);
    const initial = allyCenter(await page.screenshot({ path: path.join(output, "deployment-desktop.png") }));
    assert(initial.y > 370, "An ally must start in the bottom deployment zone.");
    report.checks.push("bottom deployment zone visible");

    await page.waitForTimeout(1800);
    const waiting = allyCenter(await page.screenshot());
    assert(Math.abs(waiting.x - initial.x) < 2 && Math.abs(waiting.y - initial.y) < 2,
      "The battle started before deployment was confirmed.");
    report.checks.push("battle waits for confirmation");

    await page.mouse.click(initial.x, initial.y);
    await page.mouse.click(778, 453);
    await page.waitForTimeout(150);
    const moved = allyCenter(await page.screenshot({ path: path.join(output, "deployment-moved.png") }));
    assert(moved.x > initial.x + 100 && moved.y > initial.y + 30, "A piece did not move to the selected empty cell.");
    report.checks.push("pointer input moves an ally");

    await page.mouse.click(moved.x, moved.y);
    await page.mouse.click(614, 185);
    await page.mouse.click(moved.x, moved.y);
    await page.waitForTimeout(100);
    const rejected = allyCenter(await page.screenshot());
    assert(Math.abs(rejected.x - moved.x) < 2 && Math.abs(rejected.y - moved.y) < 2,
      "Deployment allowed entering the enemy zone.");
    report.checks.push("enemy-zone placement rejected");

    await page.mouse.click(920, 665);
    await page.waitForTimeout(2000);
    const fighting = allyCenter(await page.screenshot({ path: path.join(output, "battle-desktop.png") }));
    assert(Math.abs(fighting.x - moved.x) > 3 || Math.abs(fighting.y - moved.y) > 3, "The battle did not begin after confirmation.");
    report.checks.push("confirmation starts movement and combat");

    await page.evaluate(() => window.unityInstance.SendMessage("UIManager", "ShowFormation"));
    await page.waitForTimeout(300);
    await page.mouse.click(520, 230);
    await page.waitForTimeout(500);
    const restored = allyCenter(await page.screenshot({ path: path.join(output, "deployment-restored.png") }));
    assert(Math.abs(restored.x - moved.x) < 2 && Math.abs(restored.y - moved.y) < 2,
      "The confirmed layout was not restored.");
    report.checks.push("confirmed layout restored on next battle");

    for (const [name, viewport] of [
      ["mobile", { width: 390, height: 844 }],
      ["landscape", { width: 844, height: 390 }]
    ]) {
      await page.setViewportSize(viewport);
      await page.waitForTimeout(600);
      const screenshot = await page.screenshot({ path: path.join(output, `deployment-${name}.png`) });
      if (name === "mobile") {
        const ally = allyCenter(screenshot, { left: 10, right: 380, top: 100, bottom: 520 });
        assert(ally.count > 450, "The ally piece did not scale with the portrait board.");
        report.checks.push("portrait ally piece scales with board cells");
      }
      const size = await page.locator("#unity-canvas").boundingBox();
      assert(size.width <= viewport.width + 1 && size.height <= viewport.height + 1, "Canvas overflow after resizing.");
      report.checks.push(`${name} canvas stays in viewport`);
    }
    assert.equal(errors.length, 0, "Runtime errors were detected.");
    console.log(JSON.stringify(report, null, 2));
  } finally {
    fs.writeFileSync(path.join(output, "report.json"), JSON.stringify(report, null, 2));
    await browser.close();
  }
}

main().catch(error => { console.error(error); process.exitCode = 1; });
