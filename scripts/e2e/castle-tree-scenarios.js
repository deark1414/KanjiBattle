const assert=require('node:assert/strict');
const expand=['c11','s1','c13','c14','s2','c27','s3','c23','s4','c19','c31','c32','s5'];
module.exports=[
  {
    id:'tree-investment',name:'個別強化・融合継承・保存・強化だけ無料振り直し',fixture:'standard',viewport:{width:390,height:844},
    async run(game){
      await game.request('treeFresh');await game.request('treeAdvance','15');
      await game.request('treeTryPurchase','u10_0');assert.equal(game.state.lastTreePurchase,false);assert.equal(game.state.investmentRefund,0);
      await game.request('treeFunds','300');
      await game.request('treePurchase','c11');await game.request('treePurchase','s1');await game.request('treePurchase','f6');
      const permanent=[...game.state.treeOwned];const before=game.state.stagePoints;
      for(const id of ['u10_0','u10_1','u10_2'])await game.request('treePurchase',id);
      assert.equal(game.state.investmentRefund,18);assert.equal(game.state.stagePoints,before-18);
      await game.request('treeTryPurchase','u10_0');assert.equal(game.state.lastTreePurchase,false);assert.equal(game.state.investmentRefund,18);
      await game.request('investmentProbe');
      await game.page.waitForTimeout(800);await game.request();
      await game.named('TreeCenter');await game.named('TreeBranch_0');
      await game.named('TreeNode_u10_0');await game.screenshot('personal-health');assert(!game.state.treeOverflow);
      await game.named('TreeRespec');await game.screenshot('respec-confirmation');
      await game.named('TreeRespecCancel');assert.equal(game.state.investmentRefund,18);
      await game.page.waitForTimeout(1500);await game.page.reload();
      await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500);await game.request();
      assert.equal(game.state.investmentRefund,18);assert(game.state.treeOwned.includes('u10_0'));
      await game.named('FacilityTabButton');await game.named('TreeRespec');await game.named('TreeRespecConfirm');
      assert.equal(game.state.stagePoints,before);assert.equal(game.state.investmentRefund,0);
      assert(permanent.every(id=>game.state.treeOwned.includes(id)),'Recruitment, slots and facilities stay unlocked');
      assert(!game.state.treeOwned.some(id=>id.startsWith('u')));assert.equal(game.state.treeSlots,4);
      await game.request('investmentProbe');assert(!game.state.controls.find(c=>c.path.endsWith('/TreeRespec')).enabled);
      await game.screenshot('reset-keeps-unlocks');
    }
  },
  {
    id:'tree-legacy-refund',name:'旧全体強化の返金は一度だけ、他の施設は維持',fixture:'standard',
    async run(game){
      await game.request('treeFresh');await game.request('treeAdvance','35');await game.request('treeFunds','100');
      await game.request('treePurchase','c11');await game.request('treePurchase','s1');await game.request('treePurchase','f6');
      const permanent=[...game.state.treeOwned],before=game.state.stagePoints,refund=game.state.retiredRefundExpected;
      await game.request('legacyCombatSave');await game.page.waitForTimeout(1800);
      for(let i=0;i<2;i++){
        await game.page.reload();await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
        await game.page.waitForTimeout(4500);await game.request();
        assert.equal(game.state.stagePoints,before+refund);assert(permanent.every(id=>game.state.treeOwned.includes(id)));
        assert(!game.state.treeOwned.some(id=>['f8','f9','f10','f11'].includes(id)));
      }
      await game.request('investmentProbe');
    }
  },
  {
    id:'tree-radial',name:'5方向マップ・章ごとの表示・建物アイコン・順不同の増枠',fixture:'standard',
    async run(game){
      await game.request('treeFresh');await game.request('treeFunds','1000');await game.page.waitForTimeout(700);await game.request();
      assert(!game.state.treeVisible.includes('c23'));assert(!game.state.treeVisible.includes('s4'));
      assert(game.state.treeFacilityIcons.length>0,'Existing facility art must render');
      for(const id of game.state.treeVisible.filter(id=>id.startsWith('f')))assert(game.state.treeFacilityIcons.includes(id),id+' needs building art');
      assert(game.state.controls.filter(c=>/TreeBranch_/.test(c.path)).every(c=>c.path.includes('/TreeContent/')),'Branch navigation belongs inside the map');
      const zoom=game.state.treeZoom;await game.named('TreeZoomIn');assert(game.state.treeZoom>zoom);
      await game.named('TreeZoomOut');assert(Math.abs(game.state.treeZoom-zoom)<.01);await game.named('TreeCenter');await game.screenshot('initial-radial-map');
      await game.request('treeAdvance','30');await game.page.waitForTimeout(600);await game.request();
      assert(game.state.treeVisible.includes('c23')&&!game.state.treeVisible.includes('s5'));
      await game.request('treePurchase','c23');await game.request('treePurchase','s4');
      assert.equal(game.state.treeSlots,4);assert(!game.state.treeOwned.includes('s1'),'No order dependency for +1 nodes');
      await game.screenshot('chapter-30-map');await game.request('treeAdvance','35');await game.page.waitForTimeout(600);await game.request();
      assert(game.state.treeVisible.includes('s5'));assert(!game.state.treeOverflow);await game.screenshot('chapter-35-map');
    }
  },
  ...[{width:1280,height:800},{width:390,height:844},{width:844,height:390}].map(viewport=>({
    id:'tree-layout-'+viewport.width,name:'城下ツリーの加入・戦果不足・章条件 '+viewport.width,fixture:'standard',viewport,
    async run(game){
      await game.request('treeFresh');await game.page.waitForTimeout(700);await game.request();
      assert.deepEqual(game.state.owned,[1,10,20]);assert.equal(game.state.treeSlots,3);
      assert(game.state.treeZoom<.8,'Initial view must be pulled back');
      assert(!game.state.treeVisible.includes('s1')&&!game.state.treeVisible.includes('c23'),'Future chapters stay hidden');
      const first=game.state.controls.find(c=>c.path.endsWith('/TreeNode_c11'));
      const sibling=game.state.controls.find(c=>c.path.endsWith('/TreeNode_c16'));
      assert(Math.abs(first.rect.x-sibling.rect.x)>.05,'Sibling paths must branch horizontally, not form a list');
      await game.named('TreeBranch_0');await game.named('TreeNode_c11');
      assert(!game.state.controls.find(c=>c.path.endsWith('/TreePurchase')).enabled);
      await game.request('treeFunds','6');await game.page.waitForTimeout(600);
      await game.named('TreePurchase');assert(game.state.owned.includes(11));assert.equal(game.state.stagePoints,0);
      await game.request('treeFunds','10');await game.request('treeAdvance','5');await game.page.waitForTimeout(600);
      await game.request();assert(game.state.treeVisible.includes('s1'),'Chapter completion reveals its nodes');
      await game.named('TreeNode_s1');
      await game.named('TreePurchase');assert.equal(game.state.treeSlots,4);
      await game.screenshot('castle-tree');assert(!game.state.treeOverflow,'tree text must fit');
      if(viewport.width===390){
        await game.page.setViewportSize({width:844,height:390});await game.page.waitForTimeout(800);await game.request();
        assert(!game.state.treeOverflow);await game.screenshot('tree-rotated');
      }
      await game.named('HomeTabButton');assert(!game.state.controls.some(c=>c.path.includes('TreeNode_')));
      await game.named('FacilityTabButton');await game.named('TreeCenter');await game.named('TreeBranch_1');
      await game.named('TreeNode_c13');await game.screenshot('second-branch');
    }
  })),
  {
    id:'tree-save',name:'分岐の選択と再読込・全枝を後から取得',fixture:'standard',
    async run(game){
      await game.request('treeFresh');await game.request('treeFunds','1000');
      await game.request('treePurchase','c13');assert(game.state.owned.includes(13)&&!game.state.owned.includes(11));
      await game.request('treeAdvance','35');for(const id of expand)if(!game.state.treeOwned.includes(id))await game.request('treePurchase',id);
      assert.equal(game.state.treeSlots,8);
      await game.request('treeSpecialProbe');
      await game.page.waitForTimeout(1200);
      await game.page.reload();await game.page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});
      await game.page.waitForTimeout(4500);await game.request();assert.equal(game.state.treeSlots,8);assert(game.state.owned.includes(13));
      await game.named('FacilityTabButton');await game.screenshot('saved-tree');
      await game.page.setViewportSize({width:390,height:844});await game.page.waitForTimeout(700);
      await game.request('treeCampaign','1');assert.equal(game.state.formation.filter(Boolean).length,8);
      await game.screenshot('eight-slots-early-stage');await game.deploy();await game.start();
      assert.equal(game.state.battle.units.filter(u=>u.ally).length,8);
    }
  },
  {
    id:'tree-first-battle',name:'初期3体で実際の第1局を攻略',fixture:'standard',
    async run(game){
      await game.request('treeFresh');await game.request('treeCampaignDefault','1');
      assert.deepEqual(game.state.formation.filter(Boolean),[1,10,20]);
      await game.deploy();await game.start();const state=await game.result();assert(state.battle.won);
      assert(!state.battle.logs.some(log=>log.includes('縁')));await game.screenshot('first-victory');
    }
  },
  ...[12,17,22,35,39,40].map(stage=>({
    id:'tree-enemy-formation-'+stage,name:'実ステージの敵布陣 '+stage,fixture:'standard',
    async run(game){
      await game.request('treeCampaign',String(stage));await game.deploy();
      const enemies=game.state.battle.units.filter(u=>!u.ally);
      assert(enemies.length<=10);assert(enemies.some(u=>u.y===0)&&enemies.some(u=>u.y===1));
      assert.equal(new Set(enemies.map(u=>u.x+','+u.y)).size,enemies.length);
      await game.screenshot('enemy-formation');
    }
  }))
];
