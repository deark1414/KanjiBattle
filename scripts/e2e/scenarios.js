const assert = require("node:assert/strict");
const allies = state => state.battle.units.filter(unit => unit.ally);
const groups = state => state.battle.plan?.groups || [];
const sameOriginals = (state, original) => assert.deepEqual(state.battle.originalIds, original);

module.exports = [
  {
    id: "fusion-gate", name: "融合施設の局数・戦果条件", fixture: "locked",
    async run(game) {
      let state = await game.request();
      assert(!state.fusionUnlocked && !state.canUnlockFusion);
      await game.deploy();
      assert(!(await game.request()).controls.some(control => control.path.endsWith("/FusionSelection")));
      await game.named("CancelDeployment");
      state = await game.request("gate20");
      assert.equal(state.stagePoints, 0); assert(!state.canUnlockFusion);
      state = await game.request("fundFusion");
      assert.equal(state.stagePoints, 10); assert(state.canUnlockFusion);
    }
  },
  {
    id: "formation-navigation", name: "編成・出陣・布陣から戻る", fixture: "standard",
    async run(game) {
      const original = (await game.request()).formation;
      assert.equal(original.length, 6); assert(original.every(id => id > 0));
      let state = await game.deploy();
      assert.equal(allies(state).length, 6); sameOriginals(state, original);
      await game.named("CancelDeployment");
      state = await game.request(); assert.deepEqual(state.formation, original);
      await game.deploy();
      assert.equal(allies(await game.request()).length, 6);
      state = await game.start(); assert.equal(allies(state).length, 6); assert.equal(groups(state).length, 0);
      state = await game.result(); assert(state.battle.won);
      await game.named("RetryStageButton");
      state = await game.wait(state => state.battle.deploying, "Non-fusion retry did not restore deployment.");
      assert.equal(allies(state).length, 6); sameOriginals(state, original);
    }
  },
  {
    id: "fusion-selection", name: "未発見・素材重複禁止・解除・複数組", fixture: "standard",
    async run(game) {
      await game.deploy(); let state = await game.openFusion();
      assert.equal(state.discovered.length, 0);
      assert(state.controls.filter(control => control.kind === "Toggle" && control.path.includes("FusionSelection/"))
        .every(control => control.text.includes("？") && !/双剣|散弾/.test(control.text)));
      state = await game.choose(); assert.equal(groups(state).length, 1);
      const conflict = state.controls.find(game.recipe(1, 4)); assert(conflict && !conflict.enabled);
      await game.click(game.recipe(1, 4), "重複素材の融合", { disabled: true });
      assert.equal(groups(await game.request()).length, 1);
      state = await game.choose(3, 4); assert.equal(groups(state).length, 2);
      assert.equal(state.discovered.length, 0); assert.equal(allies(state).length, 6);
      state = await game.choose(); assert.equal(groups(state).length, 1);
      state = await game.click(control => control.text === "全解除" && control.path.includes("FusionSelection/"), "融合全解除");
      assert.equal(groups(state).length, 0);
      await game.screenshot("selection");
    }
  },
  {
    id: "fusion-cancel", name: "出陣取消では融合・発見・保存しない", fixture: "standard",
    async run(game) {
      await game.deploy(); await game.openFusion(); await game.choose(); await game.closeFusion();
      await game.named("CancelDeployment"); await game.deploy();
      const state = await game.request();
      assert.equal(groups(state).length, 0); assert.equal(state.discovered.length, 0); assert.equal(allies(state).length, 6);
    }
  },
  {
    id: "fusion-origin", name: "素材位置の指定と移動・開戦時の融合", fixture: "standard",
    async run(game) {
      let state = await game.deploy(); const original = state.battle.originalIds;
      await game.openFusion(); state = await game.choose();
      await game.click(control => control.kind === "Button" && control.text.includes("位置") && control.text.includes("[1]"), "融合出現元を変更");
      state = await game.closeFusion(); assert.equal(groups(state)[0].origin, 1);
      const unit = allies(state)[1];
      await game.clickBox(state.battle.cells.find(cell => cell.x === unit.x && cell.y === unit.y).rect);
      await game.clickBox(state.battle.cells.find(cell => cell.x === 0 && cell.y === 6).rect);
      state = await game.start();
      const fusion = allies(state).find(unit => unit.id === -1001);
      assert(fusion); assert.equal(fusion.x, 0); assert.equal(fusion.y, 6);
      assert.equal(allies(state).length, 5); sameOriginals(state, original);
      assert.deepEqual(state.discovered, [-1001]);
      await game.screenshot("fusion-start");
    }
  },
  {
    id: "fusion-win-retry", name: "二種同時融合・勝利・再戦で素材に復元", fixture: "standard",
    async run(game) {
      let state = await game.deploy(); const original = state.battle.originalIds; const owned = state.owned;
      await game.openFusion(); await game.choose(); await game.choose(5, 6); await game.closeFusion();
      state = await game.start();
      assert.equal(allies(state).length, 4);
      assert(allies(state).some(unit => unit.id === -1001) && allies(state).some(unit => unit.id === -1002));
      state = await game.result(); assert(state.battle.won); assert.deepEqual(state.owned, owned);
      await game.screenshot("victory"); await game.named("RetryStageButton");
      state = await game.wait(state => state.battle.deploying, "Retry did not restore deployment.");
      assert.equal(allies(state).length, 6); assert(allies(state).every(unit => unit.id > 0));
      sameOriginals(state, original); assert.equal(groups(state).length, 2);
      state = await game.openFusion();
      assert(state.controls.some(control => game.recipe(1, 2)(control) && control.text.includes("双剣")));
      assert(state.controls.some(control => game.recipe(5, 6)(control) && control.text.includes("散弾")));
    }
  },
  {
    id: "fusion-save-reload", name: "ページ再読込で融合・発見・配置を復元", fixture: "standard",
    async run(game) {
      let state = await game.deploy();
      const source = allies(state)[0];
      await game.clickBox(state.battle.cells.find(cell => cell.x === source.x && cell.y === source.y).rect);
      await game.clickBox(state.battle.cells.find(cell => cell.x === 0 && cell.y === 6).rect);
      state = await game.request();
      const positions = allies(state).map(({ id, x, y }) => ({ id, x, y }));
      assert.equal(positions[0].x, 0); assert.equal(positions[0].y, 6);
      await game.openFusion(); await game.choose(); await game.closeFusion();
      const started = await game.start(); const expected = groups(started); const owned = started.owned;
      await game.page.waitForTimeout(1500);
      await game.page.reload();
      await game.page.waitForFunction(() => !!window.unityInstance, null, { timeout: 120000 });
      await game.page.waitForTimeout(4500);
      state = await game.request("resume");
      assert(state.fusionUnlocked); assert.deepEqual(state.owned, owned); assert.deepEqual(state.discovered, [-1001]);
      state = await game.deploy(); assert.deepEqual(groups(state), expected); assert.equal(allies(state).length, 6);
      assert.deepEqual(allies(state).map(({ id, x, y }) => ({ id, x, y })), positions);
      state = await game.start(); assert(allies(state).some(unit => unit.id === -1001));
    }
  },
  {
    id: "fusion-corrupt-save", name: "不正な保存を別素材に振り替えない", fixture: "standard",
    async run(game) {
      await game.request("corruptPlan");
      let state = await game.deploy(); assert.equal(groups(state).length, 0);
      state = await game.start(); assert.equal(allies(state).length, 6); assert.equal(state.discovered.length, 0);
    }
  },
  {
    id: "fusion-party-change", name: "編成順変更で保存済み素材を流用しない", fixture: "standard",
    async run(game) {
      await game.deploy(); await game.openFusion(); await game.choose(); await game.closeFusion(); await game.start();
      await game.request("resume", "alternate");
      const state = await game.deploy(); assert.equal(groups(state).length, 0); assert.equal(allies(state).length, 6);
    }
  },
  {
    id: "fusion-defeat-retry", name: "融合撃破で分離せず敗北後に素材を復元", fixture: "defeat",
    async run(game) {
      const initial = await game.deploy(); await game.openFusion(); await game.choose(); await game.closeFusion();
      await game.start(); const state = await game.result();
      assert(!state.battle.won); assert.equal(allies(state).length, 0); assert.deepEqual(state.owned, initial.owned);
      await game.named("RetryStageButton");
      const restored = await game.wait(state => state.battle.deploying, "Defeat retry did not restore deployment.");
      sameOriginals(restored, initial.battle.originalIds); assert.equal(allies(restored).length, 6);
    }
  },
  {
    id: "fusion-repeat", name: "勝利後の自動連戦・融合引継ぎ・敗北停止", fixture: "standard",
    async run(game) {
      await game.deploy(); await game.openFusion(); await game.choose(); await game.closeFusion();
      const first = await game.start();
      const repeating = await game.named("AutoRepeatControl");
      assert(repeating.controls.find(control => control.path.endsWith("/AutoRepeatControl"))?.selected);
      await game.result(); await game.request("nextDefeat");
      let state = await game.wait(state => state.battle.battleId !== first.battle.battleId && !state.battle.result, "Automatic retry did not start.");
      assert(!state.battle.deploying); assert.equal(groups(state).length, 1); sameOriginals(state, first.battle.originalIds);
      assert(allies(state).some(unit => unit.id === -1001));
      state = await game.result(); assert(!state.battle.won);
      assert(!state.controls.find(control => control.path.endsWith("/AutoRepeatControl"))?.selected);
      const stopped = state.battle.battleId; await game.page.waitForTimeout(4000);
      assert.equal((await game.request()).battle.battleId, stopped);
    }
  },
  ...[
    ["mobile", { width: 390, height: 844 }],
    ["landscape", { width: 844, height: 390 }]
  ].map(([shape, viewport]) => ({
    id: `fusion-${shape}`, name: `融合操作・${shape === "mobile" ? "縦長" : "横長"}表示`, fixture: "standard", viewport,
    async run(game) {
      await game.deploy(); await game.openFusion(); let state = await game.choose(); assert.equal(groups(state).length, 1);
      await game.screenshot("fusion");
      state = await game.closeFusion(); assert(!state.battle.fusionOpen);
      state = await game.start(); assert(allies(state).some(unit => unit.id === -1001));
      const canvas = await game.page.locator("#unity-canvas").boundingBox();
      assert(canvas.width <= viewport.width + 1 && canvas.height <= viewport.height + 1);
      assert(canvas.width >= viewport.width * 0.98 && canvas.height >= viewport.height * 0.98,
        "The game canvas must fill the viewport, not merely fit inside it.");
    }
  }))
];
