using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum IronForm { Sword, Spear, Shield }

public static class ElementSkillExecutor
{
    public static SkillType AttackSkill(BattleCharacter unit) => BattleActionRules.GetAttackSkill(unit);

    public static void SelectIronForm(BattleCharacter unit, BattleManager battle)
    {
        BattleCharacter target = TargetingService.FindSwordTarget(battle, unit);
        int count = target == null ? 0 : TargetingService.GetSwordWedgeTargets(battle, unit, target.gridPos - unit.gridPos).Count;
        IronForm form = ChooseIronForm(unit.currentHP, unit.maxHP, count, TargetingService.FindSpearTarget(battle, unit) != null);
        if (unit.IronForm != form)
        {
            string label = form == IronForm.Sword ? "剣形態" : form == IronForm.Spear ? "槍形態" : "盾形態";
            battle.ShowFloatingText(unit, label, Color.white);
            battle.AddLog($"{unit.DisplayName} が{label}へ変形");
        }
        unit.IronForm = form;
    }

    public static IronForm ChooseIronForm(int hp, int maxHP, int wedgeCount, bool spearTarget) =>
        (long)hp * 2 <= maxHP ? IronForm.Shield : wedgeCount >= 2 ? IronForm.Sword
            : spearTarget ? IronForm.Spear : IronForm.Sword;

    public static void EndSideTurn(BattleManager battle, bool ally)
    {
        foreach (BattleCharacter unit in battle.gridMap.Values)
            if (unit != null && unit.isAlly == ally) unit.WindBoost = unit.WindSlow = false;
    }

    public static bool ExecuteLightning(BattleCharacter caster, BattleCharacter target, BattleManager battle)
    {
        if (battle == null || !Living(target) || target.isAlly == caster.isAlly
            || !BattleRangeService.CanTargetCell(SkillType.ChainLightning, caster.gridPos, target.gridPos, battle.Cols, battle.Rows)) return false;
        var hit = new HashSet<BattleCharacter>();
        float power = 1f;
        while (Living(caster) && Living(target) && hit.Count < 4)
        {
            hit.Add(target);
            Vector2Int last = target.gridPos;
            battle.PlayElementEffect(target, "電", new Color(1f, 0.9f, 0.25f));
            target.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(caster.GetEffectiveAttack(battle)
                * caster.GetEffectiveSkillPower() * power)), battle, caster, actionLabel: $"{caster.DisplayName} の連鎖電撃");
            var next = battle.gridMap.Values.Where(unit => Living(unit) && unit.isAlly != caster.isAlly
                && !hit.Contains(unit) && BattleRangeService.Distance(last, unit.gridPos) == 1).ToList();
            target = next.Count == 0 ? null : next[Random.Range(0, next.Count)];
            power *= 0.75f;
        }
        battle.AddLog($"{caster.DisplayName} の連鎖電撃！ {hit.Count}体に命中");
        return hit.Count > 0;
    }

    public static void ApplyWind(BattleManager battle, bool ally, ISet<Vector2Int> terrain)
    {
        var winds = battle.gridMap.Values.Where(unit => Living(unit) && unit.isAlly == ally
            && unit.data.skillType == SkillType.WindShift).ToArray();
        foreach (BattleCharacter wind in winds)
        {
            if (wind.IsStunned()) continue;
            var friends = battle.gridMap.Values.Where(unit => Living(unit) && unit.isAlly == ally).ToList();
            var enemies = battle.gridMap.Values.Where(unit => Living(unit) && unit.isAlly != ally).ToList();
            if (enemies.Count == 0) continue;
            var friendCells = new HashSet<Vector2Int>(friends.Select(unit => unit.gridPos));
            var enemyCells = new HashSet<Vector2Int>(enemies.Select(unit => unit.gridPos));
            bool Helps(BattleCharacter unit)
            {
                if (unit.IsStunned() || unit.WindBoost || unit.EligibleTurn > battle.CurrentBattleTurn) return false;
                SkillType skill = unit.data.skillType == SkillType.WaterHeal ? SkillType.None : AttackSkill(unit);
                var normal = BattleMovementPlanner.FindNextMove(unit.gridPos, enemyCells.ToList(), skill,
                    unit.data.category, friendCells, enemyCells, terrain, battle.Cols, battle.Rows, unit.MovementModifier);
                if (normal == null) return false;
                var boosted = BattleMovementPlanner.FindNextMove(unit.gridPos, enemyCells.ToList(), skill,
                    unit.data.category, friendCells, enemyCells, terrain, battle.Cols, battle.Rows, unit.MovementModifier + 1);
                return boosted != null && boosted.Count > normal.Count && enemyCells.Any(cell =>
                    BattleRangeService.CanTargetCell(skill, boosted[boosted.Count - 1], cell, battle.Cols, battle.Rows, friendCells));
            }
            BattleCharacter recipient = friends.FirstOrDefault(unit => unit != wind && Helps(unit));
            if (recipient != null || Helps(wind))
            {
                wind.WindBoost = true;
                recipient ??= friends.FirstOrDefault(unit => unit != wind);
                if (recipient != null) recipient.WindBoost = true;
                battle.PlayElementEffect(wind, "追風", new Color(0.45f, 1f, 0.75f));
                if (recipient != null) battle.PlayElementEffect(recipient, "移動+1", new Color(0.45f, 1f, 0.75f));
                battle.AddLog($"{wind.DisplayName} の追風！ 自分と味方の移動+1");
            }
            else
            {
                var targets = enemies.Where(unit => !unit.WindSlow).ToList();
                if (targets.Count == 0) continue;
                BattleCharacter victim = targets[Random.Range(0, targets.Count)];
                victim.WindSlow = true;
                battle.PlayElementEffect(victim, "向風", new Color(0.6f, 0.8f, 1f));
                battle.AddLog($"{wind.DisplayName} の向風！ {victim.DisplayName} の移動-1（最低1）");
            }
        }
    }

    private static bool Living(BattleCharacter unit) => unit != null && !unit.isDead && unit.currentHP > 0;
}
