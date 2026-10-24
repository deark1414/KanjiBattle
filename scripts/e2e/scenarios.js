const assert = require("node:assert/strict");
const allies = state => state.battle.units.filter(unit => unit.ally);
const groups = state => state.battle.plan?.groups || [];
const positions = state => allies(state).sort((a,b)=>a.slot-b.slot).map(({id,x,y})=>({id,x,y}));

module.exports = [
  {
    id:"support-recruitment", name:"新3体の縁候補・勝利累積・加入処理と保存", fixture:"standard",
    async run(game) {
      let state=await game.request("recruitSupport");
      for(const id of [27,28,29]) assert(state.owned.includes(id));
      await game.page.waitForTimeout(1500);
      await game.page.reload();
      await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500);
      state=await game.request("resume");
      for(const id of [27,28,29]) assert(state.owned.includes(id));
    }
  },
  {
    id:"support-abilities", name:"新能力の確定検証・再響回数と引寄せ失敗とHP維持", fixture:"support",
    async run(game) {
      await game.deploy();
      const state=await game.request("supportProbe");
      assert(state.battle.logs.some(log=>log.includes("新キャラ能力チェック完了")));
      assert(state.battle.logs.some(log=>log.includes("再響")));
      assert(state.battle.units.some(unit=>unit.ally && unit.originalId===29 && unit.transformed && unit.hp===31));
    }
  },
  {
    id:"support-battle", name:"鈴・糸・影の実戦と再戦復元", fixture:"support",
    async run(game) {
      let state=await game.deploy();
      const original=state.battle.originalIds, owned=state.owned;
      assert.deepEqual(original,[27,28,29,10,11,20]);
      await game.arrange([[3,6],[3,5],[2,5],[0,6],[5,6],[4,6]]);
      await game.screenshot("support-deployment");
      await game.start();
      const observed=new Set(); let transformed=false;
      state=await game.wait(current=>{
        for(const log of current.battle.logs || []) observed.add(log);
        transformed ||= current.battle.units.some(unit=>unit.ally && unit.originalId===29 && unit.transformed && unit.id!==29);
        return current.battle.result;
      },"Support battle timed out.",120000);
      const logs=[...observed].join("\n");
      assert(logs.includes("引き寄せた"),"Thread pull never resolved.");
      assert(logs.includes("変身"),"Shadow never transformed.");
      assert(transformed,"The copied form must retain shadow's original identity.");
      assert.deepEqual(state.owned,owned,"Transformation must not change permanent ownership.");
      await game.screenshot("support-result");
      await game.named("RetryStageButton");
      state=await game.wait(current=>current.battle.deploying,"Support retry did not open.");
      assert.deepEqual(state.battle.originalIds,original);
      assert(allies(state).every(unit=>!unit.transformed && unit.id===unit.originalId));
    }
  },
  {
    id:"fusion-gate", name:"融合解放前は隣接しても融合しない", fixture:"locked",
    async run(game) {
      const state=await game.deploy();
      assert(!state.fusionUnlocked);
      assert.equal(groups(state).length,0);
      assert.equal(state.battle.previewLinks,0);
      assert(!state.controls.some(control=>control.path.endsWith("/FusionSelection")));
      await game.named("CancelDeployment");
      const cleared=await game.request("gate20"); assert.equal(cleared.stagePoints,0); assert(!cleared.canUnlockFusion);
      assert((await game.request("fundFusion")).canUnlockFusion);
    }
  },
  {
    id:"formation-navigation", name:"離して個別運用・戦闘中の接近では融合しない", fixture:"standard",
    async run(game) {
      await game.deploy(); const state=await game.separate(); const original=state.battle.originalIds;
      assert.equal(groups(state).length,0);
      assert.equal(state.battle.previewLinks,0);
      await game.start();
      await game.wait(current=>{
        assert(allies(current).every(unit=>unit.id>0),"Units must not fuse during combat.");
        return current.battle.result;
      },"Non-fusion battle timed out.",120000);
      await game.named("RetryStageButton");
      const restored=await game.wait(current=>current.battle.deploying,"Retry did not restore deployment.");
      assert.deepEqual(restored.battle.originalIds,original);
      assert.equal(allies(restored).length,6); assert.equal(groups(restored).length,0);
    }
  },
  {
    id:"fusion-selection", name:"配置だけで融合予告・斜めと離隔では解除", fixture:"standard",
    async run(game) {
      await game.deploy(); await game.separate();
      let state=await game.move(1,1,5);
      assert.equal(groups(state).length,1); assert.equal(state.battle.previewLinks,1);
      assert(state.battle.fusionSummary.includes("？") && state.battle.fusionSummary.includes("2→1"));
      assert(!state.battle.fusionSummary.includes("双剣"));
      assert.equal(state.discovered.length,0); assert.equal(allies(state).length,6);
      assert(!state.controls.some(control=>control.path.endsWith("/FusionSelection")));
      await game.screenshot("adjacent-preview");
      await game.move(3,2,6);
      state=await game.move(1,1,6);
      assert.equal(groups(state).length,0); assert.equal(state.battle.previewLinks,0);
      state=await game.move(1,1,5); assert.equal(groups(state).length,1);
      state=await game.move(1,2,5); assert.equal(groups(state).length,0);
    }
  },
  {
    id:"fusion-cancel", name:"配置取消では発見・配置を保存しない", fixture:"standard",
    async run(game) {
      let state=await game.deploy(); const original=positions(state);
      await game.separate(); await game.named("CancelDeployment"); state=await game.deploy();
      assert.deepEqual(positions(state),original); assert.equal(state.discovered.length,0);
      assert.equal(allies(state).length,6);
    }
  },
  {
    id:"fusion-origin", name:"前側素材の位置に融合体を出現", fixture:"standard",
    async run(game) {
      await game.deploy();
      let state=await game.arrange([[0,6],[0,5],[4,5],[2,6],[3,5],[5,6]]);
      assert.equal(groups(state).length,1); assert.equal(groups(state)[0].origin,1);
      state=await game.start();
      const fusion=allies(state).find(unit=>unit.id===-1001);
      assert(fusion && fusion.x===0 && fusion.y===5);
      assert.equal(allies(state).length,5); assert.deepEqual(state.discovered,[-1001]);
      await game.screenshot("fusion-start");
    }
  },
  {
    id:"fusion-win-retry", name:"独立した二組を自動融合・勝利・再戦", fixture:"standard",
    async run(game) {
      await game.deploy(); let state=await game.adjacent(true);
      const original=state.battle.originalIds, owned=state.owned;
      assert.equal(groups(state).length,2); assert.equal(state.battle.conflicts,0);
      state=await game.start(); assert.equal(allies(state).length,4);
      assert(allies(state).some(unit=>unit.id===-1001) && allies(state).some(unit=>unit.id===-1002));
      state=await game.result(); assert(state.battle.won); assert.deepEqual(state.owned,owned);
      await game.screenshot("victory"); await game.named("RetryStageButton");
      state=await game.wait(current=>current.battle.deploying,"Retry did not restore deployment.");
      assert.deepEqual(state.battle.originalIds,original);
      assert.equal(allies(state).length,6); assert.equal(groups(state).length,2);
      assert(state.battle.fusionSummary.includes("双剣") && state.battle.fusionSummary.includes("散弾"));
    }
  },
  {
    id:"fusion-save-reload", name:"競合の選択・配置・発見を再読込で復元", fixture:"standard",
    async run(game) {
      await game.deploy(); let state=await game.competing();
      assert(state.battle.conflicts>0);
      await game.openFusion(); await game.choose(3,2); await game.closeFusion();
      state=await game.request(); const saved=positions(state); const expected=groups(state);
      assert.equal(expected.length,1); assert.equal(state.battle.conflicts,0);
      await game.start(); await game.page.waitForTimeout(1500);
      await game.page.reload();
      await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500); await game.request("resume");
      state=await game.deploy();
      assert.deepEqual(positions(state),saved); assert.deepEqual(groups(state),expected);
      assert.equal(state.battle.conflicts,0); assert.deepEqual(state.discovered,[-1001]);
      state=await game.start(); assert.equal(allies(state).length,5);
    }
  },
  {
    id:"fusion-corrupt-save", name:"競合を黙って決めず開戦を保留", fixture:"standard",
    async run(game) {
      await game.request("corruptPlan"); await game.deploy(); let state=await game.competing();
      assert.equal(groups(state).length,0); assert(state.battle.conflicts>0);
      assert(!state.controls.find(control=>control.path.endsWith("/ConfirmDeployment")).enabled);
      await game.openFusion();
      assert((await game.request()).controls.filter(control=>control.kind==="Toggle" && control.path.includes("FusionSelection/"))
        .every(control=>control.text.includes("？")));
      await game.choose(1,2);
      state=await game.request();
      assert(!state.controls.find(game.recipe(3,2)).enabled);
      await game.closeFusion(); state=await game.request();
      assert.equal(state.battle.conflicts,0); assert.equal(groups(state).length,2);
      await game.openFusion();
      await game.click(control=>control.text==="選び直す","競合の選び直し");
      await game.closeFusion(); state=await game.request(); assert(state.battle.conflicts>0);
      await game.openFusion(); await game.choose(3,2); await game.closeFusion();
      state=await game.start(); assert.equal(allies(state).length,5);
    }
  },
  {
    id:"fusion-party-change", name:"編成順が変われば旧競合選択を使わず再確認", fixture:"standard",
    async run(game) {
      await game.deploy(); await game.competing();
      await game.openFusion(); await game.choose(3,2); await game.closeFusion(); await game.start();
      await game.request("resume","alternate");
      let state=await game.deploy();
      assert(state.battle.conflicts>0);
      assert(!state.controls.find(control=>control.path.endsWith("/ConfirmDeployment")).enabled);
      state=await game.separate(); assert.equal(groups(state).length,0); assert.equal(state.battle.conflicts,0);
    }
  },
  {
    id:"fusion-defeat-retry", name:"融合体撃破後に分離せず敗北・素材復元", fixture:"defeat",
    async run(game) {
      const initial=await game.deploy(); await game.adjacent(); await game.start();
      const state=await game.result(); assert(!state.battle.won);
      assert.equal(allies(state).length,0); assert.deepEqual(state.owned,initial.owned);
      await game.named("RetryStageButton");
      const restored=await game.wait(current=>current.battle.deploying,"Defeat retry did not restore deployment.");
      assert.equal(allies(restored).length,6); assert.deepEqual(restored.battle.originalIds,initial.battle.originalIds);
    }
  },
  {
    id:"fusion-repeat", name:"隣接配置を連戦で再利用・敗北停止", fixture:"standard",
    async run(game) {
      await game.deploy(); await game.adjacent();
      const first=await game.start(); await game.named("AutoRepeatControl");
      await game.result(); await game.request("nextDefeat");
      let state=await game.wait(current=>current.battle.battleId!==first.battle.battleId && !current.battle.result,"Automatic retry did not start.");
      assert(!state.battle.deploying); assert.equal(groups(state).length,1);
      assert(allies(state).some(unit=>unit.id===-1001));
      state=await game.result(); assert(!state.battle.won);
      assert(!state.controls.find(control=>control.path.endsWith("/AutoRepeatControl"))?.selected);
      const stopped=state.battle.battleId; await game.page.waitForTimeout(4000);
      assert.equal((await game.request()).battle.battleId,stopped);
    }
  },
  ...[["mobile",{width:390,height:844}],["landscape",{width:844,height:390}]].map(([shape,viewport])=>({
    id:"fusion-"+shape, name:"隣接融合の予告と操作・"+shape, fixture:"standard", viewport,
    async run(game) {
      await game.deploy(); let state=await game.adjacent();
      assert.equal(state.battle.previewLinks,1); assert.equal(groups(state).length,1);
      await game.screenshot("fusion");
      state=await game.start(); assert(allies(state).some(unit=>unit.id===-1001));
      const canvas=await game.page.locator("#unity-canvas").boundingBox();
      assert(canvas.width>=viewport.width*.98 && canvas.height>=viewport.height*.98);
      assert(canvas.width<=viewport.width+1 && canvas.height<=viewport.height+1);
    }
  }))
];
