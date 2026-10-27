const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const {chromium}=require('playwright');
const {PNG}=require(path.join(path.dirname(require.resolve('playwright-core/package.json')),'lib/utilsBundle.js'));
async function main(){
  const directory=path.resolve(__dirname,'../../tmp/character-art/normal');fs.mkdirSync(directory,{recursive:true});
  const browser=await chromium.launch();const report={checks:[],errors:[]};
  try{
    const page=await browser.newPage({viewport:{width:1280,height:800}});
    page.on('pageerror',e=>report.errors.push(e.message));
    page.on('console',m=>{if(/NullReferenceException|ArgumentException|IndexOutOfRangeException|RuntimeError/.test(m.text()))report.errors.push(m.text());});
    await page.route('**/game/index.html',async route=>{const response=await route.fetch();const body=(await response.text()).replace('.then((unityInstance) => {','.then((unityInstance) => { window.unityInstance = unityInstance;');await route.fulfill({response,body});});
    await page.goto(process.env.KANJI_DEPLOYMENT_URL||'http://127.0.0.1:8795/game/index.html');
    await page.waitForFunction(()=>!!window.unityInstance,null,{timeout:120000});await page.waitForTimeout(4500);
    await page.evaluate(()=>window.unityInstance.SendMessage('UIManager','ShowStageSelect'));await page.waitForTimeout(500);
    await page.mouse.click(640,230);await page.waitForTimeout(500);await page.mouse.click(520,230);await page.waitForTimeout(600);
    for(const [name,viewport]of [['desktop',{width:1280,height:800}],['mobile',{width:390,height:844}],['landscape',{width:844,height:390}]]){
      await page.setViewportSize(viewport);await page.waitForTimeout(700);
      const canvas=page.locator('#unity-canvas');const box=await canvas.boundingBox();assert(box.width<=viewport.width+1&&box.height<=viewport.height+1);
      const png=PNG.sync.read(await canvas.screenshot());const colors=new Set();
      for(let y=0;y<png.height;y+=7)for(let x=0;x<png.width;x+=7){const i=(y*png.width+x)*4;colors.add(png.data[i]+','+png.data[i+1]+','+png.data[i+2]);}
      assert(colors.size>200,'Blank or failed normal WebGL canvas');
      await page.screenshot({path:path.join(directory,name+'.png')});report.checks.push(name+': normal WebGL renders, nonblank canvas and viewport fit');
    }
    assert.deepEqual(report.errors,[]);
  }finally{fs.writeFileSync(path.join(directory,'report.json'),JSON.stringify(report,null,2)+'\n');await browser.close();}
  console.log(JSON.stringify(report,null,2));
}
main().catch(error=>{console.error(error);process.exitCode=1;});
