const fs = require('node:fs');
const path = require('node:path');
const { spawn, execFileSync } = require('node:child_process');
const assert = require('node:assert/strict');
const root = path.resolve(__dirname, '../..');
const args = process.argv.slice(2);
const option = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
const stages = JSON.parse(fs.readFileSync(path.join(root, 'Assets/Data/stages.json')));
const characters = JSON.parse(fs.readFileSync(path.join(root, 'Assets/Data/characters.json')));
const skills = JSON.parse(fs.readFileSync(path.join(root, 'Assets/Resources/skills.json')));
const variants = option('--variants', 'baseline').split(',');
const variantNames = ['baseline', 'shield90', 'gun90', 'gun80', 'gun70', 'gun60'];
const seeds = Number(option('--seeds', 3));
const selected = option('--stages', stages.map(s => s.stageId).join(',')).split(',').map(Number);
const profiles = option('--profiles', 'mixed,fusion').split(',');
const directory = path.resolve(root, option('--output', 'tmp/balance/campaign'));
fs.mkdirSync(directory, { recursive: true });

function party(stage, profile) {
  const capacity = 3 + [5, 10, 20, 30, 35].filter(gate => gate < stage).length;
  let ids = [10, 1, 20];
  if (stage > 5) ids = [13, 10, 17, 20];
  if (stage > 10) ids = [13, 10, 11, 17, 20];
  if (stage > 20) ids = [13, 10, 11, 17, 20, 27];
  if (stage > 30) ids = [13, 14, 10, 11, 17, 20, 27];
  if (stage > 35) ids = [13, 14, 10, 11, 17, 20, 27, 31];
  if (profile === 'ranged' && stage > 5) ids = [13, 10, ...Array(capacity - 3).fill(stage > 10 ? 18 : 17), 20];
  if (['spear', 'sentinel'].includes(profile) && stage > 5) {
    ids = [11, 11, 17, 20];
    if (stage > 10) ids = [13, 11, 11, 17, 20];
    if (stage > 20) ids = [13, 11, 11, 18, 20, 27];
    if (stage > 30) ids.push(18);
    if (stage > 35) ids.push(31);
    if (profile === 'sentinel' && stage > 20) {
      ids.splice(ids.indexOf(13), 1); ids.splice(ids.indexOf(11), 1); ids.unshift(-1004);
    }
  }
  // Same material budget as mixed; manufacture only the resulting form, using
  // the runtime factory. Adjacency/selection already has separate E2E coverage.
  if (profile === 'fusion' && stage > 20) {
    ids.splice(ids.indexOf(13), 1); ids.splice(ids.indexOf(10), 1); ids.unshift(-1003);
  }
  return ids;
}

function summary(rows) {
  const tagged = rows.some(row => row.variant !== undefined);
  const measuredVariants = [...new Set(rows.map(row => row.variant || 'baseline'))];
  const measuredStages = [...new Set(rows.map(row => row.stage))].sort((a, b) => a - b);
  const measuredProfiles = [...new Set(rows.map(row => row.profile))];
  return measuredStages.flatMap(stage => measuredProfiles.flatMap(profile => measuredVariants.map(variant => {
    const samples = rows.filter(r => r.stage === stage && r.profile === profile && (r.variant || 'baseline') === variant);
    if (!samples.length) return null;
    return { stage, profile, ...(tagged ? { variant } : {}), samples: samples.length, wins: samples.filter(r => r.won).length,
      meanTurns: +(samples.reduce((sum, r) => sum + r.turns, 0) / samples.length).toFixed(1),
      timeouts: samples.filter(r => !r.won && r.turns === 20 && r.allyHP > 0 && r.enemyHP > 0).length };
  }))).filter(Boolean);
}

function validateResults(rows, jobs, source = { characters, skills }) {
  if (jobs) assert.equal(rows.length, jobs.length, '測定が途中で終了しています。');
  rows.forEach((row, index) => {
    assert(variantNames.includes(row.variant || 'baseline'), '未知の比較条件です。');
    assert(Array.isArray(row.initialUnits), '実際の加入・レベルを検証していない旧測定です。再測定してください。');
    assert.equal(row.initialUnits.length, row.party.length);
    for (let i = 0; i < row.initialUnits.length; i++) {
      const unit = row.initialUnits[i];
      assert.equal(unit.id, row.party[i]);
      assert.equal(unit.level, row.level, '未加入の駒がLv1で測定されています。');
      assert(unit.hp > 0 && unit.attack >= 0 && unit.defense >= 0);
      if (unit.id > 0) {
        const data = source.characters.find(c => c.id === unit.id);
        assert(data);
        const hp = data.baseHP + data.hpGrowth * (row.level - 1);
        assert.equal(unit.hp, row.variant === 'shield90' && unit.id === 13 ? Math.floor(hp * 9 / 10) : hp);
        assert.equal(unit.attack, data.baseAttack + data.attackGrowth * (row.level - 1));
        assert.equal(unit.defense, data.baseDefense + data.defenseGrowth * (row.level - 1));
        if (row.variant !== undefined) {
          let multiplier = source.skills.find(skill => skill.skillTypeName === data.skillType)?.powerMultiplier ?? 1;
          if (data.skillType === 'Gun' && row.variant.startsWith('gun')) multiplier *= Number(row.variant.slice(3)) / 100;
          assert(Number.isFinite(unit.roleMultiplier) && Math.abs(unit.roleMultiplier - multiplier) < .00001,
            '比較条件と実際のスキル倍率が一致しません。');
        }
      }
    }
    if (row.economyEntry) {
      assert.equal(row.level, row.economyEntry.level);
      const materials = row.initialUnits.flatMap(unit => {
        assert(Array.isArray(unit.materials), '素材の所持確認ができない旧測定です。');
        return unit.materials;
      });
      assert(materials.length <= row.economyEntry.slots);
      assert(materials.every(id => row.economyEntry.owned.includes(id)), '購入していない素材を使用しています。');
    }
    if (jobs) {
      assert.equal(row.stage, jobs[index].stage);
      assert.equal(row.seed, jobs[index].seed);
      assert.equal(row.profile, jobs[index].profile);
      assert.equal(row.variant || 'baseline', jobs[index].variant || 'baseline');
    }
  });
}

async function main() {
  if (args.includes('--summary')) {
    const rows = JSON.parse(fs.readFileSync(path.join(directory, 'results.json')));
    const context = path.join(directory, 'context.json');
    validateResults(rows, undefined, fs.existsSync(context) ? JSON.parse(fs.readFileSync(context)) : undefined);
    console.table(summary(rows)); return;
  }
  assert(Number.isInteger(seeds) && seeds >= 1 && seeds <= 100);
  assert(selected.length && new Set(selected).size === selected.length && selected.every(id => stages.some(s => s.stageId === id)));
  assert(profiles.every(profile => ['mixed', 'fusion', 'ranged', 'spear', 'sentinel'].includes(profile)));
  assert(variants.length && new Set(variants).size === variants.length && variants.every(variant => variantNames.includes(variant)));
  assert(!variants.includes('shield90') || profiles.every(profile => !['fusion', 'sentinel'].includes(profile)),
    '盾HPの比較は通常盾だけを変更し、融合は対象にしません。非融合編成で比較してください。');
  const economyWins = Number(option('--economy-wins', 0));
  assert([0, 1, 3, 5].includes(economyWins));
  assert(!economyWins || !args.includes('--upgrades'), '購入済み強化と全強化を同時指定できません。');
  const playthrough = args.includes('--playthrough');
  const farmWins = Number(option('--farm-wins', 3));
  const layout = option('--layout', 'compact');
  assert(['compact', 'staggered', 'adaptive'].includes(layout));
  assert(!playthrough || (profiles.length === 1 && variants.length === 1 && !economyWins && !args.includes('--upgrades') && seeds === 1));
  assert(!playthrough || (selected.length === 40 && selected.every((id, index) => id === index + 1)));
  assert(Number.isInteger(farmWins) && farmWins >= 1 && farmWins <= 5);
  const campaignRef = option('--campaign-ref');
  const campaignFile = option('--campaign-file');
  assert(!(campaignRef && campaignFile));
  const campaign = campaignFile ? JSON.parse(fs.readFileSync(path.resolve(root, campaignFile), 'utf8'))
    : campaignRef ? JSON.parse(execFileSync('git', ['show', `${campaignRef}:Assets/Data/campaign-plan.json`], { cwd: root, encoding: 'utf8' })) : null;
  const jobs = selected.flatMap(id => profiles.flatMap(profile => variants.flatMap(variant => Array.from({ length: seeds }, (_, seed) => ({
    stage: id, level: Math.min(50, Math.max(3, stages.find(s => s.stageId === id).enemyLevel + Number(option('--level-offset', 0)))), seed: 1729 + seed * 7919,
    profile, variant, party: party(id, profile), upgrades: args.includes('--upgrades'), economyWins, playthrough, farmWins, layout,
    ...(campaign ? { enemyIds: campaign[id - 1].enemies, enemyLevel: campaign[id - 1].level, enemyPositions: campaign[id - 1].positions } : {})
  })))));
  fs.writeFileSync(path.join(directory, 'plan.json'), JSON.stringify(jobs, null, 2));
  fs.writeFileSync(path.join(directory, 'context.json'), JSON.stringify({ schema: 1,
    gitHead: execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(),
    arguments: args, characters, skills, stages, campaign }, null, 2));
  if (args.includes('--plan-only')) return;
  const unity = process.env.UNITY_EDITOR || '/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity';
  console.log(`無音実戦測定: ${playthrough ? `表40局の通し攻略・各局最大${farmWins}勝／7試行` : `${jobs.length}戦・${economyWins ? `各局${economyWins}勝仮定の購入済み育成` : '基準レベル・進行収支は別測定'}`}。ログ: ${directory}/unity.log`);
  await new Promise((resolve, reject) => {
    const child = spawn(unity, ['-batchmode', '-nographics', '-noaudio', '-projectPath', root,
      '-executeMethod', 'CampaignBalanceRunner.Run', '-balancePlan', path.join(directory, 'plan.json'),
      '-balanceOutput', path.join(directory, 'results.json'), '-logFile', path.join(directory, 'unity.log')],
    { cwd: root, stdio: 'ignore' });
    child.on('error', reject);
    child.on('exit', code => code === 0 ? resolve() : reject(new Error(`Unity exit ${code}; ${directory}/unity.log`)));
  });
  const rows = JSON.parse(fs.readFileSync(path.join(directory, 'results.json')));
  validateResults(rows, playthrough ? undefined : jobs);
  if (playthrough) {
    const campaign = JSON.parse(fs.readFileSync(path.join(directory, 'campaign.json')));
    assert(campaign.every((row, index) => row.stage === index + 1 && row.points >= 0
      && row.slots >= 3 && row.slots <= 8 && row.attempts >= row.wins && row.attempts <= 7));
    console.log('通し攻略:', campaign.at(-1));
    if (args.includes('--require-clear')) assert.equal(campaign.at(-1)?.stage === 40 && campaign.at(-1).wins > 0, true, '表40局を完走できませんでした。');
  }
  const report = summary(rows);
  fs.writeFileSync(path.join(directory, 'summary.json'), JSON.stringify(report, null, 2));
  console.table(report);
}
if (require.main === module) main().catch(error => { console.error(error); process.exitCode = 1; });
module.exports = { party, summary, validateResults };
