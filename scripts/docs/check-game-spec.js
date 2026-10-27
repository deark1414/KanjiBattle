const fs=require('node:fs');
const path=require('node:path');
const assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const read=file=>fs.readFileSync(path.join(root,file),'utf8');
const directory='Docs/GameDesign';
const files=fs.readdirSync(path.join(root,directory)).filter(file=>file.endsWith('.md')).sort();
assert.deepEqual(files,['ContentCatalog.md','FutureDesign.md','GameRules.md']);
const catalog=read(directory+'/ContentCatalog.md');
const characters=JSON.parse(read('Assets/Data/characters.json'));
const tree=read('Assets/Scripts/Game/CastleTree.cs');
const ids=[...tree.matchAll(/\bC\((\d+),/g)].map(match=>Number(match[1]));
const documented=[...catalog.split('### 防御')[0].matchAll(/^\|([^|]+)\|(\d+)\|/gm)].map(match=>({name:match[1],id:Number(match[2])}));
assert.deepEqual(documented.map(c=>c.id).sort((a,b)=>a-b),ids.sort((a,b)=>a-b));
for(const row of documented)assert.equal(characters.find(c=>c.id===row.id)?.characterName,row.name);
const recipes=[...read('Assets/Scripts/Game/Rules/BattleFusionRules.cs').matchAll(/new Recipe\(-\d+, "([^"]+)"[^\n]+?, (?:true|false), ([^\n]+)\)/g)]
  .map(match=>({name:match[1],materials:[...match[2].matchAll(/"([^"]+)"/g)].map(m=>m[1])}));
assert.equal(recipes.length,17,'Update the catalog check when adding a fusion');
for(const recipe of recipes)assert(catalog.includes(`|${recipe.name}|${recipe.materials.join('＋')}|`),recipe.name+' recipe differs');
const stages=JSON.parse(read('Assets/Data/stages.json')),campaign=JSON.parse(read('Assets/Data/campaign-plan.json'));
assert.equal(stages.length,40);assert.equal(campaign.length,40);
stages.forEach((stage,i)=>{assert.deepEqual(stage.enemyIds,campaign[i].enemies);assert.equal(stage.enemyLevel,campaign[i].level);});
for(const file of ['Docs/README.md',...files.map(file=>directory+'/'+file)]){
  for(const match of read(file).matchAll(/\[[^\]]*\]\(([^)]+)\)/g)){
    const target=match[1].split('#')[0];
    if(target&&!/^https?:/.test(target))assert(fs.existsSync(path.resolve(root,path.dirname(file),target)),file+': '+target);
  }
}
console.log(`正本3冊・通常${ids.length}種・融合${recipes.length}種・表${stages.length}局・リンク整合: 成功`);
