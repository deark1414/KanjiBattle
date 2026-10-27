const test=require('node:test');
const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const root=path.resolve(__dirname,'../..');

test('release cache version reaches every compressed Unity resource, not only the loader',()=>{
  const html=fs.readFileSync(path.join(root,'Docs/game/index.html'),'utf8');
  for(const asset of ['game.loader.js','game.data.unityweb','game.framework.js.unityweb','game.wasm.unityweb'])
    assert(html.includes(`buildUrl + "/${asset}" + buildCacheSuffix`),asset+' must include the release version');
  const builder=fs.readFileSync(path.join(root,'Assets/Scripts/Editor/WebGLBuild.cs'),'utf8');
  assert(builder.includes('new[] { "", ".unityweb" }'),'Future builds must handle compressed and uncompressed resources');
  assert(!html.includes('KANJI_E2E')&&!html.includes('window.unityInstance ='),'Production HTML must not add the test bridge');
});
