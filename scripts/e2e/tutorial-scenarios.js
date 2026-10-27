const assert=require('node:assert/strict');
async function guide(game,id){
  await game.wait(state=>state.guideId===id,'Guide did not open: '+id);
  await game.page.waitForTimeout(180);await game.request();
  assert(!game.state.guideOverflow,'Guide copy must fit: '+id);
  const controls=game.state.controls.filter(c=>c.visible&&c.reachable);
  if(game.state.guideStep>=0){
    assert(game.state.guideInstructionSize>=21.9,'Instructions must remain large in CSS pixels, including Retina');
    assert(controls.some(c=>c.path.endsWith('/'+game.state.guideTarget)),'Highlighted real control must be reachable');
    assert.equal(controls.filter(c=>c.path.endsWith('/'+game.state.guideTarget)).length,1,'Exactly one real action is highlighted');
    if(['DeployButton','ConfirmDeployment'].includes(game.state.guideTarget)){
      const target=controls.find(c=>c.path.endsWith('/'+game.state.guideTarget));
      assert(target.rect.h*game.page.viewportSize().height>=47,'Guided action must be a full-size tap target');
    }
    assert(controls.every(c=>c.path.includes('/GuidedTutorial/')||c.path.endsWith('/'+game.state.guideTarget)),'Only the highlighted action may pass through');
  }else assert(controls.every(c=>c.path.includes('/FullScreenGuide/')),'Guide must block underlying actions');
}
async function dismiss(game,id){await guide(game,id);await game.named(game.state.guideStep>=0?'GuideSkip':'GuideContinue');}
async function moveGuided(game){
  await guide(game,'deployment');assert.equal(game.state.guideStep,0);assert.equal(game.state.battle.fusionSummary,'');
  const confirm=game.state.controls.find(c=>c.path.endsWith('/ConfirmDeployment'));
  assert(!confirm.reachable,'Opening battle must be blocked until placement is practiced');
  await game.clickBox(confirm.rect);await game.request();assert.equal(game.state.guideStep,0);assert(game.state.battle.deploying);
  await game.click(c=>c.path.endsWith('/DeploymentTarget'),'highlighted water');
  await game.wait(s=>s.guideStep===1,'destination step');await guide(game,'deployment');
  await game.screenshot('movement-demonstration');
  await game.click(c=>c.path.endsWith('/DeploymentTarget'),'highlighted rear cell');
  await game.wait(s=>s.guideStep===2,'confirm step');await guide(game,'deployment');
  assert.equal(game.state.guideTarget,'ConfirmDeployment');
  assert.equal(game.state.battle.units.find(u=>u.ally&&u.id===20).y,6,'Real water piece must have moved to the rear');
}
async function deployment(game){
  await game.deploy();await dismiss(game,'deployment');await dismiss(game,'fusion');
}
module.exports=[
  ...[{width:1280,height:800},{width:390,height:844},{width:844,height:390},{width:390,height:844,dpr:2},{width:844,height:390,dpr:2}].map(viewport=>({
    id:'tutorial-first-'+viewport.width+(viewport.dpr?'-retina':''),name:'初回の編成・自動戦闘・初勝利・加入 '+viewport.width+(viewport.dpr?' Retina':''),fixture:'tutorial',viewport:{width:viewport.width,height:viewport.height},deviceScaleFactor:viewport.dpr||1,
    async run(game){
      await guide(game,'opening');assert.deepEqual(game.state.formation.filter(Boolean),[1,10,20]);await game.screenshot('opening');
      await game.named('GuideSkip');await guide(game,'formation');await game.screenshot('formation-guide');
      await game.deploy();assert(game.state.guideSeen.includes('formation'));
      await guide(game,'deployment');await game.screenshot('deployment-guide');await moveGuided(game);
      await game.start();assert(game.state.guideSeen.includes('deployment'));await game.result();await guide(game,'victory');
      assert(game.state.battle.won);assert.equal(game.state.stagePoints,6);await game.screenshot('victory-guide');
      await game.named('GuideContinue');await guide(game,'castle');await game.named('GuideContinue');
      await game.named('TreeBranch_0');await game.named('TreeNode_c11');await game.named('TreePurchase');
      assert(game.state.owned.includes(11));assert.equal(game.state.stagePoints,0);await game.screenshot('first-recruit');
      await game.named('HomeTabButton');await game.named('FacilityTabButton');await game.page.waitForTimeout(200);
      assert.equal((await game.request()).guideId,'');
    }
  })),
  {
    id:'tutorial-cancel',name:'未読の画面離脱・再表示・既読後は一度だけ',fixture:'tutorial',
    async run(game){
      await guide(game,'opening');await game.named('GuideContinue');await guide(game,'formation');
      await game.request('tutorialRoute','top');assert(!game.state.guideSeen.includes('formation'));
      await game.request('tutorialRoute','formation');await dismiss(game,'formation');
      await game.request('tutorialRoute','top');await game.request('tutorialRoute','formation');await game.page.waitForTimeout(200);
      assert.equal((await game.request()).guideId,'');
      await game.deploy();await guide(game,'deployment');
      await game.page.setViewportSize({width:390,height:844});await game.page.waitForTimeout(600);await game.request();
      assert(!game.state.guideOverflow);await game.screenshot('guide-rotated');await game.named('GuideSkip');
    }
  },
  {
    id:'tutorial-settings',name:'オープニング非表示の保存・見直しは進行を変えない',fixture:'tutorial',viewport:{width:390,height:844},
    async run(game){
      await dismiss(game,'opening');await dismiss(game,'formation');
      await game.named('SettingsStatus');await game.named('OpeningToggle');assert(!game.state.openingEnabled);
      await game.screenshot('settings');const owned=game.state.owned,seen=game.state.guideSeen;
      await game.named('ReviewTutorialsButton');await guide(game,'help');
      await game.named('GuideHelp_fusion');await guide(game,'replay:fusion');await game.screenshot('review-fusion');
      assert.deepEqual(game.state.owned,owned);assert.deepEqual(game.state.guideSeen,seen);
      await game.named('GuideContinue');await guide(game,'help');await game.named('GuideContinue');
      await game.named('ReplayOpeningButton');await dismiss(game,'opening');assert(!game.state.openingEnabled);
      await game.page.reload();await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500);await game.request();assert.equal(game.state.guideId,'');assert(!game.state.openingEnabled);
      await game.request('treeCampaign','1');await game.page.waitForTimeout(200);await game.request();assert.equal(game.state.guideId,'');
      await game.deploy();await guide(game,'deployment');await game.named('GuideSkip');await game.named('CancelDeployment');
      await game.named('SettingsStatus');await game.named('OpeningToggle');assert(game.state.openingEnabled);
      await game.page.reload();await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500);await guide(game,'opening');await game.named('GuideSkip');
      assert.equal((await game.request()).guideId,'');
    }
  },
  {
    id:'tutorial-features',name:'融合・プレイヤースキル・図鑑の初回説明と一回制限',fixture:'standard',
    async run(game){
      await game.request('tutorialEnable');await deployment(game);await game.screenshot('after-fusion-guide');
      await game.named('PlayerSkillSelection');await dismiss(game,'player-skill');assert.equal(game.state.battle.playerSkillPlan.skillId,0);
      await game.named('CancelPlayerSkill');await game.named('PlayerSkillSelection');await game.page.waitForTimeout(200);assert.equal((await game.request()).guideId,'');
      await game.named('CancelPlayerSkill');await game.named('CancelDeployment');await dismiss(game,'formation');
      await game.deploy();await game.page.waitForTimeout(200);assert.equal((await game.request()).guideId,'');
      await game.named('CancelDeployment');await game.request('tutorialRoute','codex');await dismiss(game,'codex');
      await game.request('tutorialRoute','top');await game.request('tutorialRoute','codex');await game.page.waitForTimeout(200);assert.equal((await game.request()).guideId,'');
    }
  },
  {
    id:'tutorial-records',name:'成績の初回説明・閉じても再表示しない',fixture:'animals',
    async run(game){
      await game.request('tutorialEnable');await deployment(game);await game.request('recordsProbe');await dismiss(game,'victory');
      await game.named('OpenBattleRecords');await guide(game,'records');await game.screenshot('records-guide');await game.named('GuideContinue');
      await game.named('CloseCodex');await game.named('OpenBattleRecords');await game.page.waitForTimeout(200);assert.equal((await game.request()).guideId,'');
    }
  },
  {
    id:'tutorial-defeat',name:'敗北の説明は一度だけ・報酬なしを維持',fixture:'animals-defeat',
    async run(game){
      await game.request('tutorialEnable');await deployment(game);const before=game.state.stagePoints;
      await game.start();await game.result();await guide(game,'defeat');assert(!game.state.battle.won);assert.equal(game.state.stagePoints,before);
      await game.screenshot('defeat-guide');await game.named('GuideContinue');await game.named('RetryStageButton');
      assert(game.state.battle.deploying);await game.start();await game.result();await game.page.waitForTimeout(200);
      assert.equal((await game.request()).guideId,'');
    }
  },
  {
    id:'tutorial-repeat',name:'説明を読む間は自動連戦を保留・閉じると復帰',fixture:'animals',
    async run(game){
      await game.request('tutorialEnable');await deployment(game);await game.start();await game.named('AutoRepeatControl');
      const state=await game.result();await guide(game,'victory');await game.page.waitForTimeout(3500);
      assert.equal((await game.request()).battle.battleId,state.battle.battleId);
      await game.named('GuideContinue');await game.wait(next=>next.battle.battleId!==state.battle.battleId,'repeat did not resume after guide');
      assert(game.state.guideSeen.includes('victory'));
    }
  },
  ...[{width:390,height:844},{width:844,height:390}].map(viewport=>({
    id:'tutorial-gallery-'+viewport.width,name:'全9説明の文字・図・見直し '+viewport.width,fixture:'tutorial',viewport,
    async run(game){
      await dismiss(game,'opening');await dismiss(game,'formation');await game.named('SettingsStatus');
      const seen=game.state.guideSeen,owned=game.state.owned;
      await game.named('ReviewTutorialsButton');
      for(const id of ['formation','deployment','victory','castle','player-skill','fusion','records','codex','defeat']){
        await guide(game,'help');await game.named('GuideHelp_'+id);await guide(game,'replay:'+id);
        await game.screenshot('gallery-'+id);await game.named('GuideContinue');
      }
      assert.deepEqual(game.state.guideSeen,seen);assert.deepEqual(game.state.owned,owned);
    }
  }))
];
