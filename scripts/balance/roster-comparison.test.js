const test = require('node:test');
const assert = require('node:assert/strict');
const { group, analyze, validateComparison } = require('./roster-comparison');
test('summaries include losses and distinguish paired wins from speed differences', () => {
  const rows = [
    { profile: 'fusion/-1/materials', stage: 40, seed: 1, won: false, turns: 20 },
    { profile: 'fusion/-1/fused', stage: 40, seed: 1, won: true, turns: 10 },
    { profile: 'fusion/-1/materials', stage: 35, seed: 1, won: true, turns: 5 },
    { profile: 'fusion/-1/fused', stage: 35, seed: 1, won: false, turns: 12 }
  ].map(row => ({ ...row, allyHP: 50, enemyHP: 10, initialUnits: [{ hp: 100 }] }));
  const result = analyze(rows, { characters: [], recipes: [{ id: -1, name: 'test' }] }).fusions[0];
  assert.equal(result.better, 1); assert.equal(result.worse, 1);
  assert.equal(result.materialsResult.turns, 12.5); assert.equal(result.materialsResult.timeouts, 1);
  assert.equal(group(rows).remaining, .5);
  assert.throws(() => analyze(rows.slice(1), { characters: [], recipes: [{ id: -1 }] }), /Missing paired/);
});
test('comparison validates actual eight-slot stats and ignores computed Unity vector metadata', () => {
  const source = { characters: [{ id: 10, baseHP: 100, hpGrowth: 1, baseAttack: 10, attackGrowth: 1,
    baseDefense: 1, defenseGrowth: 1, skillType: 'Slash' }], skills: [{ skillTypeName: 'Slash', powerMultiplier: 1 }] };
  const positions = Array.from({ length: 8 }, (_, index) => ({ x: index % 6, y: 5 + Math.floor(index / 6), magnitude: 7 }));
  const job = { stage: 35, seed: 1, profile: 'normal/10', variant: 'baseline', allyPositions: positions };
  const row = { ...job, comparison: true, level: 33, party: Array(8).fill(10), upgrades: false,
    initialUnits: positions.map(cell => ({ id: 10, level: 33, hp: 132, attack: 42, defense: 33,
      roleMultiplier: 1, x: cell.x, y: cell.y, materials: [10] })) };
  const manifest = { kind: 'projectile-probe', characters: [{ id: 10 }], recipes: [], stages: [35], seeds: 1, level: 33 };
  validateComparison([row], [job], manifest, source);
  validateComparison([row], [job], {...manifest, kind: 'guard-probe'}, source);
  validateComparison([row], [job], {...manifest, kind: 'fusion-probe'}, source);
  const bad = structuredClone(row); bad.initialUnits[0].materials = [];
  assert.throws(() => validateComparison([bad], [job], manifest, source));
  const moved = structuredClone(row); moved.initialUnits[0].x = 5;
  assert.throws(() => validateComparison([moved], [job], manifest, source), /positions/);
  assert.throws(() => validateComparison([row,row], [job,job], {...manifest,seeds:2}, source), /Duplicate/);
});
