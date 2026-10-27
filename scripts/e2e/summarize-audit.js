const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');

function summarize(cases,reports){
  const attempts=new Map(cases.map(item=>[item.id,[]]));
  for(const report of [...reports].sort((a,b)=>Date.parse(a.startedAt)-Date.parse(b.startedAt))){
    assert.equal(new Set(report.results.map(r=>r.id)).size,report.results.length,'Duplicate cases in one run');
    for(const result of report.results){
      assert(attempts.has(result.id),'Unknown case: '+result.id);
      attempts.get(result.id).push({...result,source:report.source});
    }
  }
  return cases.map(item=>({id:item.id,name:item.name,status:attempts.get(item.id).at(-1)?.status||'missing',attempts:attempts.get(item.id)}));
}
function main(){
  const args=process.argv.slice(2),option=(key,fallback)=>args.includes(key)?args[args.indexOf(key)+1]:fallback;
  const files=option('--reports','').split(',').filter(Boolean).map(file=>path.resolve(file));
  assert(files.length,'Pass the result directories with --reports');
  const reports=files.map(directory=>({...JSON.parse(fs.readFileSync(path.join(directory,'report.json'))),source:directory}));
  const results=summarize(require('./scenarios'),reports);
  const output=path.resolve(option('--output','tmp/e2e/audit-summary'));
  fs.mkdirSync(output,{recursive:true});
  const report={results,passed:results.filter(r=>r.status==='passed').length,total:results.length};
  fs.writeFileSync(path.join(output,'report.json'),JSON.stringify(report,null,2));
  const escape=value=>String(value).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const html=['<!doctype html><html lang="ja"><meta charset="utf-8"><title>試遊版の検査</title>',
    '<style>body{font:16px sans-serif;max-width:1100px;margin:32px auto;padding:16px}table{border-collapse:collapse;width:100%}td,th{padding:10px;border-bottom:1px solid #ccc;text-align:left}pre{white-space:pre-wrap}</style>',
    `<h1>試遊版の検査</h1><p>${report.passed}/${report.total} 項目成功。再確認は最後の試行を採用し、元の失敗・中断も記録する。</p>`,
    '<table><tr><th>項目</th><th>最終結果</th><th>試行記録</th></tr>',
    ...results.map(r=>`<tr><td>${escape(r.name)}</td><td>${escape(r.status)}</td><td>${r.attempts.map((a,i)=>`<a href="${escape(path.relative(output,path.join(a.source,r.id,a.status==='passed'?'final.png':'failure.png')))}">${i+1}: ${escape(a.status)}</a>${a.error?`<details><summary>詳細</summary><pre>${escape(a.error)}</pre></details>`:''}`).join('<br>')}</td></tr>`),'</table></html>'].join('\n');
  fs.writeFileSync(path.join(output,'index.html'),html);
  console.log(`${report.passed}/${report.total} 項目成功: ${output}`);
  assert.equal(report.passed,report.total,'Unverified or failed cases remain');
}
if(require.main===module)try{main();}catch(error){console.error(error);process.exitCode=1;}
module.exports={summarize};
