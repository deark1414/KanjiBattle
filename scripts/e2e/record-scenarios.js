const assert = require('node:assert/strict');
module.exports = [
  ...[{width:1280,height:800},{width:390,height:844},{width:844,height:390}].map(viewport => ({
    id:`records-${viewport.width}`,name:`成績の実HP集計・勝利保存・表示 ${viewport.width}`,fixture:'animals',viewport,
    async run(game) {
      await game.deploy();
      let state = await game.request('recordsProbe');
      assert(state.records.some(row=>row.wins===1&&row.damage===60));
      assert(state.records.some(row=>row.healed===18));
      assert.equal(state.records.find(row=>row.id===1).wins,0,'undeployed starter does not gain records');
      await game.named('OpenBattleRecords');
      assert(!game.state.codexOverflow,'visible records fit their rows: '+game.state.codexOverflowNames);
      await game.screenshot('battle-records');
      await game.named('CloseCodex');
      await game.click(control=>control.text==='戻る','戻る');
      await game.click(control=>control.text==='軍勢','軍勢');
      await game.named('OpenCodex');
      await game.screenshot('codex-normal');
      assert(game.state.codexTitles.includes('丑:加入済'));
      assert(game.state.codexTitles.includes('？:未遭遇'));
      await game.named('FusionTab');
      await game.screenshot('codex-hidden-fusion');
      assert(game.state.codexTitles.filter(title=>title==='？:未遭遇').length>=17);
      if(viewport.width===390){
        await game.page.setViewportSize({width:844,height:390}); await game.page.waitForTimeout(800);
        state=await game.request(); assert(!state.codexOverflow,'open codex survives rotation: '+state.codexOverflowNames);
        await game.screenshot('codex-rotated');
      }
      await game.named('CloseCodex');
      await game.page.reload(); await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(1500);
      state = await game.request();
      assert(state.records.some(row=>row.wins===1&&row.damage===60),'records survive reload');
    }
  })),
  {
    id:'records-fusion',name:'融合の戦績を素材へ均等配分して図鑑で公開',fixture:'fusion-dice',
    async run(game){
      await game.deploy(); await game.start(); let state = await game.result();
      assert(state.discovered.includes(-1011));
      const records = [1,2,3].map(id=>state.records.find(row=>row.id===id));
      assert(records.every(row=>row&&row.wins===1));
      assert(records.every(row=>row.damage===records[0].damage&&row.healed===records[0].healed));
      await game.named('OpenBattleRecords'); assert(!game.state.codexOverflow,'fusion record rows fit: '+game.state.codexOverflowNames);
      await game.screenshot('fusion-records'); await game.named('CloseCodex');
      await game.click(control=>control.text==='戻る','戻る'); await game.click(control=>control.text==='軍勢','軍勢');
      await game.named('OpenCodex'); await game.named('FusionTab');
      assert(game.state.codexTitles.includes('賽子:発見済')); await game.screenshot('discovered-fusion');
    }
  },
  {
    id:'records-repeat',name:'成績を閲覧中は連戦を保留し閉じると再開する',fixture:'standard',
    async run(game){
      await game.deploy(); await game.adjacent(); await game.start(); const state=await game.result();
      await game.named('RepeatStageButton'); await game.named('OpenBattleRecords');
      await game.page.waitForTimeout(3500); assert.equal((await game.request()).battle.battleId,state.battle.battleId);
      await game.named('CloseCodex');
      await game.wait(next=>next.battle.battleId!==state.battle.battleId,'repeat did not resume after closing records');
    }
  },
  {
    id:'records-defeat',name:'敗北時は成績を表示するが累計には加算しない',fixture:'animals-defeat',
    async run(game){
      await game.deploy(); await game.start(); const state=await game.result();
      assert(!state.battle.won); assert(state.records.every(row=>row.wins===0&&row.damage===0));
      await game.named('OpenBattleRecords'); await game.screenshot('defeat-records'); await game.named('CloseCodex');
    }
  }
];
