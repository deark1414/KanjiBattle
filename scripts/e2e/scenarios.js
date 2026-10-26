const assert = require("node:assert/strict");
const allies = state => state.battle.units.filter(unit => unit.ally);
const groups = state => state.battle.plan?.groups || [];
const positions = state => allies(state).sort((a,b)=>a.slot-b.slot).map(({id,x,y})=>({id,x,y}));

module.exports = [
  ...[{width:390,height:844},{width:844,height:390}].map(viewport=>({
    id:'fusion-border-grass-'+viewport.width,name:'草の未発見融合でも盤面を黄色く覆わず操作できる '+viewport.width,fixture:'fusion-grass',viewport,
    async run(game){
      await game.deploy();assert.equal(groups(game.state).length,1);
      assert.equal(game.state.battle.fusionFrameCount,2);assert(game.state.battle.fusionFramesValid);
      assert.equal(game.state.battle.fusionGlowCount,0,'Remove the custom piece pulse and restore the original local glow');
      await game.screenshot('grass-border-only');
      await game.screenshot('grass-original-glow');
      await game.named('CancelDeployment');assert.equal(game.state.battle.fusionFrameCount,0);
      assert.equal(game.state.battle.fusionGlowCount,0);
      await game.deploy();assert.equal(game.state.battle.fusionFrameCount,2);assert(game.state.battle.fusionFramesValid);
      await game.start();assert.equal(game.state.battle.fusionFrameCount,0);
      assert.equal(game.state.battle.fusionGlowCount,0);
    }
  })),
  {
    id:'water-growth',name:'水の30%から60%への育成・実回復量・敵は30%のまま',fixture:'standard',viewport:{width:390,height:844},
    async run(game){
      await game.request('treeFresh');await game.request('treeAdvance','25');await game.request('treeFunds','500');
      for(const id of ['c20','c20','k3_4','k3_6'])await game.request('treePurchase',id);
      await game.page.waitForTimeout(700);await game.request();
      await game.named('TreeBranch_3');await game.named('TreeNode_c20');
      assert(['u20_4','u20_6','k3_4','k3_6'].every(id=>game.state.investmentOwned.includes(id)));
      assert(!game.state.treeOverflow);await game.screenshot('water-max-growth');
      await game.request('treeCampaignDefault','1');await game.deploy();await game.request('waterBalanceProbe','max');
      await game.screenshot('water-recovery-probe');
    }
  },
  {
    id:'battle-turn-limit',name:'水の基礎30%回復と20ターンでの引き分け・報酬なし',fixture:'standard',viewport:{width:390,height:844},
    async run(game){
      await game.request('treeFresh');await game.request('treeCampaignDefault','1');await game.deploy();
      await game.request('waterBalanceProbe');await game.request('stalemate');
      const points=game.state.stagePoints;
      await game.start();await game.result();
      assert(!game.state.battle.won);assert(game.state.battle.resultText.includes('20ターン経過'));
      const turns=game.state.battle.logs.filter(text=>text.includes('--- ターン'));
      assert(turns.includes('--- ターン 20 ---')&&!turns.includes('--- ターン 21 ---'));
      assert.equal(game.state.stagePoints,points);assert(game.state.battle.resultText.includes('今回の獲得なし'));
      assert(!game.state.battle.resultTextOverflow);await game.screenshot('turn-limit-result');
    }
  },
  ...[{width:390,height:844},{width:844,height:390}].map(viewport=>({
    id:'battle-pause-'+viewport.width,name:'戦闘の共通バー・ターン途中停止・行動途中の全滅判定 '+viewport.width,fixture:'animals',viewport,
    async run(game){
      await game.deploy();
      const settings=game.state.controls.find(c=>c.path.endsWith('/GlobalStatusBar/SettingsStatus'));
      assert(settings?.visible&&settings.reachable,'Common status bar is available during deployment');
      assert(settings.rect.y+settings.rect.h<game.state.battle.header.y,'Common and battle headers do not overlap');
      for(const cell of game.state.battle.cells)assert(cell.rect.y>=game.state.battle.header.y+game.state.battle.header.h,'Board stays below the headers');
      await game.start();await game.wait(s=>s.battle.logs.some(text=>text.includes('--- ターン')),'First turn not started');
      await game.named('PauseButton');assert(game.state.battle.paused);
      const units=game.state.battle.units,logs=game.state.battle.logs;
      await game.page.waitForTimeout(2000);await game.request();
      assert.deepEqual(game.state.battle.units,units,'No movement or HP changes while paused');
      assert.deepEqual(game.state.battle.logs,logs,'No further actions during a paused turn');
      await game.screenshot('paused-with-status');
      await game.request('defeatSide',viewport.width===390?'ally':'enemy');
      assert(!game.state.battle.result,'Pause also suspends the result transition');
      const afterDeath=game.state.battle.logs.length;
      await game.named('ResumeButton');
      await game.wait(s=>s.battle.result,'Elimination should end the battle before the remaining actors',5000);
      assert.equal(game.state.battle.won,viewport.width!==390);
      assert(game.state.battle.logs.slice(afterDeath).every(text=>/バトル再開|勝利|敗北|報酬|縁/.test(text)),'No remaining actors after elimination');
      await game.screenshot('immediate-result');
    }
  })),
  ...require("./tutorial-scenarios"),
  ...require("./castle-tree-scenarios"),
  ...require("./player-skill-scenarios"),
  ...require("./codex-navigation-scenarios"),
  ...require("./record-scenarios"),
  ...require("./support-vfx-scenarios"),
  ...require("./art-scenarios"),
  ...require("./advanced-scenarios"),
  ...["soldiers","forge"].flatMap(fixture=>[
    {
      id:`${fixture}-abilities`,name:`${fixture}: 隣接融合と支援効果・持続・対象制限`,fixture,
      async run(game) {
        let state=await game.deploy();
        assert.equal(groups(state).length,2); assert.equal(state.battle.conflicts,0);
        assert(!state.battle.fusionSummary.includes(fixture==="soldiers"?"戦士":"鍛冶"));
        await game.start();
        state=await game.request("fusionSupportProbe");
        assert(state.battle.logs.some(log=>log.includes("支援融合の能力チェック完了")));
        assert(state.discovered.includes(fixture==="soldiers"?-1003:-1005));
        await game.screenshot("support-fusion-abilities");
      }
    },
    {
      id:`${fixture}-battle`,name:`${fixture}: 支援融合の実戦・原編成復元`,fixture,
      viewport:fixture==="forge"?{width:390,height:844}:undefined,
      async run(game) {
        const initial=await game.deploy(); await game.start();
        const logs=new Set();
        const result=await game.wait(state=>{
          for(const log of state.battle.logs || []) logs.add(log);
          return state.battle.result;
        },"Support fusion battle timed out.",120000);
        assert([...logs].some(log=>log.includes(fixture==="soldiers"?"鼓舞":"鍛造")));
        assert(!result.battle.resultTextOverflow);
        await game.screenshot("support-fusion-result");
        await game.named("RetryStageButton");
        const state=await game.wait(current=>current.battle.deploying,"Support fusion retry did not open.");
        assert.deepEqual(state.battle.originalIds,initial.battle.originalIds);
        assert.equal(allies(state).length,6);
        assert(allies(state).every(unit=>unit.hp===unit.maxHP && unit.attackBuff===0));
        assert.equal(groups(state).length,2);
      }
    }
  ]),
  {
    id:"element-abilities",name:"電の連鎖減衰と上限・鉄3形態・風の有効期間",fixture:"elements",
    async run(game) {
      const initial=await game.deploy();
      assert.deepEqual(initial.battle.originalIds,[31,32,33,23,10,20]);
      const state=await game.request("elementProbe");
      assert(state.battle.logs.some(log=>log.includes("電風鉄の能力チェック完了")));
      await game.screenshot("element-abilities");
    }
  },
  ...[undefined,{width:390,height:844}].map((viewport,index)=>({
    id:index?"element-mobile":"element-battle",name:index?"電風鉄の実戦・縦長":"電風鉄の実戦・再戦復元",fixture:"elements",viewport,
    async run(game) {
      const initial=await game.deploy(); await game.start();
      const observed=new Set();
      await game.wait(state=>{
        for(const log of state.battle.logs || []) observed.add(log);
        return state.battle.result;
      },"Element battle timed out.",120000);
      assert([...observed].some(log=>log.includes("連鎖電撃")));
      assert([...observed].some(log=>log.includes("追風") || log.includes("向風")));
      await game.screenshot("element-result");
      await game.named("RetryStageButton");
      const state=await game.wait(current=>current.battle.deploying,"Element retry did not open.");
      assert.deepEqual(state.battle.originalIds,initial.battle.originalIds);
      assert(allies(state).every(unit=>unit.hp===unit.maxHP));
    }
  })),
  {
    id:"element-recruitment",name:"電風鉄のツリー加入・旧数字の除外・再読込",fixture:"standard",
    async run(game) {
      let state=await game.request("recruitElements");
      for(const id of [31,32,33]) assert(state.owned.includes(id));
      assert.equal(state.roster.length,26);
      assert(!state.roster.some(id=>id>=4 && id<=9));
      await game.page.waitForTimeout(1500); await game.page.reload();
      await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500); state=await game.request("resume");
      for(const id of [31,32,33]) assert(state.owned.includes(id));
    }
  },
  ...[
    {id:"animals-dead",name:"丑が撃破されても勝利経験値1.5倍・再戦で復元",fixture:"animals",bonus:1.5,killCow:true},
    {id:"animals-double",name:"丑2体の経験値倍率は重複しない",fixture:"animals-double",bonus:1.5},
    {id:"animals-enemy",name:"所持だけ・敵側の丑では経験値が増えない",fixture:"animals-enemy",bonus:1},
    {id:"animals-mobile",name:"干支の編成と経験値補正リザルト・縦長",fixture:"animals",bonus:1.5,viewport:{width:390,height:844}}
  ].map(options=>({
    ...options,
    async run(game) {
      const initial=await game.deploy();
      assert(allies(initial).every(unit=>unit.level===initial.level),"New animals use the shared player level.");
      for(const [id,name] of [[23,"午"],[24,"酉"],[25,"寅"]])
        for(const unit of allies(initial).filter(unit=>unit.id===id)) assert.equal(unit.name,name);
      if(options.fixture==="animals-enemy") {
        assert(initial.owned.includes(30)); assert(!initial.formation.includes(30));
      }
      await game.separate(); await game.start();
      if(options.killCow) {
        const state=await game.request("defeatCow");
        assert(!allies(state).some(unit=>unit.id===30));
        assert(state.battle.originalIds.includes(30));
      }
      let state=await game.result();
      assert(state.battle.won); assert.equal(state.level,initial.level);
      assert.equal(state.experience-initial.experience,Math.ceil(initial.baseBattleExperience*options.bonus));
      assert.equal(state.stagePoints-initial.stagePoints,8,"Cow must not multiply stage points.");
      assert.equal(state.battle.resultText.includes("経験値補正"),options.bonus>1);
      assert(!state.battle.resultTextOverflow,"Result text must fit the modal.");
      await game.screenshot("animal-victory");
      if(options.killCow) {
        await game.named("RetryStageButton");
        state=await game.wait(current=>current.battle.deploying,"Animal retry did not open.");
        assert.deepEqual(state.battle.originalIds,initial.battle.originalIds);
        const cow=allies(state).find(unit=>unit.id===30);
        assert(cow && cow.hp===cow.maxHP); assert.equal(allies(state).length,6);
      }
    }
  })),
  {
    id:"animals-defeat",name:"丑を出撃させても敗北時に経験値・戦果を得ない",fixture:"animals-defeat",
    async run(game) {
      const initial=await game.deploy(); await game.start();
      const state=await game.result();
      assert(!state.battle.won); assert.equal(state.experience,initial.experience);
      assert.equal(state.stagePoints,initial.stagePoints);
      assert(!state.battle.resultText.includes("経験値補正"));
    }
  },
  {
    id:"animals-recruitment",name:"丑のツリー加入と保存",fixture:"standard",
    async run(game) {
      let state=await game.request("recruitCow");
      assert(state.owned.includes(30)); assert.equal(state.experience,0);
      await game.page.waitForTimeout(1500); await game.page.reload();
      await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500); state=await game.request("resume");
      assert(state.owned.includes(30)); assert.equal(state.experience,0);
    }
  },
  {
    id:"defense-abilities", name:"盾軽減・甲の肩代わり・連鎖禁止・肩代わり撃破", fixture:"defense",
    async run(game) {
      const initial=await game.deploy();
      assert.deepEqual(initial.battle.originalIds,[14,13,15,14,17,20]);
      assert(initial.battle.units.some(unit=>unit.id===14 && unit.name==="甲"));
      assert(initial.battle.units.some(unit=>unit.id===17 && unit.name==="弓"));
      const state=await game.request("defenseProbe");
      assert(state.battle.logs.some(log=>log.includes("防御能力チェック完了")));
      assert(state.battle.logs.some(log=>log.includes("かばった")));
      assert.equal(allies(state).length,5);
      await game.screenshot("guard-resolution");
    }
  },
  {
    id:"defense-battle", name:"防御編成の実戦と再戦での原編成復元", fixture:"defense",
    async run(game) {
      let state=await game.deploy(); const original=state.battle.originalIds,owned=state.owned;
      await game.arrange([[2,6],[2,5],[3,5],[3,6],[1,6],[4,6]]);
      await game.screenshot("defense-deployment");
      await game.start();
      const logs=new Set();
      state=await game.wait(current=>{
        for(const log of current.battle.logs || []) logs.add(log);
        return current.battle.result;
      },"Defense battle timed out.",120000);
      assert([...logs].some(log=>log.includes("かばった")),"No protection occurred in actual combat.");
      assert.deepEqual(state.owned,owned);
      await game.named("RetryStageButton");
      state=await game.wait(current=>current.battle.deploying,"Defense retry did not open.");
      assert.deepEqual(state.battle.originalIds,original);
      assert.equal(allies(state).length,6);
    }
  },
  {
    id:"support-recruitment", name:"鈴糸影のツリー加入・旧縁廃止・保存", fixture:"standard",
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
      assert.equal(state.battle.fusionFrameCount,2);assert(state.battle.fusionFramesValid,'Original low-opacity highlights must stay inside their cells and leave input unblocked');
      assert.equal(state.battle.fusionGlowCount,0);
      await game.screenshot("fusion");
      await game.page.setViewportSize({width:viewport.height,height:viewport.width});await game.page.waitForTimeout(800);state=await game.request();
      assert.equal(state.battle.fusionFrameCount,2);assert(state.battle.fusionFramesValid,'Original glow stays inside cells after rotating the screen');
      assert.equal(state.battle.fusionGlowCount,0);
      await game.screenshot('fusion-rotated');
      await game.page.setViewportSize(viewport);await game.page.waitForTimeout(800);await game.request();
      state=await game.start(); assert(allies(state).some(unit=>unit.id===-1001));
      const canvas=await game.page.locator("#unity-canvas").boundingBox();
      assert(canvas.width>=viewport.width*.98 && canvas.height>=viewport.height*.98);
      assert(canvas.width<=viewport.width+1 && canvas.height<=viewport.height+1);
    }
  }))
];
