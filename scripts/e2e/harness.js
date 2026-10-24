const assert = require("node:assert/strict");
const path = require("node:path");
const fs = require("node:fs");

class GameHarness {
  constructor(page, output) {
    this.page = page;
    this.output = output;
    this.responses = new Map();
    this.chunks = new Map();
    this.errors = [];
    this.logs = [];
    this.sequence = 0;
    page.on("console", message => {
      const text = message.text();
      if (text.startsWith("KANJI_E2E_PART:")) {
        try {
          const [, id, index, count, payload] = text.match(/^KANJI_E2E_PART:([^:]+):(\d+):(\d+):([^\n]*)/);
          const chunks = this.chunks.get(id) || new Map();
          chunks.set(Number(index), payload); this.chunks.set(id, chunks);
          if (chunks.size === Number(count)) {
            const joined = Array.from({ length: Number(count) }, (_, index) => chunks.get(index)).join("");
            const response = JSON.parse(Buffer.from(joined, "base64").toString("utf8"));
            this.responses.set(response.requestId, response); this.chunks.delete(id);
          }
        } catch (error) {
          this.errors.push(`Invalid bridge response (${text.length} chars): ${error.message}`);
          this.logs.push(text);
        }
      } else {
        this.logs.push(text);
        if (message.type() === "error" || /(?:\w+Exception|RuntimeError|Aborted\()/.test(text)) this.errors.push(text);
      }
    });
    page.on("pageerror", error => this.errors.push(error.stack || error.message));
  }

  async boot(url) {
    await this.page.route("**/game/index.html*", async route => {
      try {
        const response = await route.fetch();
        const source = await response.text();
        const hook = ".then((unityInstance) => {";
        if (!source.includes(hook)) {
          this.errors.push("Unity loader hook is missing; check the E2E build and avoid rebuilding it during a test run.");
          await route.fulfill({ status: 503, contentType: "text/plain", body: this.errors.at(-1) });
          return;
        }
        await route.fulfill({ response, body: source.replace(hook, `${hook} window.unityInstance = unityInstance;`) });
      } catch (error) {
        this.errors.push(`Unity loader request failed: ${error.message}`);
        await route.abort().catch(() => {});
      }
    });
    await this.page.goto(url);
    assert.deepEqual(this.errors, [], "Unity loader could not be prepared.");
    await this.page.waitForFunction(() => !!window.unityInstance, null, { timeout: 120000 });
    // Unity's splash can still cover the first rendered frame after startup.
    await this.page.waitForTimeout(4500);
    const canvas = await this.page.locator("#unity-canvas").boundingBox();
    const viewport = this.page.viewportSize();
    assert(canvas && Math.abs(canvas.x) <= 1 && Math.abs(canvas.y) <= 1
      && canvas.width >= viewport.width * 0.98 && canvas.height >= viewport.height * 0.98
      && canvas.width <= viewport.width + 1 && canvas.height <= viewport.height + 1,
    "The test build stylesheet must load and the canvas must fill the viewport.");
    const state = await this.request("snapshot");
    assert.equal(state.protocol, 1, "An E2E build is required; normal builds have no test bridge.");
  }

  async request(action = "snapshot", value = "") {
    const id = String(++this.sequence);
    await this.page.evaluate(request => window.unityInstance.SendMessage("BattleE2E", "Command", JSON.stringify(request)), { id, action, value });
    const deadline = Date.now() + 10000;
    while (!this.responses.has(id) && Date.now() < deadline) await this.page.waitForTimeout(50);
    const response = this.responses.get(id);
    assert(response, `No test bridge response to ${action}; is this the correct build?\n${this.errors.join("\n")}`);
    this.responses.delete(id);
    assert(!response.error, response.error);
    this.state = response;
    return response;
  }

  async wait(predicate, message, timeout = 60000) {
    const deadline = Date.now() + timeout;
    while (Date.now() < deadline) {
      const state = await this.request();
      if (predicate(state)) return state;
      await this.page.waitForTimeout(150);
    }
    throw new Error(`${message}\nLast battle: ${JSON.stringify({ ...this.state.battle, cells: undefined })}`);
  }

  async click(predicate, description, { disabled = false } = {}) {
    let control;
    for (let attempt = 0; attempt < 12; attempt++) {
      const state = await this.request();
      const matches = state.controls.filter(predicate);
      control = matches.find(item => item.visible && item.reachable);
      if (control) break;
      assert(matches.length > 0, `Control not found: ${description}`);
      assert(!matches.some(item => item.visible && !item.reachable), `Control is covered by another UI element: ${description}`);
      const canvas = await this.page.locator("#unity-canvas").boundingBox();
      await this.page.mouse.move(canvas.x + canvas.width * 0.5, canvas.y + canvas.height * 0.5);
      await this.page.mouse.wheel(0, matches[0].rect.y > 0.5 ? 220 : -220);
      await this.page.waitForTimeout(200);
    }
    assert(control?.visible, `Control is clipped: ${description}`);
    assert(disabled || control.enabled, `Control is disabled: ${description}`);
    await this.clickBox(control.rect);
    await this.page.waitForTimeout(120);
    return this.request();
  }

  async clickBox(rect) {
    const canvas = await this.page.locator("#unity-canvas").boundingBox();
    await this.page.mouse.click(canvas.x + (rect.x + rect.w / 2) * canvas.width,
      canvas.y + (rect.y + rect.h / 2) * canvas.height);
  }

  async named(name, options) {
    return this.click(control => control.path.endsWith(`/${name}`), name, options);
  }

  async deploy() {
    await this.click(control => /FormationPrimaryActions/.test(control.path) && control.text === "出陣", "出陣");
    return this.wait(state => state.battle.deploying, "Deployment did not open.");
  }

  async openFusion() {
    await this.named("FusionSelection");
    return this.wait(state => state.battle.fusionOpen, "Fusion selection did not open.");
  }

  recipe(first, second) {
    return control => control.kind === "Toggle" && control.path.includes("FusionSelection/")
      && control.text.includes(`[${first}]`) && control.text.includes(`[${second}]`);
  }

  async choose(first = 1, second = 2) {
    return this.click(this.recipe(first, second), `Fusion materials ${first},${second}`);
  }

  async move(slot, x, y) {
    let state = await this.request();
    const unit = state.battle.units.find(unit => unit.ally && unit.slot === slot);
    assert(unit, `Material slot ${slot} is missing.`);
    if (unit.x === x && unit.y === y) return state;
    await this.clickBox(state.battle.cells.find(cell => cell.x === unit.x && cell.y === unit.y).rect);
    // Unity polls pointer events per frame; do not merge source and destination
    // clicks into the same frame on small, fast-rendering landscape canvases.
    await this.page.waitForTimeout(100);
    await this.clickBox(state.battle.cells.find(cell => cell.x === x && cell.y === y).rect);
    state = await this.request();
    const moved = state.battle.units.find(unit => unit.ally && unit.slot === slot);
    assert(moved.x === x && moved.y === y, `Material ${slot} did not move to ${x},${y}; actual ${moved.x},${moved.y}.`);
    return state;
  }

  async arrange(positions) {
    for (let slot = 0; slot < positions.length; slot++) await this.move(slot, ...positions[slot]);
    return this.request();
  }

  async separate() { return this.arrange([[0,5],[2,5],[4,5],[1,6],[3,6],[5,6]]); }
  async adjacent(both = false) {
    await this.separate(); await this.move(1,1,5);
    if (both) await this.move(5,4,6);
    return this.request();
  }
  async competing() { return this.arrange([[0,5],[1,5],[2,5],[3,5],[3,6],[5,6]]); }

  async closeFusion() { return this.click(control => control.text === "布陣に戻る", "布陣に戻る"); }
  async start() {
    await this.named("ConfirmDeployment");
    return this.wait(state => !state.battle.deploying, "Battle did not start.");
  }
  async result() { return this.wait(state => state.battle.result, "Battle result timed out.", 120000); }
  async screenshot(name) { await this.page.screenshot({ path: path.join(this.output, `${name}.png`) }); }
  writeArtifacts() {
    fs.writeFileSync(path.join(this.output, "console.log"), this.logs.join("\n"));
    fs.writeFileSync(path.join(this.output, "errors.json"), JSON.stringify(this.errors, null, 2));
    fs.writeFileSync(path.join(this.output, "state.json"), JSON.stringify(this.state, null, 2));
  }
}

module.exports = { GameHarness };
