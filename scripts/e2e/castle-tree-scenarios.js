const assert=require('node:assert/strict');
const expand=['c11','s1','c13','c14','s2','c27','s3','c23','s4','c19','c31','c32','s5'];
module.exports=[
  ...[{width:1280,height:800},{width:390,height:844},{width:844,height:390}].map(viewport=>({
    id:'tree-layout-'+viewport.width,name:'城下ツリーの加入・戦果不足・章条件 '+viewport.width,fixture:'standard',viewport,
    async run(game){
      await game.request('treeFresh');await game.page.waitForTimeout(700);await game.request();
      assert.deepEqual(game.state.owned,[1,10,20]);assert.equal(game.state.treeSlots,3);
      await game.named('TreeBranch_0');await game.named('TreeNode_c11');
      assert(!game.state.controls.find(c=>c.path.endsWith('/TreePurchase')).enabled);
      await game.request('treeFunds','6');await game.page.waitForTimeout(600);
      await game.named('TreePurchase');assert(game.state.owned.includes(11));assert.equal(game.state.stagePoints,0);
      await game.named('TreeNode_s1');assert(!game.state.controls.find(c=>c.path.endsWith('/TreePurchase')).enabled);
      await game.request('treeFunds','10');await game.request('treeAdvance','5');await game.page.waitForTimeout(600);
      await game.named('TreePurchase');assert.equal(game.state.treeSlots,4);
      await game.screenshot('castle-tree');assert(!game.state.treeOverflow,'tree text must fit');
      if(viewport.width===390){
        await game.page.setViewportSize({width:844,height:390});await game.page.waitForTimeout(800);await game.request();
        assert(!game.state.treeOverflow);await game.screenshot('tree-rotated');
      }
      await game.named('HomeTabButton');assert(!game.state.controls.some(c=>c.path.includes('TreeNode_')));
      await game.named('FacilityTabButton');await game.named('TreeBranch_1');
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
