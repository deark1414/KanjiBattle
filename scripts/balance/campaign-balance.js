const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..');
const args = process.argv.slice(2);
const option = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
const stages = JSON.parse(fs.readFileSync(path.join(root, 'Assets/Data/stages.json')));
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
  if (profile === 'ranged' && stage > 5) ids = [13, 10, ...Array(capacity - 3).fill(17), 20];
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
  return selected.flatMap(stage => profiles.map(profile => {
    const samples = rows.filter(r => r.stage === stage && r.profile === profile);
    if (!samples.length) return null;
    return { stage, profile, samples: samples.length, wins: samples.filter(r => r.won).length,
      meanTurns: +(samples.reduce((sum, r) => sum + r.turns, 0) / samples.length).toFixed(1),
      timeouts: samples.filter(r => !r.won && r.turns === 20 && r.allyHP > 0 && r.enemyHP > 0).length };
  })).filter(Boolean);
}

async function main() {
  if (args.includes('--summary')) {
    console.table(summary(JSON.parse(fs.readFileSync(path.join(directory, 'results.json'))))); return;
  }
  const jobs = selected.flatMap(id => profiles.flatMap(profile => Array.from({ length: seeds }, (_, seed) => ({
    stage: id, level: Math.max(3, stages.find(s => s.stageId === id).enemyLevel + Number(option('--level-offset', 0))), seed: 1729 + seed * 7919,
    profile, party: party(id, profile), upgrades: args.includes('--upgrades')
  }))));
  fs.writeFileSync(path.join(directory, 'plan.json'), JSON.stringify(jobs, null, 2));
  if (args.includes('--plan-only')) return;
  const unity = process.env.UNITY_EDITOR || '/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity';
  console.log(`実戦測定: ${jobs.length}戦。進行・加入費用は別測定。ログ: ${directory}/unity.log`);
  await new Promise((resolve, reject) => {
    const child = spawn(unity, ['-batchmode', '-nographics', '-noaudio', '-projectPath', root,
      '-executeMethod', 'CampaignBalanceRunner.Run', '-balancePlan', path.join(directory, 'plan.json'),
      '-balanceOutput', path.join(directory, 'results.json'), '-logFile', path.join(directory, 'unity.log')],
    { cwd: root, stdio: 'ignore' });
    child.on('error', reject);
    child.on('exit', code => code === 0 ? resolve() : reject(new Error(`Unity exit ${code}; ${directory}/unity.log`)));
  });
  const report = summary(JSON.parse(fs.readFileSync(path.join(directory, 'results.json'))));
  fs.writeFileSync(path.join(directory, 'summary.json'), JSON.stringify(report, null, 2));
  console.table(report);
}
if (require.main === module) main().catch(error => { console.error(error); process.exitCode = 1; });
module.exports = { party, summary };
