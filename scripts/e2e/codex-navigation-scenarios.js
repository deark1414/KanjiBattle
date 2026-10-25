const assert=require('node:assert/strict');
module.exports=[1280,390,844].map(width=>({
  id:`codex-navigation-${width}`,name:`独立図鑑と下部4タブの往復 ${width}`,fixture:'animals',
  viewport:{width,height:width===390?844:width===844?390:800},
  async run(game){
    let state=await game.request();
    assert(!state.controls.some(c=>c.path.endsWith('/OpenCodex')),'roster has no codex launcher');
    const tabs=state.controls.filter(c=>/\/TabButtons\//.test(c.path)&&['軍勢','行軍','城下','図鑑'].includes(c.text)).sort((a,b)=>a.rect.x-b.rect.x);
    assert.deepEqual(tabs.map(c=>c.text),['軍勢','行軍','城下','図鑑']);
    assert(tabs.every(c=>c.visible&&c.reachable&&Math.abs(c.rect.w-.25)<.015));
    for(const label of ['城下','行軍','軍勢']){
      await game.click(c=>c.text===label,label);
      await game.named('CodexTabButton');
      state=await game.request();
      assert(state.controls.some(c=>c.path.includes('/CharacterCodexScreen/NormalTab')&&c.reachable));
      assert(!state.controls.some(c=>c.path.includes('/CharacterEntry')),'roster is not underneath the codex');
      assert(!state.controls.some(c=>c.path.endsWith('/CloseCodex')),'codex is a screen, not a dialog');
      assert(!state.codexOverflow,state.codexOverflowNames);
    }
    await game.screenshot('normal-codex');
    await game.named('FusionTab'); await game.screenshot('fusion-codex');
    await game.named('NormalTab'); await game.named('EnemyTab'); await game.named('NormalTab');
    await game.click(c=>c.text==='軍勢','軍勢');
    assert(!(await game.request()).controls.some(c=>c.path.includes('/CharacterCodexScreen/')));
    await game.request('resume');
    await game.deploy(); assert(!game.state.controls.some(c=>c.path.endsWith('/CodexTabButton')),'navigation hides in battle');
    await game.named('CancelDeployment'); await game.named('CodexTabButton');
  }
}));
