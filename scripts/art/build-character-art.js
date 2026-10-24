const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require('playwright');

const root = path.resolve(__dirname, '../..');
const manifest = require('../../Docs/Art/CharacterArtProduction.json');
const oldCatalog = require('./reviewed-motif-crops.json');
const characters = require('../../Assets/Data/characters.json');
const fusionNames = ['双剣','散弾','戦士','衛兵','鍛冶','波','磁石','山','炭','要塞','賽子','雨','砂','草','繭','風鈴','灯'];
const roster = [...characters.map(c => ({ id:c.id, name:c.characterName })),
  ...fusionNames.map((name,index) => ({ id:-1001-index, name })), {id:-20000,name:'砂偶'}];
const rawDirectory = 'Assets/Art/Generated/CharacterMotifs';
const pieceDirectory = 'Assets/Resources/UI/CharacterPieces';
const motifDirectory = 'Assets/Resources/UI/PieceMotifs/Ready';

async function main() {
  for (const dir of [rawDirectory,pieceDirectory,motifDirectory,'tmp/character-art'])
    fs.mkdirSync(path.join(root,dir),{recursive:true});
  const generated = new Map();
  for (const item of manifest.generated) {
    const local = `${rawDirectory}/${item.key}.png`;
    if (!fs.existsSync(path.join(root,local))) fs.copyFileSync(item.path,path.join(root,local));
    generated.set(item.id,local);
  }
  const browser = await chromium.launch();
  try {
    const page = await browser.newPage();
    const font = fs.readFileSync(path.join(__dirname,'fonts/YujiBoku-Regular.ttf')).toString('base64');
    await page.evaluate(async data => {
      const face = new FontFace('PieceBrush',`url(data:font/ttf;base64,${data})`);
      await face.load(); document.fonts.add(face);
    },font);
    const entries = [];
    const checks = [];
    for (const item of roster) {
      const inherited = oldCatalog.entries.find(entry => entry.id === item.id);
      const source = generated.get(item.id === -20000 ? -1013 : item.id) || inherited?.source;
      assert(source,`Missing motif for ${item.name}`);
      const png = fs.readFileSync(path.join(root,source)).toString('base64');
      const result = await page.evaluate(async ({item,png,inherited,fresh}) => {
        const image = new Image(); image.src = `data:image/png;base64,${png}`; await image.decode();
        const source = document.createElement('canvas'); source.width=image.width;source.height=image.height;
        const sc = source.getContext('2d',{willReadFrequently:true});sc.drawImage(image,0,0);
        let x0=0,y0=0,x1=image.width,y1=image.height;
        if (!fresh && inherited) {
          x0=Math.floor(inherited.u*image.width); x1=Math.ceil((inherited.u+inherited.width)*image.width);
          y0=Math.floor((1-inherited.v-inherited.height)*image.height); y1=Math.ceil((1-inherited.v)*image.height);
        }
        const pixels=sc.getImageData(0,0,image.width,image.height).data;
        let left=x1,top=y1,right=x0,bottom=y0,opaque=0,clear=0;
        for(let y=y0;y<y1;y++)for(let x=x0;x<x1;x++){
          const a=pixels[(y*image.width+x)*4+3];
          if(a>20){left=Math.min(left,x);top=Math.min(top,y);right=Math.max(right,x);bottom=Math.max(bottom,y);opaque++;}
          if(a===0)clear++;
        }
        if(!opaque || !clear)throw Error(`Not an isolated transparent image: ${item.name}`);
        const motif=document.createElement('canvas');motif.width=motif.height=512;
        const mc=motif.getContext('2d');mc.imageSmoothingEnabled=false;
        const w=right-left+1,h=bottom-top+1,s=432/Math.max(w,h);
        mc.drawImage(image,left,top,w,h,(512-w*s)/2,(512-h*s)/2,w*s,h*s);
        const pieces=[];
        for(const enemy of [false,true]){
          const c=document.createElement('canvas');c.width=c.height=512;const ctx=c.getContext('2d');
          const polygon=(points)=>{ctx.beginPath();points.forEach(([x,y],i)=>i?ctx.lineTo(x,y):ctx.moveTo(x,y));ctx.closePath();};
          ctx.save();ctx.shadowColor='#140d0780';ctx.shadowBlur=6;ctx.shadowOffsetY=5;
          polygon([[256,20],[425,116],[473,481],[39,481],[87,116]]);ctx.fillStyle=enemy?'#5e1b14':'#684626';ctx.fill();ctx.restore();
          polygon([[256,29],[418,121],[460,470],[52,470],[94,121]]);
          const face=ctx.createLinearGradient(100,70,410,480);face.addColorStop(0,enemy?'#c94e39':'#e6cf96');
          face.addColorStop(.45,enemy?'#9b2d25':'#c9ad71');face.addColorStop(1,enemy?'#72251f':'#b69457');
          ctx.fillStyle=face;ctx.fill();
          ctx.save();ctx.clip();
          ctx.strokeStyle=enemy?'#e89e6b16':'#81562115';ctx.lineWidth=1.3;
          for(let n=0;n<25;n++){const x=60+n*17;ctx.beginPath();ctx.moveTo(x,40);ctx.bezierCurveTo(x+18,180,x-14,320,x+5,480);ctx.stroke();}
          ctx.restore();
          polygon([[256,47],[402,132],[441,451],[71,451],[110,132]]);ctx.strokeStyle=enemy?'#f2c590':'#fff0bb';ctx.lineWidth=2;ctx.stroke();
          if(item.id<0 && item.id!==-20000){polygon([[256,57],[394,138],[431,442],[81,442],[118,138]]);ctx.strokeStyle=enemy?'#ffe0ad':'#7c5b29';ctx.lineWidth=2;ctx.stroke();}
          const chars=Array.from(item.name);
          const areas=chars.length===1?[{x:100,y:145,w:312,h:278}]:chars.map((_,i)=>({x:150,y:143+i*147,w:212,h:142}));
          chars.forEach((ch,i)=>{
            const area=areas[i];ctx.font='300px PieceBrush';const m=ctx.measureText(ch);
            const width=m.actualBoundingBoxLeft+m.actualBoundingBoxRight,height=m.actualBoundingBoxAscent+m.actualBoundingBoxDescent;
            const scale=Math.min(area.w/width,area.h/height);
            ctx.save();ctx.translate(area.x+area.w/2,area.y+area.h/2);ctx.scale(scale,scale);
            ctx.fillStyle=enemy?'#fff2d7':'#301c11';
            ctx.fillText(ch,(m.actualBoundingBoxLeft-m.actualBoundingBoxRight)/2,(m.actualBoundingBoxAscent-m.actualBoundingBoxDescent)/2);ctx.restore();
          });
          const small=document.createElement('canvas');small.width=small.height=256;small.getContext('2d').drawImage(c,0,0,256,256);
          pieces.push(small.toDataURL('image/png').split(',')[1]);
        }
        return {motif:motif.toDataURL('image/png').split(',')[1],pieces,opaque,clear,bounds:{left,top,right,bottom}};
      },{item,png,inherited,fresh:generated.has(item.id === -20000 ? -1013 : item.id)});
      const token=String(item.id).replace('-','F');
      const motifPath=`${motifDirectory}/Motif_${token}.png`;
      const allyPath=`${pieceDirectory}/Piece_${token}.png`;
      const enemyPath=`${pieceDirectory}/Piece_${token}_Enemy.png`;
      fs.writeFileSync(path.join(root,motifPath),Buffer.from(result.motif,'base64'));
      fs.writeFileSync(path.join(root,allyPath),Buffer.from(result.pieces[0],'base64'));
      fs.writeFileSync(path.join(root,enemyPath),Buffer.from(result.pieces[1],'base64'));
      entries.push({...item,source,resource:motifPath.replace('Assets/Resources/','').replace('.png',''),
        piece:allyPath.replace('Assets/Resources/','').replace('.png',''),
        enemyPiece:enemyPath.replace('Assets/Resources/','').replace('.png',''),
        u:0,v:0,width:1,height:1,aspect:1,scale:1.18,rotation:!generated.has(item.id)&&inherited?inherited.rotation:0});
      checks.push({id:item.id,name:item.name,source,bounds:result.bounds,transparentPixels:result.clear});
    }
    fs.writeFileSync(path.join(root,'Assets/Resources/UI/PieceMotifs/catalog.json'),JSON.stringify({entries},null,2)+'\n');
    fs.writeFileSync(path.join(root,'tmp/character-art/production-checks.json'),JSON.stringify(checks,null,2)+'\n');
    console.log(`Produced ${entries.length} motifs and ${entries.length*2} kanji pieces.`);
  } finally {await browser.close();}
}
main().catch(error=>{console.error(error);process.exitCode=1;});
