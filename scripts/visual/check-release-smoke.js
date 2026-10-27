const assert=require('node:assert/strict');
const fs=require('node:fs');
const path=require('node:path');
const http=require('node:http');
const {chromium}=require('playwright');
const {PNG}=require(path.join(path.dirname(require.resolve('playwright-core/package.json')),'lib/utilsBundle.js'));
const root=path.resolve(__dirname,'../..');
const args=process.argv.slice(2);
const option=(key,fallback)=>args.includes(key)?args[args.indexOf(key)+1]:fallback;

async function main(){
  const output=path.resolve(root,option('--output','tmp/release-smoke'));
  fs.mkdirSync(output,{recursive:true});
  let server,browser;
  const report={results:[]};
  try{
    let url=option('--url');
    if(!url){
      const directory=path.join(root,'Docs');
      server=http.createServer((req,res)=>{
        const filename=path.resolve(directory,'.'+new URL(req.url,'http://localhost').pathname);
        if(!filename.startsWith(directory+path.sep)){res.writeHead(403).end();return;}
        fs.readFile(filename,(error,bytes)=>{
          if(error){res.writeHead(404).end();return;}
          const types={'.html':'text/html','.js':'application/javascript','.css':'text/css','.png':'image/png'};
          res.writeHead(200,{'Content-Type':types[path.extname(filename)]||'application/octet-stream','Cache-Control':'no-store'});
          res.end(bytes);
        });
      });
      await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
      url=`http://127.0.0.1:${server.address().port}/game/index.html?v=20261027-release`;
    }
    browser=await chromium.launch({headless:true,args:['--mute-audio']});
    for(const viewport of [{width:1280,height:800},{width:390,height:844},{width:844,height:390}]){
      const context=await browser.newContext({viewport});
      const page=await context.newPage();
      const errors=[],assets=[];
      const result={viewport,status:'failed',errors,assets};report.results.push(result);
      page.on('pageerror',error=>errors.push(error.message));
      page.on('console',message=>{
        if(/(?:NullReference|Argument|IndexOutOfRange|MissingReference)Exception|RuntimeError|abort\(/.test(message.text()))errors.push(message.text());
      });
      page.on('response',response=>{
        if(/\/game\.(?:loader\.js|data\.unityweb|framework\.js\.unityweb|wasm\.unityweb)(?:\?|$)/.test(response.url()))
          assets.push({url:response.url(),status:response.status()});
      });
      try{
        await page.goto(url);
        await page.waitForFunction(()=>document.querySelector('#unity-loading-bar')?.style.display==='none'
          &&performance.getEntriesByType('resource').some(e=>/game\.wasm\.unityweb/.test(e.name)),null,{timeout:180000});
        await page.waitForTimeout(2500);
        const canvas=await page.locator('#unity-canvas').boundingBox();
        assert(canvas&&canvas.width>100&&canvas.height>100);
        assert(canvas.x>=-1&&canvas.y>=-1&&canvas.width<=viewport.width+1&&canvas.height<=viewport.height+1,'Canvas must fit');
        const pixels=PNG.sync.read(await page.screenshot({path:path.join(output,`${viewport.width}.png`)}));
        const colors=new Set();
        for(let i=0;i<pixels.data.length;i+=64)colors.add(pixels.data.subarray(i,i+3).toString('hex'));
        assert(colors.size>100,'The actual game must render, not only an empty canvas');
        assert.equal(await page.evaluate(()=>typeof window.unityInstance),'undefined','No production test-instance hook');
        assert.equal(assets.length,4,'Loader and all three compressed resources must finish');
        const version=new URL(url).searchParams.get('v');
        assert(assets.every(asset=>asset.status===200&&(!version||new URL(asset.url).searchParams.get('v')===version)),
          'Every production resource must use the requested release version');
        assert.deepEqual(errors,[]);
        result.status='passed';console.log(`公開版の起動・描画・更新番号・エラー確認: ${viewport.width} 成功`);
      }finally{
        fs.writeFileSync(path.join(output,'report.json'),JSON.stringify(report,null,2));
        await context.close();
      }
    }
  }finally{
    if(browser)await browser.close();
    if(server)await new Promise(resolve=>server.close(resolve));
  }
}
main().catch(error=>{console.error(error);process.exitCode=1;});
