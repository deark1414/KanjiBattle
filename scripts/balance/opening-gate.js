const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { spawn, execFileSync } = require('node:child_process');
const { validateResults } = require('./campaign-balance');
const { group } = require('./roster-comparison');
const root = path.resolve(__dirname, '../..');
const cells = coordinates => coordinates.map(([x,y]) => ({x,y}));
const scenarios = [
  { key:'dice-guards', ids:[1,2,-1011], level:4, positions:cells([[2,1],[3,1],[2,0]]) },
  { key:'solo', ids:[-1011], level:4, positions:cells([[2,1]]) },
  { key:'solo-low', ids:[-1011], level:1, positions:cells([[2,1]]) },
  { key:'pair-low', ids:[1,-1011], level:1, positions:cells([[2,1],[2,0]]) }
];
const numberScenarios = [
  { key:'numbers-three', ids:[1,2,3], level:5, positions:cells([[2,1],[3,1],[2,0]]) },
  { key:'numbers-four', ids:[1,2,3,2], level:4, positions:cells([[2,1],[3,1],[2,0],[1,1]]) }
];
function makePlan(seeds=5, numbers=false) {
  assert(Number.isInteger(seeds)&&seeds>=1&&seeds<=10);
  return (numbers?numberScenarios:scenarios).flatMap(scenario=>[[10,1,20],[10,13,20],[10,10,20],[13,13,20]].flatMap(party=>
    Array.from({length:seeds},(_,i)=>({stage:5,level:5,seed:1729+i*7919,
      profile:`${scenario.key}/${party.join('-')}`,variant:'baseline',layout:'adaptive',party,
      economyWins:3,upgrades:false,enemyIds:scenario.ids,enemyLevel:scenario.level,enemyPositions:scenario.positions}))));
}
async function main() {
  const args=process.argv.slice(2);
  const option=(key,fallback)=>args.includes(key)?args[args.indexOf(key)+1]:fallback;
  const directory=path.resolve(root,option('--output','tmp/balance/opening-gate'));
  fs.mkdirSync(directory,{recursive:true});
  const jobs=makePlan(Number(option('--seeds',5)),args.includes('--numbers'));
  const context={gitHead:execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),arguments:args,
    characters:JSON.parse(fs.readFileSync(path.join(root,'Assets/Data/characters.json'))),
    stages:JSON.parse(fs.readFileSync(path.join(root,'Assets/Data/stages.json'))),
    skills:JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/skills.json')))};
  fs.writeFileSync(path.join(directory,'plan.json'),JSON.stringify(jobs,null,2));
  fs.writeFileSync(path.join(directory,'context.json'),JSON.stringify(context,null,2));
  console.log(`第5局の有料加入・育成条件で無音比較: ${jobs.length}戦`);
  await new Promise((resolve,reject)=>{
    const child=spawn(process.env.UNITY_EDITOR||'/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity',
      ['-batchmode','-nographics','-noaudio','-projectPath',root,'-executeMethod','CampaignBalanceRunner.Run',
        '-balancePlan',path.join(directory,'plan.json'),'-balanceOutput',path.join(directory,'results.json'),
        '-logFile',path.join(directory,'unity.log')],{cwd:root,stdio:'ignore'});
    child.on('error',reject);child.on('exit',code=>code===0?resolve():reject(new Error(`Unity exit ${code}`)));
  });
  const rows=JSON.parse(fs.readFileSync(path.join(directory,'results.json')));
  validateResults(rows,jobs,context);
  rows.forEach((row,i)=>{
    assert.deepEqual(row.party,jobs[i].party,'Requested opening party was not affordable');
    assert.deepEqual(row.initialEnemies.map(u=>u.id),jobs[i].enemyIds);
    assert(row.initialEnemies.every(u=>u.level===jobs[i].enemyLevel));
  });
  const report=[...new Set(jobs.map(j=>j.profile))].map(profile=>({profile,...group(rows.filter(r=>r.profile===profile))}));
  fs.writeFileSync(path.join(directory,'summary.json'),JSON.stringify(report,null,2));
  console.table(report);
}
if(require.main===module)main().catch(error=>{console.error(error);process.exitCode=1;});
module.exports={makePlan};
