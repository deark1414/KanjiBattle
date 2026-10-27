using System;
using System.Collections.Generic;
using UnityEngine;

public static class ElementSkillRegression
{
    public static void Run()
    {
        Check(ElementSkillExecutor.ChooseIronForm(50, 100, 3, true) == IronForm.Shield, "half HP prioritizes shield");
        Check(ElementSkillExecutor.ChooseIronForm(51, 100, 2, true) == IronForm.Sword, "multiple enemies prioritize sword");
        Check(ElementSkillExecutor.ChooseIronForm(51, 100, 1, true) == IronForm.Spear, "single target selects spear");
        Check(ElementSkillExecutor.ChooseIronForm(100, 100, 0, false) == IronForm.Sword, "approach with sword");
        Check(BattleRangeService.CanTargetCell(SkillType.ChainLightning, new(0, 0), new(3, 2), 6, 7), "lightning radius three");
        Check(!BattleRangeService.CanTargetCell(SkillType.ChainLightning, new(0, 0), new(4, 0), 6, 7), "lightning range limit");
        var targets = new List<Vector2Int> { new(0, 0) };
        var enemies = new HashSet<Vector2Int>(targets);
        List<Vector2Int> Move(CharacterCategory category, int modifier, int startY) => BattleMovementPlanner.FindNextMove(
            new(0, startY), targets, SkillType.None, category, null, enemies, null, 6, 7, modifier);
        Check(Move(CharacterCategory.Weapon, 1, 3).Count == 3, "tailwind grants two steps");
        Check(Move(CharacterCategory.Weapon, -1, 4).Count == 2, "headwind minimum one step");
        Check(Move(CharacterCategory.Animal, -1, 4).Count == 2, "animal slowed to one step");
        Check(Move(CharacterCategory.Animal, 1, 4).Count == 4, "animal boosted to three steps");
        var host = new GameObject("ElementRegression", typeof(RectTransform));
        host.SetActive(false);
        var data = ScriptableObject.CreateInstance<CharacterData>();
        try
        {
            var battle = host.AddComponent<BattleManager>();
            var unit = host.AddComponent<BattleCharacter>();
            data.skillType = SkillType.IronAdapt; unit.Init(data, new(0, 5), true);
            battle.gridMap[unit.gridPos] = unit;
            unit.IronForm = IronForm.Shield;
            unit.TakeDamage(40, null, ignoreDefense: true);
            Check(unit.currentHP == unit.maxHP - 30, "shield form mitigation executes");
            unit.currentHP = unit.maxHP;
            unit.TakeDamage(40, null, origin: BattleDamageOrigin.Transferred);
            Check(unit.currentHP == unit.maxHP - 40, "transferred damage bypasses form");
            unit.WindBoost = unit.WindSlow = true;
            Check(unit.MovementModifier == 0, "opposing effects cancel");
            ElementSkillExecutor.EndSideTurn(battle, false);
            Check(unit.WindSlow && unit.WindBoost, "other side ending cannot clear pending effects");
            ElementSkillExecutor.EndSideTurn(battle, true);
            Check(!unit.WindSlow && !unit.WindBoost, "effects expire after affected side acts");
            unit.IronForm = IronForm.Spear;
            Check(ElementSkillExecutor.AttackSkill(unit) == SkillType.Spear, "form has one attack geometry");
            data.skillType = SkillType.WindShift;
            Check(SkillExecutor.TryExecuteDetailed(unit, unit, null).Failure == SkillExecutionFailure.PassiveOrReactiveSkill,
                "wind does not consume normal action");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException("[ElementSkillRegression] " + label);
    }
}
