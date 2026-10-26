#if KANJI_BATTLE_E2E
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    internal void ProbeScatterModesForE2E()
    {
        if (!IsDeploying || enemies.Count < 4 || allies.Count < 2)
            throw new System.InvalidOperationException("Scatter deployment fixture required.");
        var snapshot=allies.Concat(enemies).Select(unit=>(unit,unit.data,unit.gridPos,unit.isAlly,unit.isDead,
            unit.maxHP,unit.currentHP,unit.attack,unit.defense)).ToArray();
        var terrain=impassableCells.ToArray();
        var caster=allies[0];var friend=allies[1];var main=enemies[0];var left=enemies[1];var right=enemies[2];var near=enemies[3];
        var db=Resources.Load<CharacterDatabase>("CharacterDatabase");
        var definition=CharacterData.CreateBattleFusion(BattleFusionRules.GetRecipe(-1002),new[]{db.GetById(3),db.GetById(18)},20);
        void Check(bool valid,string label)
        { if(!valid)throw new System.InvalidOperationException("Scatter mode: "+label); }
        void Place(BattleCharacter unit,Vector2Int cell)
        { unit.gridPos=cell;gridMap[cell]=unit;unit.transform.position=gridCells[cell.x,cell.y].position; }
        void Reset(bool side)
        {
            gridMap.Clear();impassableCells.Clear();
            foreach(var unit in new[]{caster,friend,main,left,right,near})
            { unit.isDead=false;unit.SetBattleVitals(1000,1000,100,0);unit.CocoonShield=false; }
            caster.isAlly=friend.isAlly=side;main.isAlly=left.isAlly=right.isAlly=near.isAlly=!side;
            main.data=db.GetById(10);
            Place(caster,new(2,5));Place(main,new(2,2));
        }
        try
        {
            caster.data=definition;
            foreach(bool side in new[]{true,false})
            {
                Reset(side);Place(friend,new(1,2));
                Check(SkillExecutor.TryExecute(caster,main,this)&&main.currentHP==760&&friend.currentHP==1000,
                    "one enemy concentrates three pellets without friendly fire");
                Reset(side);Place(left,new(1,2));
                Check(SkillExecutor.TryExecute(caster,main,this)&&main.currentHP==760&&left.currentHP==1000,
                    "two enemies still concentrate on one target");
                Reset(side);Place(left,new(1,2));Place(right,new(3,2));
                Check(SkillExecutor.TryExecute(caster,main,this)&&new[]{main,left,right}.All(unit=>unit.currentHP==920),
                    "three enemies receive eighty percent each");
                Reset(side);Place(left,new(1,2));Place(right,new(3,2));right.isDead=true;
                Check(SkillExecutor.TryExecute(caster,main,this)&&main.currentHP==760&&left.currentHP==1000,
                    "dead pieces do not trigger spread mode");
                Reset(side);Place(left,new(1,2));Place(right,new(3,2));right.currentHP=0;
                Check(SkillExecutor.TryExecute(caster,main,this)&&main.currentHP==760,
                    "zero-HP pieces do not trigger spread mode");
                Reset(side);Place(left,new(1,2));Place(right,new(3,2));Place(near,new(2,4));
                Check(TargetingService.FindEnemyTarget(this,caster,SkillType.ScatterShot)==main,
                    "three distant targets take priority over a nearer single target");
                Reset(side);main.data=db.GetById(15);
                Check(SkillExecutor.TryExecute(caster,main,this)&&main.currentHP==1000,
                    "focused shot does not bypass wall immunity");
                Reset(side);gridMap.Remove(main.gridPos);Place(main,new(2,1));
                Check(!SkillExecutor.TryExecute(caster,main,this)&&main.currentHP==1000,
                    "focused shot keeps the existing range limit");
            }
            AddLog("散弾モードチェック完了");
        }
        finally
        {
            StopAllCoroutines();gridMap.Clear();
            foreach(var row in snapshot)
            {
                row.unit.data=row.data;row.unit.isAlly=row.isAlly;row.unit.isDead=row.isDead;
                row.unit.SetBattleVitals(row.maxHP,row.currentHP,row.attack,row.defense);Place(row.unit,row.gridPos);
            }
            impassableCells.Clear();impassableCells.UnionWith(terrain);Destroy(definition);
        }
    }

    internal void ProbeShootingDeadZoneForE2E()
    {
        if (!IsDeploying || enemies.Count < 2) throw new System.InvalidOperationException("Ranged deployment fixture required.");
        var units = allies.Concat(enemies).Select(unit => (unit, unit.gridPos, unit.isAlly,
            unit.maxHP, unit.currentHP, unit.attack, unit.defense)).ToArray();
        var terrain = impassableCells.ToArray(); var traps = trapCells.ToArray(); var soil = soilTrapCells.ToArray();
        var caster = allies[0]; var near = enemies[0]; var far = enemies[1];
        var original = caster.data; var definition = Instantiate(original);
        void Check(bool valid, string label)
        { if (!valid) throw new System.InvalidOperationException("Shooting dead zone: " + label); }
        void Place(BattleCharacter unit, Vector2Int cell)
        {
            if (gridMap.TryGetValue(unit.gridPos, out var current) && current == unit) gridMap.Remove(unit.gridPos);
            unit.gridPos = cell; gridMap[cell] = unit;
            unit.transform.position = gridCells[cell.x, cell.y].position;
        }
        try
        {
            caster.data = definition; definition.category = CharacterCategory.Ranged;
            trapCells.Clear(); soilTrapCells.Clear();
            near.SetBattleVitals(10000, 10000, 1, 0); far.SetBattleVitals(10000, 10000, 1, 0);
            foreach (bool side in new[] { true, false }) foreach (SkillType skill in new[] { SkillType.Arrow, SkillType.Gun })
            {
                definition.skillType = skill; caster.isAlly = side; near.isAlly = far.isAlly = !side;
                gridMap.Clear(); impassableCells.Clear(); Place(caster, new(2, 3)); Place(near, new(2, 2));
                int hp = near.currentHP;
                DoAction(caster, "test");
                Check(near.currentHP == hp && BattleRangeService.Distance(caster.gridPos, near.gridPos) >= 2,
                    "adjacent enemy causes retreat instead of a basic attack: " + skill + "/" + side);
                var firingCell = caster.gridPos;
                DoAction(caster, "test");
                Check(near.currentHP < hp && caster.gridPos == firingCell, "shoot on the next action after retreat");

                gridMap.Clear(); Place(caster, new(2, 3)); Place(near, new(2, 2)); hp = near.currentHP;
                for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++)
                    if (x != 0 || y != 0) impassableCells.Add(caster.gridPos + new Vector2Int(x, y));
                DoAction(caster, "test");
                Check(caster.gridPos == new Vector2Int(2, 3) && near.currentHP == hp, "trapped shooter waits without attacking");

                gridMap.Clear(); impassableCells.Clear();
                Place(caster, new(2, 4)); Place(near, new(2, 3)); Place(far, new(2, 1));
                hp = near.currentHP; int farHP = far.currentHP;
                DoAction(caster, "test");
                Check(near.currentHP == hp && far.currentHP < farHP, "distant target can be shot without hitting adjacent enemy");
            }
        }
        finally
        {
            StopAllCoroutines(); caster.data = original; Destroy(definition); gridMap.Clear();
            foreach (var row in units)
            {
                row.unit.isAlly = row.isAlly; row.unit.SetBattleVitals(row.maxHP, row.currentHP, row.attack, row.defense);
                Place(row.unit, row.gridPos);
            }
            impassableCells.Clear(); impassableCells.UnionWith(terrain);
            trapCells.Clear(); trapCells.UnionWith(traps); soilTrapCells.Clear(); soilTrapCells.UnionWith(soil);
        }
    }

    internal void ProbeWaterBalanceForE2E(bool upgraded=false)
    {
        var water=allies.First(unit=>unit.data.skillType==SkillType.WaterHeal);
        int hp=water.currentHP,max=water.maxHP,attack=water.attack,defense=water.defense;
        bool ally=water.isAlly;
        try
        {
            foreach(bool side in new[]{true,false})
            {
                water.isAlly=side;
                water.SetBattleVitals(200,1,attack,defense);
                int expected=side&&upgraded?121:61;
                if(!SkillExecutor.TryExecute(water,water,this)||water.currentHP!=expected)
                    throw new System.Exception("Water must heal 30% base or 60% with full allied upgrades.");
                water.currentHP=190;
                if(!SkillExecutor.TryExecute(water,water,this)||water.currentHP!=200)
                    throw new System.Exception("Water must not overheal.");
            }
        }
        finally { water.isAlly=ally;water.SetBattleVitals(max,hp,attack,defense); }
    }
    internal void SeedStalemateForE2E()
    {
        foreach(var unit in allies.Concat(enemies))unit.ApplyStun(100);
    }
    internal void DefeatSideForE2E(bool ally)
    {
        if(IsDeploying)throw new System.InvalidOperationException("Battle must have started.");
        foreach(var unit in (ally?allies:enemies).ToArray())HandleDeath(unit);
    }
    internal void DefeatOxForE2E()
    {
        if (IsDeploying) throw new System.InvalidOperationException("Battle must have started.");
        HandleDeath(allies.First(unit => unit.data.characterId == 30));
    }

    internal void ProbeSupportSkills()
    {
        if (!IsDeploying || allies.Count != 6 || allies[0].data.skillType != SkillType.BellEcho)
            throw new System.InvalidOperationException("Support deployment fixture required.");
        void Check(bool value, string label)
        {
            if (!value) throw new System.InvalidOperationException("Support probe: " + label);
        }
        void Place(BattleCharacter unit, Vector2Int position)
        {
            if (gridMap.TryGetValue(unit.gridPos, out var current) && current == unit) gridMap.Remove(unit.gridPos);
            unit.gridPos = position;
            gridMap[position] = unit;
            unit.transform.position = gridCells[position.x, position.y].position;
        }
        var bell = allies[0]; var thread = allies[1]; var shadow = allies[2];
        var extraBell = allies[3]; var target = enemies[0]; var other = enemies[1];
        CharacterData old = extraBell.data;
        CharacterData echo = Instantiate(bell.data);
        bool force = SkillExecutor.ForceSkillActivationForVfxReview;
        var blocked = new Vector2Int(2, 3);
        bool wasWater = impassableCells.Contains(blocked);
        try
        {
            gridMap.Clear();
            Place(bell, new(1, 4)); Place(thread, new(2, 4)); Place(shadow, new(4, 4));
            Place(extraBell, new(3, 4)); Place(allies[4], new(0, 6)); Place(allies[5], new(5, 6));
            Place(target, new(2, 2)); Place(other, new(5, 0)); Place(enemies[2], new(4, 0));
            impassableCells.Remove(blocked);
            bell.isDead = true;
            int hp = target.currentHP;
            Check(SkillExecutor.TryExecute(thread, target, this), "reliable thread attack");
            int damage = hp - target.currentHP;
            Check(damage > 0 && target.gridPos == blocked, "damage and one-cell pull");
            target.currentHP = hp; Place(target, new(2, 2)); bell.isDead = false;
            extraBell.data = echo;
            SkillExecutor.ForceSkillActivationForVfxReview = true;
            Check(SkillExecutor.TryExecute(thread, target, this), "echo source action");
            Check(hp - target.currentHP == 2 * damage, "exactly one repeat despite two adjacent bells");
            Check(target.gridPos == blocked, "second pull cannot enter caster");
            SkillExecutor.ForceSkillActivationForVfxReview = false;
            bell.isDead = true; extraBell.isDead = true;
            target.currentHP = hp; Place(target, new(2, 2)); Place(allies[4], blocked);
            Check(SkillExecutor.TryExecute(thread, target, this) && target.gridPos == new Vector2Int(2, 2)
                && target.currentHP < hp, "occupied destination blocks pull but not damage");
            Place(allies[4], new(0, 6)); impassableCells.Add(blocked); target.currentHP = hp;
            Check(SkillExecutor.TryExecute(thread, target, this) && target.gridPos == new Vector2Int(2, 2)
                && target.currentHP < hp, "water blocks pull but not damage");
            Check(!SkillExecutor.TryExecute(thread, allies[4], this), "no friendly fire");
            Place(target, new(4, 3)); shadow.currentHP = 31;
            int maxHP = shadow.maxHP;
            Check(SkillExecutor.TryExecute(shadow, target, this), "copy through real skill dispatcher");
            Check(shadow.currentHP == 31 && shadow.maxHP == maxHP && shadow.OriginalData.characterId == 29
                && shadow.data.skillType == target.data.skillType, "copy preserves HP and identity");
            AddLog("新キャラ能力チェック完了");
        }
        finally
        {
            SkillExecutor.ForceSkillActivationForVfxReview = force;
            bell.isDead = false; extraBell.isDead = false;
            extraBell.data = old;
            Destroy(echo);
            if (!wasWater) impassableCells.Remove(blocked);
            else impassableCells.Add(blocked);
        }
    }

    internal BattleE2EBridge.BattleState CaptureE2EState()
    {
        var frames=GetComponentsInChildren<RectTransform>().Where(rect=>rect.name=="FusionMaterial"&&rect.gameObject.activeInHierarchy).ToArray();
        var glows=GetComponentsInChildren<UnityEngine.UI.Image>().Where(image=>image.name=="FusionPieceGlow"&&image.gameObject.activeInHierarchy).ToArray();
        bool FrameValid(RectTransform frame)
        {
            var cell=frame.parent as RectTransform;
            if(cell==null||gridCells==null||!gridCells.Cast<Transform>().Select(content=>content.parent).Contains(cell))return false;
            var outer=BattleE2EBridge.Bounds(cell);var inner=BattleE2EBridge.Bounds(frame);
            var image=frame.GetComponent<UnityEngine.UI.Image>();var outline=frame.GetComponent<UnityEngine.UI.Outline>();
            return inner.x>=outer.x-.0001f&&inner.y>=outer.y-.0001f&&inner.x+inner.w<=outer.x+outer.w+.0001f
                &&inner.y+inner.h<=outer.y+outer.h+.0001f&&image!=null&&!image.raycastTarget&&Mathf.Approximately(image.color.a,.08f)
                &&outline!=null&&outline.useGraphicAlpha&&outline.effectDistance.magnitude*UiReadability.Scale(GetComponentInParent<Canvas>())<=4.3f;
        }
        return new BattleE2EBridge.BattleState
        {
            fusionFrameCount=frames.Length,fusionFramesValid=frames.All(FrameValid),
            fusionGlowCount=glows.Length,
            active = gameObject.activeInHierarchy,
            paused = isPaused,
            header = BattleE2EBridge.Bounds(transform.Find("BattleInformationHeader") as RectTransform),
            playerSkillPlan = playerSkillPlan.Copy(), playerSkillOpen = playerSkillView != null,
            playerSkillFired = playerSkill.Fired, playerSkillActive = playerSkill.Active,
            playerSkillRemaining = playerSkill.Remaining,
            supportVfxCount = supportRoots.Count,
            supportTextures = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
                System.Linq.Enumerable.Where(GetComponentsInChildren<UnityEngine.UI.RawImage>(),
                    image => image.texture != null && image.color.a > .05f
                        && image.GetComponentInParent<SupportSkillVfxLifetime>() != null), image => image.texture.name)),
            deploying = IsDeploying,
            fusionOpen = fusionView != null,
            conflicts = fusionResolution?.conflicts.Count ?? 0,
            previewLinks = fusionPreview?.LinkCount ?? 0,
            fusionSummary = deploymentView?.FusionSummary ?? "",
            logs = logContent == null ? new string[0] : logContent.GetComponentsInChildren<TMPro.TMP_Text>().Select(text => text.text).ToArray(),
            resultText = resultText == null ? "" : resultText.text,
            resultTextOverflow = resultText != null && resultText.isTextOverflowing,
            battleId = gridCells == null ? 0 : gridCells[0, 0].GetInstanceID(),
            result = resultPanel != null && resultPanel.activeInHierarchy,
            won = resultText != null && resultText.text.Contains("勝利"),
            originalIds = deployedAllies.Select(data => data.characterId).ToArray(),
            units = gridMap.Values.Where(unit => unit != null && !unit.isDead).Select(unit => new BattleE2EBridge.Unit
            {
                id = unit.data.characterId, slot = unit.isAlly ? allies.IndexOf(unit) : -1, name = unit.data.characterName, ally = unit.isAlly,
                originalId = unit.OriginalData.characterId, transformed = unit.HasTransformed,
                x = unit.gridPos.x, y = unit.gridPos.y, hp = unit.currentHP,
                maxHP = unit.maxHP, level = unit.level, attackBuff = unit.AttackBuffPercent,
                icon = unit.data.icon != null && unit.data.enemyIcon != null,
                motif = unit.GetComponentInChildren<PieceMotifPresentation>(true)?.HasMotif ?? false,
                motifVisible = unit.GetComponentInChildren<PieceMotifPresentation>(true)?.IsVisible ?? false,
                motifAlpha = unit.GetComponentInChildren<PieceMotifPresentation>(true)?.VisibleAmount ?? 0f
            }).ToArray(),
            plan = fusionPlan,
            cells = gridCells == null ? new BattleE2EBridge.Cell[0] : Enumerable.Range(0, cols * rows).Select(index =>
            {
                int x = index % cols, y = index / cols;
                return new BattleE2EBridge.Cell { x = x, y = y, rect = BattleE2EBridge.Bounds(gridCells[x, y] as RectTransform) };
            }).ToArray()
        };
    }
}
#endif
