const assert=require('node:assert/strict');
const {chromium}=require('playwright');
async function main(){
  const browser=await chromium.launch();
  try{
    const page=await browser.newPage({viewport:{width:1280,height:800}});
    const errors=[];page.on('pageerror',error=>errors.push(error.message));
    const base=process.env.KANJI_MANUAL_URL||'http://127.0.0.1:8796/';
    async function ready(){
      await page.waitForFunction(()=>document.querySelector('#status').textContent.startsWith('準備完了'),null,{timeout:120000});
    }
    async function state(){return page.evaluate(()=>command('snapshot'));}
    async function click(name){
      const control=(await state()).controls.find(c=>c.path.endsWith('/'+name));
      assert(control?.enabled&&control.visible&&control.reachable,name+' is not reachable');
      const canvas=await page.frameLocator('#game').locator('#unity-canvas').boundingBox();
      await page.mouse.click(canvas.x+(control.rect.x+control.rect.w/2)*canvas.width,canvas.y+(control.rect.y+control.rect.h/2)*canvas.height);
      await page.waitForTimeout(250);
    }
    await page.goto(base+'?preset=tree');await ready();await page.waitForTimeout(600);
    let current=await state();assert.deepEqual(current.owned,[1,10,20]);assert.equal(current.treeSlots,3);assert.equal(current.stagePoints,30);
    await click('TreeNode_c11');await click('TreePurchase');assert((await state()).owned.includes(11));
    await page.locator('#formation').click();await ready();assert.equal((await state()).formation.filter(Boolean).length,3);
    await click('DeployButton');assert.equal((await state()).battle.units.filter(u=>!u.ally).length,3);
    await click('CancelDeployment');await page.goto(base);await ready();assert((await state()).owned.includes(11));
    await page.setViewportSize({width:390,height:844});await page.waitForTimeout(1000);
    current=await state();assert(!current.treeOverflow);assert.deepEqual(errors,[]);
    await page.screenshot({path:'tmp/e2e/manual-tree-mobile.png'});
    console.log('Passed: manual tree preset, purchase, real campaign deployment, reload, portrait.');
  }finally{await browser.close();}
}
main().catch(error=>{console.error(error);process.exitCode=1;});
