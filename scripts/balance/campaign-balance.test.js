const test = require('node:test');
const assert = require('node:assert/strict');
const { party, summary, validateResults } = require('./campaign-balance');

test('chapter bosses reserve undisclosed fusions and deploy without overlapping cells', () => {
  const campaign = require('../../Assets/Data/campaign-plan.json');
  const expected = [-1011, -1003, -1010, -1015, -1002, -1009, -1016, -1015];
  const seen = new Set();
  campaign.forEach((stage, index) => {
    const fusions = stage.enemies.filter(id => id < 0);
    assert.deepEqual(fusions, (index + 1) % 5 === 0 ? [expected[Math.floor(index / 5)]] : []);
    fusions.forEach(id => seen.add(id));
    if (!stage.positions) return;
    assert.equal(stage.positions.length, stage.enemies.length);
    assert.equal(new Set(stage.positions.map(p => `${p.x},${p.y}`)).size, stage.enemies.length);
    assert(stage.positions.every(p => p.x >= 0 && p.x < 6 && p.y >= 0 && p.y < 2));
    assert(stage.enemies.length <= 10);
  });
  assert.equal(seen.size, 7);
  assert(campaign[39].enemies.includes(26), 'The dragon remains the final boss');
});

test('unowned Lv1 fixtures and missing proof are rejected, actual shared stats pass', () => {
  const row = { stage: 10, seed: 1, profile: 'mixed', level: 8, party: [10],
    initialUnits: [{ id: 10, level: 8, hp: 155, attack: 68, defense: 15 }] };
  validateResults([row], [{ stage: 10, seed: 1, profile: 'mixed' }]);
  assert.throws(() => validateResults([{ ...row, initialUnits: undefined }]));
  assert.throws(() => validateResults([{ ...row, initialUnits: [{ ...row.initialUnits[0], level: 1 }] }]));
  assert.throws(() => validateResults([{ ...row, initialUnits: [{ ...row.initialUnits[0], hp: 120 }] }]));
  assert.throws(() => validateResults([], [{}]));
});

test('paid progression rejects unowned material and oversubscribed formation slots', () => {
  const row = { level: 8, party: [10], economyEntry: { level: 8, slots: 3, owned: [10] },
    initialUnits: [{ id: 10, level: 8, hp: 155, attack: 68, defense: 15, materials: [10] }] };
  validateResults([row]);
  assert.throws(() => validateResults([{ ...row, economyEntry: { ...row.economyEntry, owned: [] } }]));
  assert.throws(() => validateResults([{ ...row, economyEntry: { ...row.economyEntry, slots: 0 } }]));
});

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
