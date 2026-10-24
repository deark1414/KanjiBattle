const assert = require("node:assert/strict");
const recipes = ["wave","magnet","mountain","coal","fortress","dice","rain","sand","grass","cocoon","windchime","lantern"];

module.exports = recipes.flatMap((name,index)=>[
  {
    id:`fusion-${name}-abilities`,name:`通常融合 ${name}: 固有能力と境界条件`,fixture:`fusion-${name}`,
    async run(game) {
      const initial=await game.deploy();
      assert.equal(initial.battle.plan.groups.length,1);
      assert.equal(initial.battle.plan.groups[0].members.length,name==="dice"?3:2);
      assert(initial.battle.fusionSummary.includes("？"));
      await game.start();
      const state=await game.request("advancedFusionProbe");
      assert(state.discovered.includes(-1006-index));
      assert(state.battle.logs.some(log=>log.includes("通常融合の能力チェック完了")));
      await game.screenshot("advanced-abilities");
    }
  },
  {
    id:`fusion-${name}-battle`,name:`通常融合 ${name}: 実戦と再戦復元`,fixture:`fusion-${name}`,
    viewport:index%3===0?{width:390,height:844}:undefined,
    async run(game) {
      const initial=await game.deploy();
      await game.start();
      const result=await game.wait(state=>state.battle.result,"Advanced fusion battle did not finish.",180000);
      assert(!result.battle.resultTextOverflow);
      assert(result.discovered.includes(-1006-index));
      await game.screenshot("advanced-result");
      await game.named("RetryStageButton");
      const state=await game.wait(current=>current.battle.deploying,"Advanced fusion retry did not open.");
      assert.deepEqual(state.battle.originalIds,initial.battle.originalIds);
      const units=state.battle.units.filter(unit=>unit.ally);
      assert.equal(units.length,6);
      assert(units.every(unit=>unit.id>0 && unit.hp===unit.maxHP && unit.attackBuff===0));
      assert.equal(state.battle.plan.groups.length,1);
    }
  }
]);
