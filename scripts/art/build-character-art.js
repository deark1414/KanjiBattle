const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const { chromium } = require('playwright');
const { renderClassicPieces } = require('./render-classic-pieces');

const root = path.resolve(__dirname, '../..');
const manifest = require('../../Docs/Art/CharacterArtPixelProduction.json');
const refinements = require('../../Docs/Art/CharacterArtRefinements.json');
const oldCatalog = require('./reviewed-motif-crops.json');
const characters = require('../../Assets/Data/characters.json');
const fusionNames = ['双剣','散弾','戦士','衛兵','鍛冶','波','磁石','山','炭','要塞','賽子','雨','砂','草','繭','風鈴','灯'];
const roster = [...characters.map(c => ({ id:c.id, name:c.characterName })),
  ...fusionNames.map((name,index) => ({ id:-1001-index, name })), {id:-20000,name:'砂'}];
const rawDirectory = 'Assets/Art/Generated/CharacterMotifsPixel';
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
  for(const item of refinements.generated){
    const local=`Assets/Art/Generated/CharacterMotifsReviewed/${item.key}.png`;
    fs.mkdirSync(path.dirname(path.join(root,local)),{recursive:true});
    if(!fs.existsSync(path.join(root,local)))fs.copyFileSync(item.path,path.join(root,local));
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
      const token=String(item.id).replace('-','F');
      const allyPath=`${pieceDirectory}/Piece_${token}.png`;
      const enemyPath=`${pieceDirectory}/Piece_${token}_Enemy.png`;
      const pieces=await renderClassicPieces(page,item,inherited,root);
      fs.writeFileSync(path.join(root,allyPath),Buffer.from(pieces[0],'base64'));
      fs.writeFileSync(path.join(root,enemyPath),Buffer.from(pieces[1],'base64'));
      if (inherited) {
        const piece=`${pieceDirectory}/Piece_${token}`.replace('Assets/Resources/','');
        const enemyPiece=piece+'_Enemy';
        const source='Assets/Resources/'+inherited.resource+'.png';
        for(const file of [source,'Assets/Resources/'+piece+'.png','Assets/Resources/'+enemyPiece+'.png'])
          assert(fs.existsSync(path.join(root,file)),`Missing preserved art: ${file}`);
        const {resource,u,v,width,height,aspect,rotation}=inherited;
        entries.push({...item,source,resource,piece,enemyPiece,u,v,width,height,aspect,rotation,scale:1.18});
        checks.push({id:item.id,name:item.name,source,reused:true});
        continue;
      }
      const source = generated.get(item.id === -20000 ? -1013 : item.id);
      assert(source,`Missing motif for ${item.name}`);
      const png = fs.readFileSync(path.join(root,source)).toString('base64');
      const logicalSize = refinements.generated.find(entry=>entry.id===item.id)?.logicalSize ?? 128;
      assert([64,128].includes(logicalSize),'Unsupported motif logical size');
      const result = await page.evaluate(async ({item,png,logicalSize}) => {
        const image = new Image(); image.src = `data:image/png;base64,${png}`; await image.decode();
        const source = document.createElement('canvas'); source.width=image.width;source.height=image.height;
        const sc = source.getContext('2d',{willReadFrequently:true});sc.drawImage(image,0,0);
        let x0=0,y0=0,x1=image.width,y1=image.height;
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
        const w=right-left+1,h=bottom-top+1,s=Math.round(logicalSize*108/128)/Math.max(w,h);
        const logical=document.createElement('canvas');logical.width=logical.height=logicalSize;
        const lc=logical.getContext('2d');lc.imageSmoothingEnabled=false;
        const dw=Math.max(1,Math.round(w*s)),dh=Math.max(1,Math.round(h*s));
        lc.drawImage(image,left,top,w,h,Math.floor((logicalSize-dw)/2),Math.floor((logicalSize-dh)/2),dw,dh);
        mc.drawImage(logical,0,0,512,512);
        return {motif:motif.toDataURL('image/png').split(',')[1],opaque,clear,bounds:{left,top,right,bottom}};
      },{item,png,logicalSize});
      const motifPath=`${motifDirectory}/Motif_${token}.png`;
      fs.writeFileSync(path.join(root,motifPath),Buffer.from(result.motif,'base64'));
      entries.push({...item,source,resource:motifPath.replace('Assets/Resources/','').replace('.png',''),
        piece:allyPath.replace('Assets/Resources/','').replace('.png',''),
        enemyPiece:enemyPath.replace('Assets/Resources/','').replace('.png',''),
        u:0,v:0,width:1,height:1,aspect:1,scale:1.18,rotation:0,...(logicalSize===64?{logicalSize}: {})});
      checks.push({id:item.id,name:item.name,source,bounds:result.bounds,transparentPixels:result.clear});
    }
    fs.writeFileSync(path.join(root,'Assets/Resources/UI/PieceMotifs/catalog.json'),JSON.stringify({entries},null,2)+'\n');
    fs.writeFileSync(path.join(root,'tmp/character-art/production-checks.json'),JSON.stringify(checks,null,2)+'\n');
    console.log(`Prepared ${entries.length} art sets; preserved ${checks.filter(c=>c.reused).length} reviewed motifs; all pieces use the classic design.`);
  } finally {await browser.close();}
}
main().catch(error=>{console.error(error);process.exitCode=1;});
