const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { spawn, execFileSync } = require('node:child_process');
const { validateResults } = require('./campaign-balance');
const { group } = require('./roster-comparison');
const root = path.resolve(__dirname, '../..');
const cells = coordinates => coordinates.map(([x,y]) => ({x,y}));
const scenarios = [
  { key:'clustered', name:'密集射撃', ids:[13,13,17,18,20,31,27,10],
    positions:cells([[2,1],[3,1],[2,0],[3,0],[1,0],[4,0],[0,0],[4,1]]) },
  { key:'spread', name:'分散射撃', ids:[13,13,17,18,20,31,27,10],
    positions:cells([[0,1],[5,1],[0,0],[5,0],[2,0],[3,1],[2,1],[3,0]]) },
  { key:'rush', name:'動物の速攻', ids:[25,25,23,23,13,10,11,20],
    positions:cells([[0,1],[5,1],[1,1],[4,1],[2,1],[3,1],[2,0],[3,0]]) },
  { key:'mirror', name:'敵も磁石と雨', ids:[13,10,17,20,-1007,-1012],
    positions:cells([[2,1],[3,1],[0,0],[5,0],[1,0],[4,0]]) }
];

function makePlan(sourceJobs, manifest, seeds=3) {
  assert(Number.isInteger(seeds) && seeds>=1 && seeds<=10);
  const forms=[];
  for(const name of ['磁石','雨','散弾']) {
    const recipe=manifest.recipes.find(recipe=>recipe.name===name);
    assert(recipe && recipe.materials.length===2);
    for(const form of ['materials','single',...(name==='散弾'?[]:['double'])]) {
      const original=sourceJobs.find(job=>job.profile===`fusion/${recipe.id}/${form==='materials'?'materials':'fused'}`);
      assert(original);
      forms.push({name,form,recipe,job:{...structuredClone(original),
        ...(form==='double'?{party:[recipe.id,recipe.id,13,10,20,27],
          allyPositions:cells([[2,6],[4,6],[0,5],[2,5],[0,6],[4,5]])}:{}),
        stage:1,level:33,comparison:true,upgrades:false,economyWins:0}});
    }
  }
  return scenarios.flatMap(scenario=>forms.flatMap(({name,form,job})=>
    Array.from({length:seeds},(_,i)=>({...structuredClone(job),profile:`${scenario.key}/${name}/${form}`,
      seed:1729+i*7919,enemyLevel:33,enemyIds:scenario.ids,enemyPositions:scenario.positions}))));
}

function validateStress(rows,jobs,context) {
  validateResults(rows,jobs,context);
  assert.equal(new Set(rows.map(row=>row.profile+'/'+row.seed)).size,rows.length);
  rows.forEach((row,i)=>{
    const job=jobs[i];
    assert(row.comparison && !row.upgrades && row.level===33 && !row.economyEntry);
    assert.equal(row.initialUnits.reduce((sum,unit)=>sum+unit.materials.length,0),8);
    assert.deepEqual(row.initialUnits.map(unit=>({x:unit.x,y:unit.y})),job.allyPositions.map(({x,y})=>({x,y})));
    assert.deepEqual(row.initialEnemies.map(unit=>unit.id),job.enemyIds);
    assert.deepEqual(row.initialEnemies.map(unit=>({x:unit.x,y:unit.y})),job.enemyPositions.map(({x,y})=>({x,y})));
    assert(row.initialEnemies.every(unit=>unit.level===33));
    assert.equal(new Set(job.allyPositions.map(cell=>cell.x+','+cell.y)).size,job.party.length);
    assert(job.allyPositions.every(cell=>cell.x>=0&&cell.x<6&&cell.y>=5&&cell.y<=6));
    for(const unit of row.initialUnits.filter(unit=>unit.id<0)) {
      const recipe=context.recipes.find(recipe=>recipe.id===unit.id);
      assert(recipe);
      assert.deepEqual([...unit.materials].sort((a,b)=>a-b),[...recipe.materials].sort((a,b)=>a-b));
      assert(unit.materials.every(id=>context.characters.some(c=>c.id===id)));
    }
  });
}

function summarize(rows) {
  return scenarios.flatMap(scenario=>['磁石','雨','散弾'].map(name=>({scenario:scenario.name,name,
    results:['materials','single',...(name==='散弾'?[]:['double'])].map(form=>({form,
      ...group(rows.filter(row=>row.profile===`${scenario.key}/${name}/${form}`))}))})));
}

async function main() {
  const args=process.argv.slice(2);
  const option=(key,fallback)=>args.includes(key)?args[args.indexOf(key)+1]:fallback;
  const directory=path.resolve(root,option('--output','tmp/balance/fusion-stress'));
  fs.mkdirSync(directory,{recursive:true});
  const read=file=>JSON.parse(fs.readFileSync(path.join(directory,file)));
  if(!args.includes('--summary')) {
    const source=path.resolve(root,option('--source','tmp/balance/roster-screen'));
    const manifest=JSON.parse(fs.readFileSync(path.join(source,'manifest.json')));
    const jobs=makePlan(JSON.parse(fs.readFileSync(path.join(source,'plan.json'))),manifest,Number(option('--seeds',3)));
    const context={gitHead:execFileSync('git',['rev-parse','HEAD'],{cwd:root,encoding:'utf8'}).trim(),
      characters:JSON.parse(fs.readFileSync(path.join(root,'Assets/Data/characters.json'))),
      skills:JSON.parse(fs.readFileSync(path.join(root,'Assets/Resources/skills.json'))),recipes:manifest.recipes,scenarios};
    fs.writeFileSync(path.join(directory,'plan.json'),JSON.stringify(jobs,null,2));
    fs.writeFileSync(path.join(directory,'context.json'),JSON.stringify(context,null,2));
    console.log(`無音の対策・複数採用比較: ${jobs.length}戦。実ステージではなくLv33固定の検査戦場。`);
    await new Promise((resolve,reject)=>{
      const child=spawn(process.env.UNITY_EDITOR||'/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity',
        ['-batchmode','-nographics','-noaudio','-projectPath',root,'-executeMethod','CampaignBalanceRunner.Run',
          '-balancePlan',path.join(directory,'plan.json'),'-balanceOutput',path.join(directory,'results.json'),
          '-logFile',path.join(directory,'unity.log')],{cwd:root,stdio:'ignore'});
      child.on('error',reject);child.on('exit',code=>code===0?resolve():reject(new Error(`Unity exit ${code}: ${directory}/unity.log`)));
    });
  }
  const rows=read('results.json');validateStress(rows,read('plan.json'),read('context.json'));
  const report=summarize(rows);
  fs.writeFileSync(path.join(directory,'summary.json'),JSON.stringify(report,null,2));
  const n=value=>value.toFixed(1);
  const text=['# 融合の分散・速攻・複数採用検査','',
    '共通Lv33・強化なし・素材8枠。第1局を無罠の検査用土台とし、敵もLv33・指定編成へ差し替える。第1局の実攻略難度ではない。',
    '密集射撃と分散射撃は敵の種類・数が同じで配置だけを変える。敵も融合する検査は素材8枠を6体へ融合したもの。',
    '素材個別と1体融合は共通6体の位置を維持。2体融合は共通4体へ変更する同素材枠数の採用比較であり、他の駒を固定した単純な足し算ではない。',
    '最大育成・進行収支・全編成の勝率は未検証。演出と戦闘は乱数を共有する。','',
    '|敵編成|対象|素材個別 勝利/平均ターン|1体融合 勝利/平均ターン|2体融合 勝利/平均ターン|','|---|---|---:|---:|---:|',
    ...report.map(row=>`|${row.scenario}|${row.name}|${row.results.map(r=>`${r.wins}/${r.fights} / ${n(r.turns)}`).join('|')}${row.results.length===2?'|対象外':''}|`),''].join('\n');
  fs.writeFileSync(path.join(directory,'report.md'),text);
  console.log(text);
}
if(require.main===module)main().catch(error=>{console.error(error);process.exitCode=1;});
module.exports={makePlan,validateStress,summarize,scenarios};
