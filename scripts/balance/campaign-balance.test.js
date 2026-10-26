const test = require('node:test');
const assert = require('node:assert/strict');
const { party, summary } = require('./campaign-balance');

test('each profile uses the same material slot budget and no premature fusion', () => {
  for (let stage = 1; stage <= 40; stage++) {
    const capacity = 3 + [5, 10, 20, 30, 35].filter(gate => gate < stage).length;
    for (const profile of ['mixed', 'fusion', 'ranged', 'spear', 'sentinel']) {
      const ids = party(stage, profile);
      assert.equal(ids.reduce((sum, id) => sum + (id < 0 ? 2 : 1), 0), capacity);
      assert.ok(stage > 20 || ids.every(id => id > 0));
      assert.ok(ids.filter(id => id === 20).length === 1);
    }
  }
});

test('fusion comparisons preserve exactly the same material characters', () => {
  for (let stage = 21; stage <= 40; stage++) {
    for (const [normal, fused, recipe, materials] of [
      ['mixed', 'fusion', -1003, [10, 13]], ['spear', 'sentinel', -1004, [11, 13]]
    ]) {
      const expanded = party(stage, fused).flatMap(id => id === recipe ? materials : [id]);
      assert.deepEqual(expanded.sort((a, b) => a - b), party(stage, normal).sort((a, b) => a - b));
    }
  }
});

test('a timeout requires both armies to survive the actual turn limit', () => {
  const rows = summary([
    { stage: 40, profile: 'fusion', won: false, turns: 20, allyHP: 100, enemyHP: 20 },
    { stage: 40, profile: 'fusion', won: false, turns: 20, allyHP: 0, enemyHP: 20 },
    { stage: 40, profile: 'fusion', won: true, turns: 10, allyHP: 100, enemyHP: 0 }
  ]);
  assert.equal(rows.length, 1);
  assert.deepEqual(rows[0], { stage: 40, profile: 'fusion', samples: 3, wins: 1, meanTurns: 16.7, timeouts: 1 });
});
