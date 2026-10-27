const assert=require('node:assert/strict');
const has=(state,key)=>(state.battle.supportTextures||[]).includes(key);
module.exports=[
  ...['dice','coal','cocoon','shell','forge','magnet','chain'].map((mode,index)=>({
    id:'vfx-'+mode,name:'スキル画像の実描画: '+mode,fixture:'standard',
    viewport:index%3===0?{width:390,height:844}:undefined,
    async run(game){
      await game.deploy();await game.request('supportVfxProbe',mode);
      const key={dice:'DiceFace6',coal:'CoalCold',cocoon:'CocoonGuard',shell:'GuardShell',forge:'ForgeHammer',magnet:'MagnetBody',chain:'ChainLightning'}[mode];
      await game.wait(s=>has(s,key),'Runtime texture did not appear: '+key,5000);
      await game.screenshot('visible-'+key);
      if(mode==='coal'){
        await game.request('supportVfxProbe','ignite');
        await game.wait(s=>has(s,'CoalHot')&&!has(s,'CoalCold'),'Coal state did not switch.');
        await game.screenshot('coal-hot');
      }
      if(mode==='cocoon'){
        await game.request('supportVfxProbe','consume');
        await game.wait(s=>!has(s,'CocoonGuard'),'Consumed cocoon remained visible.');
      }
      await game.request('supportVfxProbe','clear');
      const state=await game.wait(s=>s.battle.supportVfxCount===0,'VFX cleanup left roots behind.');
      assert.equal(state.battle.supportVfxCount,0);
    }
  }))
];
