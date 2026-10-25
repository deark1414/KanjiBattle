const fs=require('node:fs');
const path=require('node:path');
const assert=require('node:assert/strict');
const {chromium}=require('playwright');
const root=path.resolve(__dirname,'../..');
const manifest=require('../../Docs/Art/SupportSkillVfx.json');

async function main(){
  const browser=await chromium.launch();
  const checks=[];
  try{
    const page=await browser.newPage();
    for(const entry of manifest.generated){
      const source=path.join(root,entry.source);
      fs.mkdirSync(path.dirname(source),{recursive:true});
      if(!fs.existsSync(source))fs.copyFileSync(entry.path,source);
      const result=await page.evaluate(async({png,entry})=>{
        const image=new Image();image.src='data:image/png;base64,'+png;await image.decode();
        const canvas=document.createElement('canvas');canvas.width=image.width;canvas.height=image.height;
        const ctx=canvas.getContext('2d',{willReadFrequently:true});ctx.drawImage(image,0,0);
        const rgba=ctx.getImageData(0,0,canvas.width,canvas.height).data;
        let left=canvas.width,top=canvas.height,right=-1,bottom=-1,clear=0;
        for(let y=0;y<canvas.height;y++)for(let x=0;x<canvas.width;x++){
          const alpha=rgba[(y*canvas.width+x)*4+3];
          if(alpha>20){left=Math.min(left,x);right=Math.max(right,x);top=Math.min(top,y);bottom=Math.max(bottom,y);}
          if(!alpha)clear++;
        }
        if(right<left||!clear)throw Error('Missing image or alpha: '+entry.key);
        const logical=document.createElement('canvas');logical.width=entry.logicalWidth;logical.height=entry.logicalHeight;
        const lc=logical.getContext('2d');lc.imageSmoothingEnabled=false;
        const w=right-left+1,h=bottom-top+1;
        const scale=Math.min((logical.width-24)/w,(logical.height-12)/h);
        const dw=Math.max(1,Math.round(w*scale)),dh=Math.max(1,Math.round(h*scale));
        const x=Math.floor((logical.width-dw)/2),y=Math.floor((logical.height-dh)/2);
        lc.drawImage(image,left,top,w,h,x,y,dw,dh);
        const output=document.createElement('canvas');output.width=logical.width*4;output.height=logical.height*4;
        const oc=output.getContext('2d');oc.imageSmoothingEnabled=false;oc.drawImage(logical,0,0,output.width,output.height);
        return {png:output.toDataURL('image/png').split(',')[1],bounds:{x:x*4,y:y*4,w:dw*4,h:dh*4},width:output.width,height:output.height};
      },{png:fs.readFileSync(source).toString('base64'),entry});
      assert(result.bounds.w>0&&result.bounds.h>0);
      fs.writeFileSync(path.join(root,entry.sprite),Buffer.from(result.png,'base64'));
      checks.push({key:entry.key,sprite:entry.sprite,width:result.width,height:result.height,bounds:result.bounds,
        pivot:[0.5,0.5],start:entry.axis==='right'?[(result.bounds.x+2)/result.width,0.5]:null,
        end:entry.axis==='right'?[(result.bounds.x+result.bounds.w-2)/result.width,0.5]:null});
    }
    fs.writeFileSync(path.join(root,'Assets/Art/Drafts/VFX/SupportSkills/layout.json'),JSON.stringify({entries:checks},null,2)+'\n');
    console.log('Prepared '+checks.length+' single-image draft effects. Production VFX unchanged.');
  }finally{await browser.close();}
}
main().catch(error=>{console.error(error);process.exitCode=1;});
