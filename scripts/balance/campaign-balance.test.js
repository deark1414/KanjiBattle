const test = require('node:test');
const assert = require('node:assert/strict');
const { party, summary, validateResults, validateCampaignEnemies } = require('./campaign-balance');

test('campaign measurements prove the enemy recipe, level and saved positions actually used',()=>{
  const campaign=[{level:4,enemies:[1,2,3,2],positions:[{x:2,y:1},{x:3,y:1},{x:2,y:0},{x:1,y:1}]}];
  const row={stage:1,initialEnemies:campaign[0].enemies.map((id,i)=>({id,level:4,...campaign[0].positions[i]}))};
  validateCampaignEnemies([row],campaign);
  for(const mutate of [r=>r.initialEnemies[0].id=-1011,r=>r.initialEnemies[0].level=1,
    r=>r.initialEnemies[0].x=4,r=>r.initialEnemies[0]={...r.initialEnemies[1]},r=>delete r.initialEnemies]){
    const bad=structuredClone(row);mutate(bad);assert.throws(()=>validateCampaignEnemies([bad],campaign));
  }
});

test('chapter bosses reserve undisclosed fusions and deploy without overlapping cells', () => {
  const campaign = require('../../Assets/Data/campaign-plan.json');
  const expected = [null, -1003, -1010, -1015, -1002, -1009, -1016, -1015];
  const seen = new Set();
  campaign.forEach((stage, index) => {
    const fusions = stage.enemies.filter(id => id < 0);
    assert.deepEqual(fusions, (index + 1) % 5 === 0 && index>=9 ? [expected[Math.floor(index / 5)]] : []);
    fusions.forEach(id => seen.add(id));
    if (!stage.positions) return;
    assert.equal(stage.positions.length, stage.enemies.length);
    assert.equal(new Set(stage.positions.map(p => `${p.x},${p.y}`)).size, stage.enemies.length);
    assert(stage.positions.every(p => p.x >= 0 && p.x < 6 && p.y >= 0 && p.y < 2));
    assert(stage.enemies.length <= 10);
  });
  assert.equal(seen.size, 6);
  assert.deepEqual(campaign[4].enemies,[1,2,3,2], 'The opening boss teaches number synergy without a self-healing fusion');
  assert(campaign[39].enemies.includes(26), 'The dragon remains the final boss');
  assert(campaign[34].enemies.includes(15), 'The late animal boss protects its support with a wall');
  assert.equal(campaign[39].enemies.filter(id=>id===15).length,2,'Two walls stop the central gun lanes');
  assert(campaign[39].level>campaign[38].level,'The final boss must not be lower-level than the preceding stage');
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

test('controlled comparisons verify actual HP and role multipliers without mixing conditions', () => {
  const data = require('../../Assets/Data/characters.json');
  const gun = data.find(row => row.id === 18), shield = data.find(row => row.id === 13);
  const stats = character => ({ id: character.id, level: 10,
    hp: character.baseHP + character.hpGrowth * 9,
    attack: character.baseAttack + character.attackGrowth * 9,
    defense: character.baseDefense + character.defenseGrowth * 9 });
  const multiplier = require('../../Assets/Resources/skills.json').find(row => row.skillTypeName === 'Gun').powerMultiplier;
  for (const [variant, scale] of [['baseline', 1], ['gun90', .9], ['gun80', .8]]) {
    const row = { variant, level: 10, party: [18], initialUnits: [{ ...stats(gun), roleMultiplier: multiplier * scale }] };
    validateResults([row]);
    assert.throws(() => validateResults([{ ...row, initialUnits: [{ ...row.initialUnits[0], roleMultiplier: 123 }] }]));
  }
  const row = { variant: 'shield90', level: 10, party: [13], initialUnits: [{ ...stats(shield),
    hp: Math.floor(stats(shield).hp * 9 / 10), roleMultiplier: 1 }] };
  validateResults([row]);
  assert.throws(() => validateResults([{ ...row, initialUnits: [{ ...row.initialUnits[0], hp: stats(shield).hp }] }]));
  const report = summary(['baseline', 'gun80'].map((variant, i) => ({ stage: 40, profile: 'fusion', variant,
    won: i === 0, turns: 10, allyHP: 100, enemyHP: 0 })));
  assert.equal(report.length, 2);
  assert.deepEqual(report.map(row => [row.variant, row.wins]), [['baseline', 1], ['gun80', 0]]);
  assert.equal(summary([{ stage: 40, profile: 'ranged', won: true, turns: 6, allyHP: 100, enemyHP: 0 }])[0].profile,
    'ranged', 'Summary uses the measured profiles rather than the current CLI defaults');
  const historical = { characters: data, skills: [{ skillTypeName: 'Gun', powerMultiplier: .123 }] };
  validateResults([{ variant: 'gun80', level: 10, party: [18], initialUnits: [{ ...stats(gun), roleMultiplier: .123 * .8 }] }], undefined, historical);
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
