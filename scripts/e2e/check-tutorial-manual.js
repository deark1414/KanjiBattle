const assert=require('node:assert/strict');
const {chromium}=require('playwright');
async function main(){
  const browser=await chromium.launch();
  try{
    const page=await browser.newPage({viewport:{width:390,height:844}});
    const errors=[];page.on('pageerror',error=>errors.push(error.message));
    const base=process.env.KANJI_MANUAL_URL||'http://127.0.0.1:8796/';
    async function state(){return page.evaluate(()=>command('snapshot'));}
    async function guide(id){
      for(let attempt=0;attempt<80;attempt++){
        const current=await state();
        if(current.guideId===id){assert(!current.guideOverflow);return current;}
        await page.waitForTimeout(150);
      }
      assert.fail('Guide did not open: '+id);
    }
    async function click(name){
      const control=(await state()).controls.find(c=>c.path.endsWith('/'+name));
      assert(control?.enabled&&control.visible&&control.reachable,name+' is not reachable');
      const canvas=await page.frameLocator('#game').locator('#unity-canvas').boundingBox();
      await page.mouse.click(canvas.x+(control.rect.x+control.rect.w/2)*canvas.width,canvas.y+(control.rect.y+control.rect.h/2)*canvas.height);
      await page.waitForTimeout(300);
    }
    await page.goto(base+'?preset=tutorial');
    await page.waitForFunction(()=>document.querySelector('#status').textContent.startsWith('準備完了'),null,{timeout:120000});
    assert.deepEqual((await guide('opening')).formation.filter(Boolean),[1,10,20]);
    await click('GuideSkip');await guide('formation');await click('DeployButton');
    await guide('deployment');await click('GuideSkip');
    assert((await state()).battle.deploying);assert.deepEqual(errors,[]);
    await page.screenshot({path:'tmp/e2e/manual-tutorial-mobile.png'});
    console.log('Passed: manual tutorial URL, opening, initial party, formation, real deployment, portrait.');
  }finally{await browser.close();}
}
main().catch(error=>{console.error(error);process.exitCode=1;});
