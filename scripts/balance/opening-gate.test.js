const test=require('node:test');
const assert=require('node:assert/strict');
const {makePlan}=require('./opening-gate');

test('opening comparison uses paid three-slot parties and checks each candidate with five seeds',()=>{
  const jobs=makePlan();
  assert.equal(jobs.length,80);
  assert.equal(new Set(jobs.map(j=>`${j.profile}/${j.seed}`)).size,80);
  for(const profile of new Set(jobs.map(j=>j.profile)))
    assert.equal(jobs.filter(j=>j.profile===profile).length,5);
  for(const job of jobs){
    assert.equal(job.stage,5);
    assert.equal(job.economyWins,3);
    assert.equal(job.party.length,3);
    assert(job.party.every(id=>[1,10,13,20].includes(id)));
    assert.equal(job.enemyIds.length,job.enemyPositions.length);
    assert.equal(new Set(job.enemyPositions.map(c=>`${c.x},${c.y}`)).size,job.enemyIds.length);
  }
  assert.throws(()=>makePlan(0));
  assert.throws(()=>makePlan(11));
  const numbers=makePlan(5,true);
  assert.equal(numbers.length,40);
  assert(numbers.every(job=>job.enemyIds.every(id=>id>0)&&job.enemyIds.includes(3)));
});
