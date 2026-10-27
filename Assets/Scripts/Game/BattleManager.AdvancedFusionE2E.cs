#if KANJI_BATTLE_E2E
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    internal void ProbeAdvancedFusion()
    {
        if (IsDeploying) throw new System.InvalidOperationException("Fusion must be executed first.");
        StopAllCoroutines();
        foreach (var copy in gridMap.Values.Where(unit => unit.IsGenerated).ToArray()) HandleDeath(copy);
        var root = allies.First(unit => unit.data.characterId <= -1006 && unit.data.characterId >= -1017);
        var friends = allies.Where(unit => unit != root).ToArray();
        void Check(bool value, string label)
        { if (!value) throw new System.InvalidOperationException(root.data.characterName + " probe: " + label); }
        void Place(BattleCharacter unit, Vector2Int cell)
        {
            if (gridMap.TryGetValue(unit.gridPos, out var old) && old == unit) gridMap.Remove(unit.gridPos);
            unit.gridPos = cell; gridMap[cell] = unit; unit.transform.position = gridCells[cell.x,cell.y].position;
        }
        gridMap.Clear(); rainZones.Clear(); grassSplits.Clear();
        magnetCollisionTurns.Clear();rainPulses.Clear();
        Place(root,new(2,4));
        var friendCells = new[] { new Vector2Int(0,6),new(1,6),new(4,6),new(5,6) };
        for (int i=0;i<friends.Length;i++) Place(friends[i],friendCells[i]);
        var enemyCells = new[] { new Vector2Int(0,0),new(1,0),new(4,0),new(5,0) };
        for(int i=0;i<enemies.Count;i++) Place(enemies[i],enemyCells[i]);
        foreach (var unit in gridMap.Values)
        {
            unit.currentHP=unit.maxHP; unit.defense=0; unit.CocoonShield=false;
            for(int i=0;i<3;i++) unit.TickAttackBuffs(); unit.DiceDefensePercent=0;
        }
        bool force = SkillExecutor.ForceSkillActivationForVfxReview;
        var attacker = enemies[0]; var target = friends[0];
        CharacterData oldAttacker = attacker.data, oldFriend = target.data;
        CharacterData oldSecondEnemy = enemies[1].data;
        try
        {
            SkillExecutor.ForceSkillActivationForVfxReview = true;
            switch(root.data.skillType)
            {
                case SkillType.Wave:
                    root.attack=10;
                    for(int i=0;i<3;i++) Place(enemies[i],new(i+1,3));
                    var health=enemies.Take(3).Select(unit=>unit.currentHP).ToArray();
                    Check(SkillExecutor.TryExecute(root,enemies[1],this),"wave dispatch");
                    for(int i=0;i<3;i++) Check(enemies[i].gridPos.y==2 && enemies[i].currentHP<health[i],"three-cell push");
                    for(int i=0;i<3;i++) Place(enemies[i],new(i+1,3));
                    var blocked=new Vector2Int(2,2); impassableCells.Add(blocked);
                    Check(SkillExecutor.TryExecute(root,enemies[1],this),"blocked wave still damages");
                    Check(enemies[1].gridPos==new Vector2Int(2,3) && !enemies[1].IsStunned(),"water blocks only displacement");
                    impassableCells.Remove(blocked); break;
                case SkillType.Magnet:
                    attacker.data=Resources.Load<CharacterDatabase>("CharacterDatabase").GetById(10);
                    enemies[1].data=Resources.Load<CharacterDatabase>("CharacterDatabase").GetById(17);
                    root.attack=20;
                    for(int i=2;i<enemies.Count;i++) Place(enemies[i],new(0,i-2));
                    foreach(bool side in new[]{true,false})
                    {
                        fusionTurn++;
                        root.isAlly=side;attacker.isAlly=enemies[1].isAlly=!side;
                        Place(attacker,new(2,2));Place(enemies[1],new(4,2));
                        attacker.currentHP=enemies[1].currentHP=1000;
                        var pair=new System.Collections.Generic.List<BattleCharacter>{attacker,enemies[1]};
                        ResolveMagnet(root,pair);
                        Check(attacker.gridPos==new Vector2Int(2,3)&&enemies[1].gridPos==new Vector2Int(5,1),
                            "melee pull and backline push on both sides");
                        Check(attacker.currentHP==1000&&enemies[1].currentHP==1000,"successful movement deals no collision damage");
                        Place(attacker,new(2,2));Place(enemies[1],new(4,2));
                        var blocks=new[]{new Vector2Int(2,3),new Vector2Int(5,1)};
                        foreach(var cell in blocks) impassableCells.Add(cell);
                        ResolveMagnet(root,pair);
                        Check(attacker.currentHP==980&&enemies[1].currentHP==980
                            &&attacker.gridPos==new Vector2Int(2,2)&&enemies[1].gridPos==new Vector2Int(4,2),
                            "blocked terrain causes one hundred percent collision damage");
                        target.data=root.data;target.attack=20;target.isAlly=side;Place(target,new(2,5));
                        ResolveMagnet(target,pair);
                        Check(attacker.currentHP==980&&enemies[1].currentHP==980,
                            "a second magnet cannot deal another collision to the same enemy in one turn");
                        foreach(var cell in blocks) impassableCells.Remove(cell);
                        fusionTurn++;
                        Place(attacker,new(2,3));Place(enemies[1],new(5,0));
                        ResolveMagnet(root,pair);
                        Check(attacker.currentHP==960&&enemies[1].currentHP==960,
                            "occupied caster cell and board edge collide again on the next turn");
                    }
                    root.isAlly=target.isAlly=true;target.data=oldFriend;attacker.isAlly=enemies[1].isAlly=false;
                    foreach(var enemy in enemies) enemy.currentHP=1000;
                    var positions=enemies.Select(unit=>unit.gridPos).ToArray();
                    var priorHP=enemies.Select(unit=>unit.currentHP).ToArray();
                    BeginAdvancedFusionTurn(true,fusionTurn+1);
                    Check(enemies.Where((unit,i)=>unit.gridPos!=positions[i]||unit.currentHP!=priorHP[i]).Count()==2,
                        "turn-start dispatch affects at most two distinct enemies");
                    break;
                case SkillType.Mountain:
                    attacker.data=Resources.Load<CharacterDatabase>("CharacterDatabase").GetById(13);
                    root.currentHP=20; int before=attacker.currentHP;
                    root.TakeDamage(100,this,attacker,ignoreDefense:true);
                    Check(root.isDead && attacker.currentHP==before-20,"fatal actual loss returned without shield reduction"); break;
                case SkillType.Coal:
                    root.CoalAwake=false; int maximum=root.maxHP; root.attack=100;
                    root.TakeDamage(40,this,attacker,origin:BattleDamageOrigin.Transferred);
                    Check(!root.CoalAwake && root.currentHP==maximum-40,"transferred damage neither mitigates nor ignites");
                    root.currentHP=maximum; root.TakeDamage(40,this,attacker,ignoreDefense:true);
                    Check(root.CoalAwake && root.currentHP==maximum-20 && root.GetEffectiveAttack(this)==140,"first direct hit halves then ignites");
                    root.TakeDamage(40,this,attacker,ignoreDefense:true);
                    Check(root.currentHP==maximum-60,"burning form no longer halves"); break;
                case SkillType.Fortress:
                    root.attack=40; var hp=enemies.Select(unit=>unit.currentHP).ToArray();
                    BeginAdvancedFusionTurn(true,1);
                    Check(enemies.Where((unit,i)=>unit.currentHP<hp[i]).Count()==2,"two distinct mortar targets");
                    for(int i=0;i<3;i++) Place(enemies[i],new(i+1,3));
                    hp=enemies.Select(unit=>unit.currentHP).ToArray(); int rootHP=root.currentHP;
                    root.TakeDamage(100,this,attacker,ignoreDefense:true);
                    Check(root.currentHP==rootHP-80,"fortress shield");
                    for(int i=0;i<3;i++) Check(hp[i]-enemies[i].currentHP==20,"area return"); break;
                case SkillType.Dice:
                    foreach(var unit in allies) unit.currentHP=1;
                    fusionTurn=1; ResolveDice(root,1); ResolveDice(root,2);
                    Check(allies.All(unit=>unit.currentHP==unit.maxHP),"both healing faces fully heal all living allies");
                    ResolveDice(root,3); ResolveDice(root,4);
                    Check(allies.All(unit=>unit.DiceDefensePercent==100),"both defense faces double defense without stacking");
                    ResolveDice(root,5); ResolveDice(root,6);
                    Check(allies.All(unit=>unit.AttackBuffPercent==100),"both attack faces double attack without stacking");
                    FusionSupportExecutor.EndSideTurn(this,true);
                    Check(allies.All(unit=>unit.AttackBuffPercent==100 && unit.DiceDefensePercent==100),"both buffs survive enemy phase");
                    ClearDiceBonuses(true);
                    Check(allies.All(unit=>unit.DiceDefensePercent==0&&unit.AttackBuffPercent==0),"bonuses expire at next friendly phase");
                    fusionTurn=2;ResolveDice(root,5);
                    Check(target.AttackBuffPercent==125,"second turn grows to 2.25 times");
                    ClearDiceBonuses(true);fusionTurn=20;ResolveDice(root,3);ResolveDice(root,5);
                    Check(target.AttackBuffPercent==300&&target.DiceDefensePercent==300,"late bonuses cap at four times");
                    root.isAlly=false;target.currentHP=1;
                    foreach(var enemy in enemies) enemy.currentHP=1;
                    ResolveDice(root,2);ResolveDice(root,4);ResolveDice(root,6);
                    Check(enemies.All(enemy=>enemy.currentHP==enemy.maxHP&&enemy.DiceDefensePercent==300
                        &&enemy.AttackBuffPercent==300)&&target.currentHP==1,"enemy dice affects only its living army");
                    root.isAlly=true;
                    break;
                case SkillType.Rain:
                    root.attack=20;
                    foreach(var friend in allies) friend.currentHP=friend.maxHP;
                    for(int i=0;i<enemies.Count;i++) Place(enemies[i],new(i+1,0));
                    Place(root,new(2,6));
                    Check(!TryFindRainCenter(root,out _)&&!SkillExecutor.TryExecute(root,target,this),
                        "rain does not spend an action on an empty distant area");
                    DoAction(root,"味方");
                    Check(root.gridPos.y==5&&rainZones.Count==0,"rain advances when nothing useful is within casting range");
                    Check(!CastRain(root,new(2,0))&&rainZones.Count==0,"direct rain casts cannot bypass range");
                    Place(root,new(2,3));
                    Vector2Int best=FindRainCenter(root);
                    Check(enemies.Count(enemy=>BattleRangeService.Distance(best,enemy.gridPos)<=1)>=3,
                        "rain can cover enemy cluster after moving within casting range");
                    Place(root,new(2,4));
                    Place(target,new(2,3));target.SetBattleVitals(1000,1,20,0);
                    Check(BattleRangeService.Distance(FindRainCenter(root),target.gridPos)<=1,"injured ally can outweigh enemy coverage");
                    Place(attacker,new(3,3));attacker.SetBattleVitals(1000,1000,20,0);attacker.CocoonShield=true;
                    int rate=100;
                    CastRain(root,target.gridPos); CastRain(root,target.gridPos);
                    Check(rainZones.Count==2 && target.currentHP==1+2*rate&&attacker.currentHP==980&&attacker.CocoonShield,
                        "two rain layers heal allies and damage enemies immediately without direct-hit protection");
                    var secondRain=friends[1];var secondRainData=secondRain.data;
                    secondRain.data=root.data;
                    CastRain(secondRain,target.gridPos);secondRain.data=secondRainData;
                    Check(rainZones.Count==3&&target.currentHP==201&&attacker.currentHP==980,
                        "another caster cannot add a third pulse to the same target in one turn");
                    root.attack=1000;
                    BeginAdvancedFusionTurn(true,fusionTurn+1);
                    Check(target.currentHP==1+4*rate&&attacker.currentHP==960,"both layers continue with snapshotted attack");
                    Place(target,new(5,3)); int current=target.currentHP;
                    root.isDead=true;
                    BeginAdvancedFusionTurn(true,fusionTurn+1);
                    Check(rainZones.Count==0 && target.currentHP==current&&attacker.currentHP==940,
                        "fixed cells expire after three pulses and survive caster death");
                    root.isDead=false;
                    root.isAlly=target.isAlly=false;attacker.isAlly=true;
                    root.attack=20;target.currentHP=1;attacker.currentHP=1000;
                    Place(attacker,new(4,3));CastRain(root,target.gridPos);
                    Check(target.currentHP==101&&attacker.currentHP==990,"enemy rain has its own pulse budget in the same battle turn");
                    root.isAlly=target.isAlly=true;attacker.isAlly=false;
                    break;
                case SkillType.Sand:
                    root.currentHP=1;
                    var copies=Enumerable.Range(0,3).Select(_=>SpawnSandCopy(root)).ToArray();
                    Check(copies.All(unit=>unit!=null && unit.maxHP==root.maxHP/2 && unit.currentHP==unit.maxHP
                        && unit.IsGenerated && unit.EligibleTurn>fusionTurn),"three full-health delayed copies");
                    Check(SpawnSandCopy(root)==null && SpawnSandCopy(copies[0])==null,"copy cap and no recursive summoning");
                    HandleDeath(root); Check(!gridMap.Values.Any(unit=>unit.Summoner==root),"sand copies vanish with source");
                    DoAction(copies[0],"味方");
                    Check(!gridMap.ContainsValue(copies[0]),"removed summon must not act from an old action snapshot"); break;
                case SkillType.Grass:
                    root.currentHP=Mathf.FloorToInt(root.maxHP*.75f); root.TakeDamage(1,this,attacker,ignoreDefense:true);
                    var first=allies.Single(unit=>unit.IsGenerated);
                    Check(first.currentHP==root.currentHP && first.maxHP==root.maxHP,"split preserves HP instead of sharing it");
                    SplitGrass(first); SplitGrass(root); SplitGrass(first);
                    Check(allies.Count(unit=>unit.data.skillType==SkillType.Grass)==4,"lineage cap three splits");
                    HandleDeath(root); Check(allies.Count(unit=>unit.data.skillType==SkillType.Grass)==3,"grass descendants survive parent"); break;
                case SkillType.Cocoon:
                    target.currentHP=100;
                    Check(SkillExecutor.TryExecute(root,target,this) && target.CocoonShield,"protection attached");
                    Check(!SkillExecutor.TryExecute(root,target,this),"no shield stacking");
                    target.TakeDamage(40,this,attacker,ignoreDefense:true);
                    Check(target.currentHP==100 && !target.CocoonShield,"one segment blocked");
                    target.TakeDamage(40,this,attacker,ignoreDefense:true);
                    Check(target.currentHP==60,"second segment damages"); break;
                case SkillType.WindChime:
                    target.data=Resources.Load<CharacterDatabase>("CharacterDatabase").GetById(17); target.attack=10;
                    Place(target,new(0,5)); Place(attacker,new(0,2)); Place(root,new(5,6));
                    root.isDead=true; int baseHP=attacker.currentHP;
                    Check(SkillExecutor.TryExecute(target,attacker,this),"arrow without echo"); int damage=baseHP-attacker.currentHP;
                    attacker.currentHP=baseHP; root.isDead=false;
                    Check(SkillExecutor.TryExecute(target,attacker,this) && baseHP-attacker.currentHP==damage*2,
                        "distant chime adds exactly one real repeat"); break;
                case SkillType.Lantern:
                    attacker.data=Resources.Load<CharacterDatabase>("CharacterDatabase").GetById(17); attacker.attack=10;
                    Place(root,new(2,4)); Place(target,new(2,3)); Place(attacker,new(2,0));
                    Check(TargetingService.FindArrowTarget(this,attacker)==root,"reachable lamp preferred over closer ally");
                    int protectedHP=target.currentHP; Check(SkillExecutor.TryExecute(attacker,target,this),"projectile dispatched");
                    Check(target.currentHP==protectedHP,"adjacent ally evades");
                    int lampHP=root.currentHP; Check(SkillExecutor.TryExecute(attacker,root,this),"lamp can be targeted");
                    Check(root.currentHP<lampHP,"lamp does not protect itself"); break;
                default: throw new System.InvalidOperationException("Unsupported fusion probe.");
            }
            AddLog("通常融合の能力チェック完了: "+root.data.characterName);
        }
        finally
        {
            SkillExecutor.ForceSkillActivationForVfxReview=force;
            attacker.data=oldAttacker; target.data=oldFriend;
            enemies[1].data=oldSecondEnemy;
        }
    }
}
#endif
