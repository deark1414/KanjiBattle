const test=require('node:test');
const assert=require('node:assert/strict');
const {summarize}=require('./summarize-audit');
const cases=[{id:'a',name:'A'},{id:'b',name:'B'}];
test('rechecks retain failure history and never substitute for missing coverage',()=>{
  const reports=[{startedAt:'2026-10-27T01:00:00Z',source:'first',results:[{id:'a',status:'failed'}]},
    {startedAt:'2026-10-27T02:00:00Z',source:'retry',results:[{id:'a',status:'passed'}]}];
  const rows=summarize(cases,reports.reverse());
  assert.equal(rows[0].status,'passed');assert.equal(rows[0].attempts.length,2);
  assert.equal(rows[0].attempts[0].status,'failed');assert.equal(rows[1].status,'missing');
});
test('a later regression cannot be hidden behind an earlier pass',()=>{
  const rows=summarize(cases,[{startedAt:'2026-10-27T01:00:00Z',results:[{id:'a',status:'passed'}]},
    {startedAt:'2026-10-27T02:00:00Z',results:[{id:'a',status:'failed'}]}]);
  assert.equal(rows[0].status,'failed');
});
test('unknown and duplicate identifiers are rejected',()=>{
  assert.throws(()=>summarize(cases,[{results:[{id:'other',status:'passed'}]}]));
  assert.throws(()=>summarize(cases,[{results:[{id:'a',status:'passed'},{id:'a',status:'passed'}]}]));
});
