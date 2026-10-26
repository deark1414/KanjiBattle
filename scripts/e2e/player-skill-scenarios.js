const assert=require('node:assert/strict');
async function select(game,id,turn=1){
  await game.named('PlayerSkillSelection');
  await game.named('PlayerSkillOption_'+id);
  for(let n=1;n<turn;n++)await game.named('NextPlayerSkillTurn');
  await game.named('ApplyPlayerSkill');
  assert.equal(game.state.battle.playerSkillPlan.skillId,id);
  assert.equal(game.state.battle.playerSkillPlan.turn,turn);
}
module.exports=[
  ...[['attack',1,1280],['guard',2,390],['heal',3,844]].map(([mode,id,width])=>({
    id:'player-skill-'+mode,name:`プレイヤースキル ${mode}: 実効果・期間・成績帰属`,fixture:'animals',
    viewport:{width,height:width===390?844:width===844?390:800},
    async run(game){
      await game.deploy();await select(game,id,3);await game.screenshot('configured');
      await game.named('PlayerSkillSelection');await game.screenshot('skill-selection');await game.named('CancelPlayerSkill');
      const state=await game.request('playerSkillProbe',mode);
      assert(state.battle.logs.some(text=>text.includes('プレイヤースキル能力チェック完了')));
      assert(state.battle.playerSkillFired&&!state.battle.playerSkillActive);
      await game.screenshot('effect-result');
    }
  })),
  {
    id:'player-skill-battle',name:'プレイヤースキル: 融合後の実戦発動・一回制限・縦横切替',fixture:'fusion-dice',
    async run(game){
      await game.deploy();await game.named('PlayerSkillSelection');await game.named('PlayerSkillOption_1');
      await game.page.setViewportSize({width:390,height:844});await game.page.waitForTimeout(700);
      await game.screenshot('rotated-selection');await game.named('ApplyPlayerSkill');
      await game.start();const state=await game.result();
      const logs=state.battle.logs;
      assert.equal(logs.filter(text=>text.includes('プレイヤースキル【')).length,1);
      const activation=logs.findIndex(text=>text.includes('プレイヤースキル【'));
      const turn=logs.findIndex(text=>text.includes('--- ターン 1 ---'));
      assert(turn>=0&&activation===turn+1,'activation must precede all turn actions');
      const fusion=logs.findIndex(text=>text.includes('→ 賽子'));
      assert(fusion>=0&&fusion<turn,'fusion must precede turn one');
      assert(state.battle.playerSkillFired&&!state.battle.playerSkillActive);
      await game.screenshot('real-battle-result');
      await game.named('OpenBattleRecords');
      assert(game.state.controls.some(control=>control.text.includes('軍師・鼓舞')));
      assert(!game.state.codexOverflow,'commander record fits the result view');
      await game.screenshot('commander-record');await game.named('CloseCodex');
    }
  },
  {
    id:'player-skill-save',name:'プレイヤースキル: 取消・保存・再読込・再戦',fixture:'animals',
    async run(game){
      await game.deploy();await select(game,1,3);await game.named('CancelDeployment');
      await game.deploy();assert.equal(game.state.battle.playerSkillPlan.skillId,0,'cancelled deployment is not saved');
      await select(game,1,3);
      await game.named('PlayerSkillSelection');await game.named('PlayerSkillOption_2');
      await game.named('NextPlayerSkillTurn');await game.named('CancelPlayerSkill');
      assert.equal(game.state.battle.playerSkillPlan.skillId,1);assert.equal(game.state.battle.playerSkillPlan.turn,3);
      await game.start();await game.result();
      await game.named('RetryStageButton');assert.equal(game.state.battle.playerSkillPlan.turn,3);assert(!game.state.battle.playerSkillFired);
      await game.page.reload();await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500);await game.request('resume');await game.deploy();
      assert.equal(game.state.battle.playerSkillPlan.skillId,1);assert.equal(game.state.battle.playerSkillPlan.turn,3);
    }
  },
  {
    id:'player-skill-traps',name:'プレイヤースキル: 初期罠の敗北後は未発動',fixture:'animals',
    async run(game){
      await game.deploy();await select(game,1);await game.request('playerSkillTraps');await game.start();
      const state=await game.result();assert(!state.battle.won&&!state.battle.playerSkillFired);
      assert(!state.battle.logs.some(text=>text.includes('プレイヤースキル【')));
    }
  },
  {
    id:'player-skill-late',name:'プレイヤースキル: 指定ターン前の勝利と連戦リセット',fixture:'animals',
    async run(game){
      await game.deploy();await game.named('PlayerSkillSelection');await game.named('PlayerSkillOption_1');
      await game.named('PlayerSkillTurn');await game.page.keyboard.press('End');await game.page.waitForTimeout(100);
      await game.page.keyboard.press('Backspace');await game.page.keyboard.type('20',{delay:100});await game.page.waitForTimeout(250);
      await game.named('ApplyPlayerSkill');assert.equal(game.state.battle.playerSkillPlan.turn,20);
      await game.start();let state=await game.result();
      assert(!state.battle.playerSkillFired);
      assert(!state.battle.logs.some(text=>text.includes('プレイヤースキル【')));
      await game.named('RepeatStageButton');
      state=await game.wait(s=>!s.battle.result&&s.battle.battleId!==state.battle.battleId,'repeat did not start');
      assert.equal(state.battle.playerSkillPlan.turn,20);assert(!state.battle.playerSkillFired);
    }
  },
  {
    id:'player-skill-locks',name:'プレイヤースキル: 解放条件と不正設定の再確認',fixture:'animals',
    async run(game){
      await game.deploy();await game.request('playerSkillGate');await game.named('PlayerSkillSelection');
      for(const id of [2,3])assert(!game.state.controls.find(c=>c.path.endsWith('/PlayerSkillOption_'+id)).enabled);
      assert(game.state.controls.find(c=>c.path.endsWith('/PlayerSkillOption_1')).enabled);
      await game.named('CancelPlayerSkill');await game.named('CancelDeployment');
      await game.request('corruptPlayerSkill');await game.request('resume');await game.deploy();
      assert(!game.state.controls.find(c=>c.path.endsWith('/ConfirmDeployment')).enabled);
      await game.named('PlayerSkillSelection');await game.named('PlayerSkillOption_0');await game.named('ApplyPlayerSkill');
      assert(game.state.controls.find(c=>c.path.endsWith('/ConfirmDeployment')).enabled);
    }
  }
];
