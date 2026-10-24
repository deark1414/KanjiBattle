const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const {chromium}=require('playwright');
const {PNG}=require(path.join(path.dirname(require.resolve('playwright-core/package.json')),'lib/utilsBundle.js'));
const {serve}=require('./serve-gallery');
const root=path.resolve(__dirname,'../..');
const entries=require('../../Assets/Resources/UI/PieceMotifs/catalog.json').entries;
async function main(){
  assert.equal(entries.length,45);assert.equal(new Set(entries.map(e=>e.id)).size,45);
  const report={sets:entries.length,pngs:0,checks:[],errors:[]};
  for(const entry of entries)for(const [key,size]of [['resource',512],['piece',256],['enemyPiece',256]]){
    const png=PNG.sync.read(fs.readFileSync(path.join(root,'Assets/Resources',entry[key]+'.png')));
    assert.equal(png.width,size);assert.equal(png.height,size);
    let solid=0,clear=0,edge=0;
    for(let y=0;y<size;y++)for(let x=0;x<size;x++){
      const alpha=png.data[(y*size+x)*4+3];if(alpha>20)solid++;if(!alpha)clear++;
      if((x<5||y<5||x>=size-5||y>=size-5)&&alpha>20)edge++;
    }
    assert(solid>size*size*.015,entry.name+' blank '+key);assert(clear>size*size*.1,entry.name+' no transparency');
    assert.equal(edge,0,entry.name+' clipped '+key);report.pngs++;
    const block=key==='resource'?8:2;
    for(let y=0;y<size;y++)for(let x=0;x<size;x++)for(let channel=0;channel<4;channel++)
      assert.equal(png.data[(y*size+x)*4+channel],png.data[((y-y%block)*size+x-x%block)*4+channel],entry.name+' inconsistent pixel grid');
  }
  report.checks.push('45 complete sets; 135 nonblank PNGs with real alpha, safe edges, fixed dimensions');
  report.checks.push('all motifs use a 64px logical grid; all pieces use a 128px logical grid');
  const server=await serve(0);const browser=await chromium.launch();
  try{
    const page=await browser.newPage({viewport:{width:1440,height:1000}});
    page.on('pageerror',error=>report.errors.push(error.message));
    await page.goto(`http://127.0.0.1:${server.address().port}/`);
    await page.waitForFunction(()=>document.querySelectorAll('article').length===45);
    await page.waitForFunction(()=>[...document.querySelectorAll('article img')].every(image=>image.complete&&image.naturalWidth>0));
    fs.mkdirSync(path.join(root,'tmp/character-art'),{recursive:true});
    await page.screenshot({path:path.join(root,'tmp/character-art/gallery-desktop.png'),fullPage:true});
    await page.getByRole('button',{name:'融合',exact:true}).click();assert.equal(await page.locator('article').count(),17);
    await page.screenshot({path:path.join(root,'tmp/character-art/fusions-desktop.png'),fullPage:true});
    await page.getByRole('button',{name:'双剣を拡大',exact:true}).click();
    assert(await page.locator('dialog').evaluate(d=>d.open));
    await page.waitForFunction(()=>[...document.querySelectorAll('dialog img')].every(i=>i.complete&&i.naturalWidth>0));
    await page.screenshot({path:path.join(root,'tmp/character-art/dual-swords-detail.png')});
    await page.keyboard.press('Escape');assert(!(await page.locator('dialog').evaluate(d=>d.open)));
    report.checks.push('all images loaded; fusion filter 17; detail opens and Escape closes');
    await page.setViewportSize({width:390,height:844});
    await page.screenshot({path:path.join(root,'tmp/character-art/fusions-mobile.png'),fullPage:true});
    assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));
    await page.getByRole('button',{name:'風鈴を拡大',exact:true}).click();
    const box=await page.locator('dialog').boundingBox();assert(box.x>=0&&box.x+box.width<=390&&box.height<=844);
    await page.screenshot({path:path.join(root,'tmp/character-art/windchime-mobile.png')});
    await page.getByRole('button',{name:'閉じる',exact:true}).click();
    await page.getByRole('button',{name:'通常',exact:true}).click();assert.equal(await page.locator('article').count(),26);
    await page.getByRole('button',{name:'その他',exact:true}).click();assert.equal(await page.locator('article').count(),2);
    report.checks.push('mobile no overflow; detail fits; normal filter 26 and other filter 2');
    assert.deepEqual(report.errors,[]);
  }finally{
    fs.writeFileSync(path.join(root,'tmp/character-art/report.json'),JSON.stringify(report,null,2)+'\n');
    await browser.close();await new Promise(resolve=>server.close(resolve));
  }
  console.log(JSON.stringify(report,null,2));
}
main().catch(error=>{console.error(error);process.exitCode=1;});
