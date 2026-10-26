const fs = require('node:fs');
const path = require('node:path');
const { spawn, execFileSync } = require('node:child_process');
const assert = require('node:assert/strict');
const { validateResults } = require('./campaign-balance');
const root = path.resolve(__dirname, '../..');

function group(rows) {
  const wins = rows.filter(row => row.won);
  return { fights: rows.length, wins: wins.length,
    turns: rows.reduce((sum, row) => sum + row.turns, 0) / rows.length,
    remaining: rows.reduce((sum, row) => sum + row.allyHP / row.initialUnits.reduce((hp, unit) => hp + unit.hp, 0), 0) / rows.length,
    timeouts: rows.filter(row => !row.won && row.turns === 20 && row.allyHP > 0 && row.enemyHP > 0).length };
}
function contribution(rows, name) {
  const totals = { damage: 0, healed: 0, guarded: 0, prevented: 0 };
  for (const row of rows) for (const entry of row.contributions || [])
    if (!name || entry.name === name) for (const key of Object.keys(totals)) totals[key] += entry[key] || 0;
  return Object.fromEntries(Object.entries(totals).map(([key, value]) => [key, value / rows.length]));
}
function analyze(rows, manifest) {
  const stageIds = manifest.stages || [...new Set(rows.map(row => row.stage))];
  const normal = manifest.characters.map(character => {
    const samples = rows.filter(row => row.profile === 'normal/' + character.id);
    return { ...character, ...group(samples), armyContribution: contribution(samples),
      stages: stageIds.map(stage => ({ stage, ...group(samples.filter(row => row.stage === stage)) })) };
  });
  const fusions = manifest.recipes.map(recipe => {
    const controls = rows.filter(row => row.profile === `fusion/${recipe.id}/materials`);
    const fused = rows.filter(row => row.profile === `fusion/${recipe.id}/fused`);
    let better = 0, worse = 0;
    for (const row of fused) {
      const control = controls.find(other => other.stage === row.stage && other.seed === row.seed);
      assert(control, 'Missing paired material battle');
      if (row.won && !control.won) better++;
      if (!row.won && control.won) worse++;
    }
    return { ...recipe, materialsResult: group(controls), fusedResult: group(fused), better, worse,
      focusContribution: contribution(fused, recipe.name),
      stages: stageIds.map(stage => ({ stage, materialsResult: group(controls.filter(row => row.stage === stage)),
        fusedResult: group(fused.filter(row => row.stage === stage)) })) };
  });
  return { normal, fusions, enemies: stageIds.map(stage => ({ stage,
    levels: [...new Set(rows.filter(row=>row.stage===stage).flatMap(row=>(row.initialEnemies||[]).map(unit=>unit.level)))].sort((a,b)=>a-b) })) };
}
function validateComparison(rows, jobs, manifest, source) {
  validateResults(rows, jobs, source);
  assert([undefined, 'projectile-probe', 'origin-probe', 'guard-probe', 'fusion-probe'].includes(manifest.kind));
  if (!manifest.kind) {
    assert.equal(manifest.characters.length, 26);
    assert.equal(manifest.recipes.length, 17);
  }
  const profiles = manifest.characters.length + manifest.recipes.length * 2;
  assert.equal(new Set(jobs.map(job => job.profile)).size, profiles);
  assert.equal(rows.length, profiles * manifest.stages.length * manifest.seeds);
  assert.equal(new Set(rows.map(row=>`${row.profile}/${row.stage}/${row.seed}`)).size, rows.length,
    'Duplicate comparison cases must not replace missing battles');
  for (const profile of new Set(jobs.map(job=>job.profile))) for (const stage of manifest.stages)
    assert.equal(rows.filter(row=>row.profile===profile&&row.stage===stage).length, manifest.seeds,
      'Each profile must cover every selected stage');
  for (let i = 0; i < rows.length; i++) {
    const row = rows[i];
    assert(row.comparison && row.level === manifest.level && !row.upgrades && !row.economyEntry);
    assert.equal(row.initialUnits.reduce((count, unit) => count + unit.materials.length, 0), 8);
    assert.deepEqual(row.initialUnits.map(unit => ({ x: unit.x, y: unit.y })),
      jobs[i].allyPositions.map(cell => ({ x: cell.x, y: cell.y })),
      'Saved comparison positions must match the actual deployment');
  }
  for (const recipe of manifest.recipes) {
    for (const row of rows.filter(row => row.profile === `fusion/${recipe.id}/fused`)) {
      assert.equal(row.initialUnits[0].id, recipe.id);
      assert.deepEqual([...row.initialUnits[0].materials].sort((a,b)=>a-b), [...recipe.materials].sort((a,b)=>a-b));
      const control = rows.find(other => other.profile === `fusion/${recipe.id}/materials`
        && other.stage === row.stage && other.seed === row.seed);
      assert(control);
      const common = control.initialUnits.slice(recipe.materials.length);
      assert.deepEqual(row.initialUnits.slice(1).map(unit => [unit.id,unit.x,unit.y]), common.map(unit => [unit.id,unit.x,unit.y]),
        'Common supports must remain in the same positions across fusion pairs');
    }
  }
}
function markdown(report, manifest) {
  const n = value => value.toFixed(1);
  return ['# 通常キャラ・融合の比較調査', '',
    `条件: 共通Lv${manifest.level}、個別・カテゴリ強化なし、素材8枠、取得費用を除外して出陣キャラを加入済みにする、第${manifest.stages.join('/')}局、各${manifest.seeds}シード。`,
    ...(manifest.enemyLevel ? [`追加検査の敵レベルは${manifest.enemyLevel}に固定。実ステージの初回到達条件ではない。`] : []),
    ...(manifest.kind === 'origin-probe' ? ['融合の起点は素材の最前列・同段左。役割別に前後列へ置いた一次調査との配置感度を調べる。'] : []),
    ...(manifest.oxRear ? ['丑だけ後列(4,6)へ移す。共通7体の配置は射撃対策の追加比較と同じ。'] : []),
    '敵レベル: '+report.enemies.map(row=>`第${row.stage}局 ${row.levels.join('/')}`).join('、')+'。',
    '進行収支・初回到達の再現ではなく、固定した混成軍勢への採用比較。全勝率の推定や全編成の順位表ではない。',
    '融合は同じ素材枠数と共通の支援駒を使い、素材を個別運用する場合と比較する。共通駒の初期位置を保持する。'
      +(manifest.kind === 'origin-probe' ? '完成形は素材の最前列・同段左へ置く。' : '完成形は役割に応じて空いた前列／後列へ置く。')
      +'素材の隣接禁止による配置差は残る。',
    '通常キャラは共通7体へ1体追加する比較。基準の剣を含め、役割・配置の相性を含む採用価値の一次調査であり、個体の絶対的な優劣とはしない。',
    '残HP率には砂・草などが生成したHPも含むため、通常の耐久率や融合前後の倍率として解釈しない。平均ターンは敗北・時間切れを含む。', '',
    `## 通常${manifest.characters.length}種`, '',
    `|追加キャラ|${manifest.stages.map(stage=>stage+'局').join('|')}|計|平均終了ターン|時間切れ|`,
    `|---|${manifest.stages.map(()=> '---:').join('|')}|---:|---:|---:|`,
    ...report.normal.map(row => `|${row.name}|${row.stages.map(stage=>stage.wins+'/'+stage.fights).join('|')}|${row.wins}/${row.fights}|${n(row.turns)}|${row.timeouts}|`), '',
    `## 通常融合${manifest.recipes.length}種`, '', '|融合|素材個別の勝利|融合後の勝利|勝敗改善/悪化ペア|平均ターン 素材→融合|', '|---|---:|---:|---:|---:|',
    ...report.fusions.map(row => `|${row.name}|${row.materialsResult.wins}/${row.materialsResult.fights}|${row.fusedResult.wins}/${row.fusedResult.fights}|${row.better}/${row.worse}|${n(row.materialsResult.turns)}→${n(row.fusedResult.turns)}|`), '',
    '## 融合の平均成績', '', '|融合|与ダメージ|回復|肩代わり|軽減・防止|', '|---|---:|---:|---:|---:|',
    ...report.fusions.map(row => `|${row.name}|${n(row.focusContribution.damage)}|${n(row.focusContribution.healed)}|${n(row.focusContribution.guarded)}|${n(row.focusContribution.prevented)}|`), '',
    '## 留意点', '', '実際の戦闘・地形・移動・スキル処理を使用。セーブと音声設定は隔離し無音で実行する。演出と戦闘が乱数を共有するため、同じシードも行動ごとの同一乱数を保証しない。',
    '与ダメージ・回復・軽減の元記録はresults.jsonのcontributionsを参照。軽減・防止には基礎防御も含む。鈴の再発動・鍛冶の強化による追加ダメージは攻撃者側へ計上され、移動補助・引き寄せの価値も本人の成績には直接表れない。強化後、別の支援構成、別の初期配置、レベル差での再確認が必要。', ''].join('\n');
}
async function main() {
  const args = process.argv.slice(2);
  const option = (name, fallback) => args.includes(name) ? args[args.indexOf(name) + 1] : fallback;
  const directory = path.resolve(root, option('--output', 'tmp/balance/roster-comparison'));
  fs.mkdirSync(directory, { recursive: true });
  const read = file => JSON.parse(fs.readFileSync(path.join(directory, file)));
  if (!args.includes('--summary')) {
    const seeds = Number(option('--seeds', 3));
    assert(Number.isInteger(seeds) && seeds >= 1 && seeds <= 10);
    const guardProbe = args.includes('--guard-probe');
    const fusionProbe = args.includes('--fusion-probe');
    const probe = args.includes('--projectile-probe') || args.includes('--origin-probe') || guardProbe || fusionProbe;
    if (probe) {
      const source = path.resolve(root, option('--source', 'tmp/balance/roster-screen'));
      assert.notEqual(source, directory, 'Do not overwrite the original comparison');
      const sourceManifest = JSON.parse(fs.readFileSync(path.join(source, 'manifest.json')));
      const sourceJobs = JSON.parse(fs.readFileSync(path.join(source, 'plan.json')));
      const stages = JSON.parse(fs.readFileSync(path.join(root, 'Assets/Data/stages.json')));
      const stage = stages.find(stage => stage.stageId === 25);
      const originProbe = args.includes('--origin-probe');
      const rearOx = args.includes('--rear-ox');
      const ids = fusionProbe ? [] : guardProbe ? [13,14,15,30] : originProbe ? [] : rearOx ? [30] : [10, 13, 14, 15, 30];
      const selected = sourceManifest.recipes.filter(recipe => {
        if (fusionProbe) return ['磁石','雨','賽子'].includes(recipe.name);
        if (guardProbe) return false;
        if (!originProbe) return !rearOx && recipe.id === -1017;
        const control = sourceJobs.find(job => job.profile === `fusion/${recipe.id}/materials`);
        const fused = sourceJobs.find(job => job.profile === `fusion/${recipe.id}/fused`);
        const origin = control.allyPositions.slice(0, recipe.materials.length).sort((a,b)=>a.y-b.y||a.x-b.x)[0];
        return origin.x !== fused.allyPositions[0].x || origin.y !== fused.allyPositions[0].y;
      });
      const manifest = { ...sourceManifest, kind: fusionProbe ? 'fusion-probe' : guardProbe ? 'guard-probe' : originProbe ? 'origin-probe' : 'projectile-probe',
        stages: guardProbe || fusionProbe ? [35,36,40] : originProbe ? [36,40] : [25], seeds, ...(originProbe || guardProbe || fusionProbe ? {} : { enemyLevel: 33 }),
        characters: sourceManifest.characters.filter(character => ids.includes(character.id)), recipes: selected,
        ...(rearOx ? { oxRear: true } : {}) };
      const profiles = [...ids.map(id => 'normal/' + id), ...selected.flatMap(recipe =>
        [`fusion/${recipe.id}/materials`, `fusion/${recipe.id}/fused`])];
      const fixed = [[2,5],[0,5],[4,5],[1,6],[3,6],[0,6],[5,6],[5,5]].map(([x,y])=>({x,y}));
      if (rearOx) fixed[0] = { x: 4, y: 6 };
      const jobs = profiles.flatMap(profile => manifest.stages.flatMap(stageId => Array.from({ length: seeds }, (_, index) => {
        const job = structuredClone(sourceJobs.find(job => job.profile === profile));
        assert(job, 'Missing source formation');
        if (guardProbe || fusionProbe) return { ...job, stage: stageId, seed: 1729 + index * 7919 };
        if (originProbe) {
          if (profile.endsWith('/fused')) {
            const recipe = selected.find(recipe => profile === `fusion/${recipe.id}/fused`);
            const control = sourceJobs.find(job => job.profile === `fusion/${recipe.id}/materials`);
            job.allyPositions[0] = control.allyPositions.slice(0, recipe.materials.length).sort((a,b)=>a.y-b.y||a.x-b.x)[0];
          }
          return { ...job, stage: stageId, seed: 1729 + index * 7919 };
        }
        return { ...job, stage: 25, seed: 1729 + index * 7919, enemyLevel: 33,
          enemyIds: stage.enemyIds, enemyPositions: stage.enemyPositions,
          allyPositions: profile.startsWith('normal/') ? fixed : job.allyPositions };
      })));
      fs.writeFileSync(path.join(directory, 'plan.json'), JSON.stringify(jobs, null, 2));
      fs.writeFileSync(path.join(directory, 'manifest.json'), JSON.stringify(manifest, null, 2));
    } else fs.writeFileSync(path.join(directory, 'plan.json'), '[]');
    const context = { gitHead: execFileSync('git', ['rev-parse', 'HEAD'], { cwd: root, encoding: 'utf8' }).trim(),
      characters: JSON.parse(fs.readFileSync(path.join(root, 'Assets/Data/characters.json'))),
      skills: JSON.parse(fs.readFileSync(path.join(root, 'Assets/Resources/skills.json'))) };
    fs.writeFileSync(path.join(directory, 'context.json'), JSON.stringify(context, null, 2));
    console.log(probe ? `追加比較: ${fusionProbe ? '磁石・雨・賽子' : guardProbe ? '終盤の守備役' : args.includes('--origin-probe') ? '融合の通常起点' : '射撃対策・敵Lv33'}・${seeds}シード。`
      : `無音の比較調査: 通常26種・融合17種の素材個別/融合後、各3局×${seeds}シード。`);
    await new Promise((resolve, reject) => {
      const child = spawn(process.env.UNITY_EDITOR || '/Applications/Unity/Hub/Editor/6000.4.4f1/Unity.app/Contents/MacOS/Unity',
        ['-batchmode', '-nographics', '-noaudio', '-projectPath', root, '-executeMethod', 'CampaignBalanceRunner.Run',
          '-balanceComparison', probe ? '0' : '1', '-comparisonSeeds', String(seeds), '-balancePlan', path.join(directory, 'plan.json'),
          '-balanceOutput', path.join(directory, 'results.json'), '-logFile', path.join(directory, 'unity.log')], { cwd: root, stdio: 'ignore' });
      child.on('error', reject); child.on('exit', code => code === 0 ? resolve() : reject(new Error(`Unity exit ${code}: ${directory}/unity.log`)));
    });
  }
  const rows = read('results.json'), manifest = read('manifest.json');
  validateComparison(rows, read('plan.json'), manifest, read('context.json'));
  const report = analyze(rows, manifest);
  fs.writeFileSync(path.join(directory, 'summary.json'), JSON.stringify(report, null, 2));
  fs.writeFileSync(path.join(directory, 'report.md'), markdown(report, manifest));
  console.table(report.fusions.map(row => ({ name: row.name, materials: row.materialsResult.wins,
    fused: row.fusedResult.wins, better: row.better, worse: row.worse })));
  console.log(path.join(directory, 'report.md'));
}
if (require.main === module) main().catch(error => { console.error(error); process.exitCode = 1; });
module.exports = { group, analyze, validateComparison, markdown };
