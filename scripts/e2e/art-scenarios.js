const assert=require('node:assert/strict');
module.exports=[
  ...[['art-standard','standard',-1001],['art-warrior','soldiers',-1003],['art-magnet','fusion-magnet',-1007],['art-dice','fusion-dice',-1011],['art-cocoon','fusion-cocoon',-1015],['art-candle','fusion-lantern',-1017],['art-rain','fusion-rain',-1012],['art-sand-mobile','fusion-sand',-1013]].map(([id,fixture,fusion])=>({
    id,name:`キャラクター原画: ${fixture} のコマ・召喚・消去`,fixture,
    viewport:id.endsWith('mobile')?{width:390,height:844}:undefined,
    async run(game){
      const initial=await game.deploy();
      assert(initial.battle.units.every(unit=>unit.icon&&unit.motif&&!unit.motifVisible));
      // Inspect the first post-click snapshot before polling: arrival is brief.
      const started=await game.named('ConfirmDeployment');
      const hasArrival=state=>state.battle.units.some(unit=>unit.id===fusion&&unit.motifAlpha>=0.9);
      const summoned=hasArrival(started)?started:await game.wait(hasArrival,'Fusion motif did not appear at arrival.',5000);
      assert(summoned.battle.units.every(unit=>unit.icon&&unit.motif));
      await game.screenshot('summon');
      const settled=await game.wait(state=>state.battle.units.every(unit=>!unit.motifVisible),'Arrival motifs did not return to pieces.',5000);
      assert(settled.battle.units.every(unit=>unit.icon&&unit.motif));
      if(fusion===-1013){
        const state=await game.wait(state=>state.battle.units.some(unit=>unit.id===-20000),'Sand copy did not spawn.');
        assert(state.battle.units.filter(unit=>unit.id===-20000).every(unit=>unit.name==='砂'&&unit.icon&&unit.motif));
      }
      await game.screenshot('pieces');
    }
  })),
  ...[['art-selection','鈴',27],['art-shadow-selection','影',29]].map(([id,characterName,characterId])=>({
    id,name:`キャラクター原画: ${characterName}の選択時だけモチーフを表示`,fixture:'support',
    async run(game){
      let state=await game.request();assert.equal(state.visibleMotifs,0);
      // Use the roster scrollbar, not wheel input outside its clipped viewport.
      for(let attempt=0;attempt<4;attempt++){
        const target=state.controls.find(control=>control.path.includes('CharacterEntry')&&control.text.startsWith(characterName));
        if(target?.visible&&target.reachable)break;
        const scrollbar=state.controls.find(control=>control.path.includes('CharacterList/Scrollbar')&&control.kind==='Scrollbar');
        assert(scrollbar?.reachable,'Roster scrollbar is not reachable.');
        await game.clickBox({...scrollbar.rect,y:scrollbar.rect.y+scrollbar.rect.h*0.95,h:scrollbar.rect.h*0.02});
        await game.page.waitForTimeout(500);
        state=await game.request();
      }
      await game.click(control=>control.path.includes('CharacterEntry')&&control.text.startsWith(characterName),characterName+'の編成項目');
      state=await game.wait(state=>state.visibleMotifs>0,'Selected roster motif did not appear.',5000);
      assert(state.visibleMotifIds.every(id=>id===characterId));await game.screenshot('selection');
      await game.wait(state=>state.visibleMotifs===0,'Roster motif did not return to a piece.',5000);
      await game.screenshot('selection-settled');
    }
  }))
];
