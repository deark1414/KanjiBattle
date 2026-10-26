const test=require('node:test');
const assert=require('node:assert/strict');
const {makePlan,validateStress,scenarios}=require('./fusion-stress');

test('stress plan keeps enemy composition fixed for spread comparison and budgets two fusions',()=>{
  const recipes=[{id:-1007,name:'磁石',materials:[31,33]},{id:-1012,name:'雨',materials:[20,17]},
    {id:-1002,name:'散弾',materials:[3,18]}];
  const source=recipes.flatMap(recipe=>['materials','fused'].map(form=>({
    profile:`fusion/${recipe.id}/${form}`,party:form==='materials'?[...recipe.materials,13,10,17,20,27,18]:[recipe.id,13,10,17,20,27,18],
    allyPositions:Array.from({length:form==='materials'?8:7},(_,i)=>({x:i%6,y:5+Math.floor(i/6)})),
    variant:'baseline'})));
  const plan=makePlan(source,{recipes},3);
  assert.equal(plan.length,96);
  assert.deepEqual(scenarios[0].ids,scenarios[1].ids);
  assert.notDeepEqual(scenarios[0].positions,scenarios[1].positions);
  assert(plan.every(job=>job.level===33&&job.enemyLevel===33&&job.stage===1&&!job.upgrades));
  for(const job of plan){
    assert.equal(job.party.reduce((sum,id)=>sum+(id>0?1:recipes.find(r=>r.id===id).materials.length),0),8);
    assert.equal(new Set(job.allyPositions.map(c=>`${c.x},${c.y}`)).size,job.party.length);
    if(job.profile.endsWith('/double'))assert.equal(job.party.filter(id=>id<0).length,2);
  }
  assert.throws(()=>makePlan(source,{recipes},0));
});

test('stress validation rejects changed enemy positions, level and material budget',()=>{
  const context={characters:[{id:10,baseHP:100,hpGrowth:1,baseAttack:10,attackGrowth:1,baseDefense:1,defenseGrowth:1}],skills:[],recipes:[]};
  const positions=Array.from({length:8},(_,i)=>({x:i%6,y:5+Math.floor(i/6)}));
  const job={stage:1,seed:1729,profile:'test',variant:'baseline',party:Array(8).fill(10),allyPositions:positions,
    enemyIds:[10],enemyPositions:[{x:2,y:1}]};
  const row={...job,level:33,comparison:true,upgrades:false,
    initialUnits:positions.map(cell=>({id:10,level:33,hp:132,attack:42,defense:33,roleMultiplier:1,materials:[10],...cell})),
    initialEnemies:[{id:10,level:33,x:2,y:1}]};
  validateStress([row],[job],context);
  const annotated=structuredClone(job);
  annotated.allyPositions.forEach(cell=>{cell.magnitude=7;cell.sqrMagnitude=49;});
  validateStress([row],[annotated],context);
  for(const change of [r=>r.initialEnemies[0].x=3,r=>r.initialEnemies[0].level=1,r=>r.initialUnits[0].materials=[]]){
    const bad=structuredClone(row);change(bad);assert.throws(()=>validateStress([bad],[job],context));
  }
});
