#if KANJI_BATTLE_E2E
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
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
    internal void DefeatExperienceUnit()
    {
        if (IsDeploying) throw new System.InvalidOperationException("Battle must have started.");
        HandleDeath(allies.First(unit => unit.data.skillType == SkillType.ExperienceBonus));
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
        bool GlowValid(UnityEngine.UI.Image glow)
        {
            var source=glow.transform.parent.GetComponent<UnityEngine.UI.Image>();
            if(source==null||source.sprite==null||glow.sprite!=source.sprite||glow.raycastTarget)return false;
            var outer=BattleE2EBridge.Bounds(source.rectTransform);var inner=BattleE2EBridge.Bounds(glow.rectTransform);
            return inner.x>=outer.x-.0001f&&inner.y>=outer.y-.0001f&&inner.x+inner.w<=outer.x+outer.w+.0001f
                &&inner.y+inner.h<=outer.y+outer.h+.0001f&&glow.color.a<=.12f&&!glow.GetComponent<CanvasGroup>().blocksRaycasts;
        }
        bool FrameValid(RectTransform frame)
        {
            var cell=frame.parent as RectTransform;
            if(cell==null||gridCells==null||!gridCells.Cast<Transform>().Select(content=>content.parent).Contains(cell)||frame.GetComponent<UnityEngine.UI.Image>()!=null)return false;
            var outer=BattleE2EBridge.Bounds(cell);var inner=BattleE2EBridge.Bounds(frame);
            var edges=frame.GetComponentsInChildren<UnityEngine.UI.Image>();
            return inner.x>=outer.x-.0001f&&inner.y>=outer.y-.0001f&&inner.x+inner.w<=outer.x+outer.w+.0001f
                &&inner.y+inner.h<=outer.y+outer.h+.0001f&&edges.Length==4
                &&edges.All(edge=>!edge.raycastTarget&&(edge.rectTransform.rect.width<=frame.rect.width*.25f||edge.rectTransform.rect.height<=frame.rect.height*.25f));
        }
        return new BattleE2EBridge.BattleState
        {
            fusionFrameCount=frames.Length,fusionFramesValid=frames.All(FrameValid),
            fusionGlowCount=glows.Length,fusionGlowsValid=glows.All(GlowValid),
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
