const fs=require('node:fs');
const path=require('node:path');

async function renderClassicPieces(page,item,reviewed,root){
  if(reviewed&&reviewed.name===item.name){
    const files=[reviewed.icon,reviewed.icon.replace('icon_','icon_enemy_')];
    return files.map(file=>fs.readFileSync(path.join(root,file)).toString('base64'));
  }
  return page.evaluate(({name})=>{
    const result=[];
    for(const enemy of [false,true]){
      const c=document.createElement('canvas');c.width=c.height=512;
      const ctx=c.getContext('2d');
      const points=enemy?[[44,28],[468,28],[420,396],[256,490],[92,396]]:[[256,22],[420,116],[468,484],[44,484],[92,116]];
      const inset=enemy?[[76,50],[436,50],[396,373],[256,456],[116,373]]:[[256,56],[396,139],[436,462],[76,462],[116,139]];
      const polygon=vertices=>{ctx.beginPath();vertices.forEach(([x,y],i)=>i?ctx.lineTo(x,y):ctx.moveTo(x,y));ctx.closePath();};
      ctx.save();ctx.shadowColor='rgba(29,20,12,.29)';ctx.shadowBlur=8;ctx.shadowOffsetX=8;ctx.shadowOffsetY=10;
      polygon(points);ctx.fillStyle='#b7a780';ctx.fill();ctx.restore();
      ctx.save();polygon(points);ctx.clip();
      polygon(enemy?[[44,28],[468,28],[444,44],[256,82],[68,44]]:[[68,468],[256,430],[444,468],[468,484],[44,484]]);
      ctx.fillStyle='#af9c74';ctx.fill();
      polygon(enemy?[[256,474],[404,388],[444,44],[256,82],[68,44],[108,388]]:[[256,38],[404,124],[444,468],[256,430],[68,468],[108,124]]);
      ctx.fillStyle='rgba(255,246,205,.035)';ctx.fill();ctx.restore();
      polygon(points);ctx.strokeStyle='#8b7253';ctx.lineWidth=10;ctx.lineJoin='round';ctx.stroke();
      polygon(inset);ctx.strokeStyle='rgba(255,249,219,.66)';ctx.lineWidth=5;ctx.stroke();
      polygon(inset);ctx.strokeStyle='rgba(151,99,43,.46)';ctx.lineWidth=2;ctx.stroke();
      const chars=Array.from(name);
      ctx.save();polygon(points);ctx.clip();
      chars.forEach((ch,i)=>{
        ctx.font=(chars.length===1?278:178)+'px PieceBrush';
        const m=ctx.measureText(ch),x=256+(m.actualBoundingBoxLeft-m.actualBoundingBoxRight)/2;
        const center=chars.length===1?(name==='一'||name==='二'||name==='三'?300:282):202+i*146;
        const y=center+(m.actualBoundingBoxAscent-m.actualBoundingBoxDescent)/2;
        ctx.fillStyle='rgba(77,39,18,.41)';ctx.fillText(ch,x+5,y+8);
        ctx.fillStyle='#36190c';ctx.fillText(ch,x,y);
      });
      ctx.restore();result.push(c.toDataURL('image/png').split(',')[1]);
    }
    return result;
  },{name:item.name});
}

module.exports={renderClassicPieces};
