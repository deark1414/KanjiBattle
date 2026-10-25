const fs=require('node:fs');
const path=require('node:path');
const assert=require('node:assert/strict');
const {chromium}=require('playwright');
const {PNG}=require(path.join(path.dirname(require.resolve('playwright-core/package.json')),'lib/utilsBundle.js'));
const root=path.resolve(__dirname,'../..');
const directory=path.join(root,'tmp/skill-vfx');
const manifest=require('../../Docs/Art/SupportSkillVfx.json');
const layout=require('../../Assets/Art/Drafts/VFX/SupportSkills/layout.json');

async function main(){
  fs.mkdirSync(directory,{recursive:true});
  const report={status:'draft-only',assets:[],checks:[],errors:[]};
  assert.equal(manifest.generated.length,5);
  for(const entry of layout.entries){
    const png=PNG.sync.read(fs.readFileSync(path.join(root,entry.sprite)));
    assert.equal(png.width,entry.width);assert.equal(png.height,entry.height);
    let clear=0,solid=0;
    for(let y=0;y<png.height;y++)for(let x=0;x<png.width;x++){
      const i=(y*png.width+x)*4,a=png.data[i+3];
      if(!a)clear++;if(a>20)solid++;
      if(x<20||y<20||x>=png.width-20||y>=png.height-20)assert.equal(a,0,entry.key+' clipped');
      for(let c=0;c<4;c++)assert.equal(png.data[i+c],png.data[((y-y%4)*png.width+x-x%4)*4+c],entry.key+' grid');
    }
    assert(clear>png.width*png.height*.25&&solid>100);
    const effect=manifest.generated.find(e=>e.key===entry.key);
    if(effect.axis!=='right'){
      assert.deepEqual(entry.pivot,[.5,.5]);
      assert.equal(entry.start,null);assert.equal(entry.end,null);
      if(entry.key==='BellEcho')assert.equal(entry.bounds.w,entry.bounds.h);
      if(entry.key==='BellEcho'||entry.key==='CocoonGuard')
      for(let y=230;y<282;y++)for(let x=230;x<282;x++)assert(png.data[(y*png.width+x)*4+3]<=1,entry.key+' center must stay clear (at most 1/255 alpha)');
    }else{
      assert(entry.bounds.w>entry.bounds.h*5,'Directional sprite is not horizontal');
      assert.deepEqual(entry.pivot,[.5,.5]);assert(entry.start[0]<entry.end[0]);
    }
    report.assets.push({key:entry.key,width:png.width,height:png.height,solid,clear});
  }
  const browser=await chromium.launch();
  try{
    const page=await browser.newPage({viewport:{width:1280,height:1000}});
    page.on('pageerror',e=>report.errors.push(e.message));
    page.on('response',r=>{if(r.status()>=400)report.errors.push(r.status()+' '+r.url());});
    await page.goto(process.env.KANJI_SUPPORT_VFX_URL||'http://127.0.0.1:8799/Docs/Art/skill-vfx-preview.html');
    await page.waitForFunction(()=>window.supportVfxPreview?.ready);
    await page.waitForFunction(()=>[...document.querySelectorAll('.still img')].every(i=>i.complete&&i.naturalWidth>0));
    assert.equal(await page.locator('.scene').count(),4);
    assert.equal(await page.locator('.still img').count(),5);
    for(const [name,time]of [['wrap',.8],['clear',1.24],['guard',1.85],['held',3]]){
      const state=await page.evaluate(t=>{supportVfxPreview.renderAt(t);return supportVfxPreview.cocoonState(t);},time);
      assert(name==='wrap'?state.wrap>0&&state.guard===0:name==='clear'?state.wrap===0&&state.guard===0:state.wrap===0&&state.guard===1,'Wrong cocoon phase: '+name);
      await page.locator('article[data-key=CocoonGuard]').screenshot({path:path.join(directory,'cocoon-'+name+'.png')});
    }
    const phases=await page.evaluate(()=>Array.from({length:420},(_,i)=>supportVfxPreview.cocoonState(i/100)));
    assert(phases.every(p=>p.wrap===0||p.guard===0),'Wrap and protection must not overlap');
    await page.evaluate(()=>supportVfxPreview.renderAt(1.85));
    const protectedImage=await page.locator('article[data-key=CocoonGuard] canvas').evaluate(c=>c.toDataURL());
    await page.evaluate(()=>supportVfxPreview.renderAt(3));
    assert.equal(await page.locator('article[data-key=CocoonGuard] canvas').evaluate(c=>c.toDataURL()),protectedImage,'Protection state should remain after wrap disappears');
    for(const angle of [0,90,180,270]){
      await page.evaluate(a=>supportVfxPreview.renderAt(.44,a),angle);
      const anchors=await page.evaluate(()=>[point(100,140),point(290,140),point(420,140),point(540,140),point(140,140),point(500,140)]);
      assert(anchors.every(p=>p.x>=27&&p.x<=613&&p.y>=32&&p.y<=248),'Piece clipped after rotation: '+angle);
      const before=await page.locator('.scene').evaluateAll(cs=>cs.map(c=>c.toDataURL()));
      await page.evaluate(a=>supportVfxPreview.renderAt(1.18,a),angle);
      const after=await page.locator('.scene').evaluateAll(cs=>cs.map(c=>c.toDataURL()));
      assert(before.every((value,i)=>value!==after[i]),'A single-image effect did not move: '+angle);
      await page.screenshot({path:path.join(directory,'direction-'+angle+'.png'),fullPage:true});
    }
    for(const [name,size]of [['desktop',{width:1280,height:1000}],['mobile',{width:390,height:844}]]){
      await page.setViewportSize(size);await page.evaluate(()=>supportVfxPreview.renderAt(.8));
      assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth),'Page overflow: '+name);
      for(const canvas of await page.locator('.scene').all()){
        const box=await canvas.boundingBox();
        assert(Math.abs(box.width/box.height-1280/560)<.02,'Canvas stretched: '+name);
        assert(box.x>=0&&box.x+box.width<=size.width,'Canvas clipped: '+name);
        const png=PNG.sync.read(await canvas.screenshot());const colors=new Set();
        for(let i=0;i<png.data.length;i+=16)colors.add(png.data[i]+','+png.data[i+1]+','+png.data[i+2]);
        assert(colors.size>30,'Blank preview canvas');
      }
      await page.screenshot({path:path.join(directory,name+'.png'),fullPage:true});
    }
    await page.getByRole('button',{name:'再生',exact:true}).click();
    assert(await page.getByRole('button',{name:'一時停止',exact:true}).isVisible());
    await page.getByRole('button',{name:'一時停止',exact:true}).click();
    await page.getByRole('button',{name:'最初から再生',exact:true}).click();
    assert(await page.getByRole('button',{name:'一時停止',exact:true}).isVisible());
    await page.getByRole('button',{name:'270°',exact:true}).click();
    assert.equal(await page.getByRole('button',{name:'270°',exact:true}).getAttribute('aria-pressed'),'true');
    await page.setViewportSize({width:1100,height:380});
    await page.evaluate(()=>{supportVfxPreview.renderAt(.8);document.querySelector('.controls').remove();document.querySelector('main').style.cssText='display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:18px';for(const a of document.querySelectorAll('article')){a.querySelector('canvas').style.display='none';a.querySelector('.row').style.gridTemplateColumns='1fr';}});
    await page.screenshot({path:path.join(directory,'three-sprites.png'),fullPage:true});
    assert.deepEqual(report.errors,[]);
    report.checks.push('5 RGBA sprites; fixed sizes, transparent margin, 128px grid and clear bell/cocoon centers');
    report.checks.push('4 skill examples change at 0/90/180/270 degrees using single-image states');
    report.checks.push('shared thread wrap is followed by a clear gap, then persistent cocoon protection; no overlap across 420 samples');
    report.checks.push('desktop/mobile nonblank, no overflow, no stretched circles, controls work');
  }finally{
    await browser.close();fs.writeFileSync(path.join(directory,'report.json'),JSON.stringify(report,null,2)+'\n');
  }
  console.log(JSON.stringify(report,null,2));
}
main().catch(e=>{console.error(e);process.exitCode=1;});
