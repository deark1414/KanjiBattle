using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class FusionSkillExecutor
{
    public static bool UsesFocusedShot(int targets) => targets > 0 && targets < 3;

    public static List<BattleCharacter> ScatterVictims(BattleCharacter caster, BattleCharacter target, BattleManager battle) =>
        BattleRangeService.GetEffectCells(SkillType.ScatterShot, caster.gridPos, target.gridPos, battle.Cols, battle.Rows)
            .Where(cell => battle.gridMap.TryGetValue(cell, out var unit) && unit != null && !unit.isDead
                && unit.currentHP > 0 && unit.isAlly != caster.isAlly)
            .Select(cell => battle.gridMap[cell]).ToList();

    public static BattleCharacter FindScatterTarget(BattleManager battle, BattleCharacter caster, IEnumerable<BattleCharacter> candidates) =>
        candidates.OrderByDescending(target => ScatterVictims(caster,target,battle).Count >= 3)
            .ThenBy(target => BattleRangeService.Distance(caster.gridPos,target.gridPos))
            .ThenBy(target => target.InstanceId).FirstOrDefault();

    public static bool Execute(BattleCharacter caster, BattleCharacter target, BattleManager battle)
    {
        if (caster == null || target == null || battle == null || caster.isDead || target.isDead
            || caster.currentHP <= 0 || target.currentHP <= 0 || caster.isAlly == target.isAlly) return false;
        SkillType skill = caster.data.skillType;
        if (skill is not (SkillType.DoubleSlash or SkillType.ScatterShot)
            || !BattleRangeService.CanTargetCell(skill, caster.gridPos, target.gridPos, battle.Cols, battle.Rows)) return false;
        var victims = new List<BattleCharacter>();
        foreach (Vector2Int cell in BattleRangeService.GetEffectCells(skill, caster.gridPos, target.gridPos, battle.Cols, battle.Rows))
            if (battle.gridMap.TryGetValue(cell, out BattleCharacter victim) && victim != null
                && !victim.isDead && victim.currentHP > 0 && victim.isAlly != caster.isAlly) victims.Add(victim);
        if (victims.Count == 0) return false;
        bool focused = skill == SkillType.ScatterShot && UsesFocusedShot(victims.Count);
        if (focused) victims = new List<BattleCharacter> { target };
        caster.UpdateDirection(target.gridPos - caster.gridPos);
        battle.PlayFusionSkillVfx(caster, target, skill, focused);
        int hits = skill == SkillType.DoubleSlash ? 2 : 1;
        float power = skill == SkillType.DoubleSlash ? 0.75f : focused ? 2.4f : 0.8f;
        int damage = Mathf.Max(1, Mathf.RoundToInt(caster.GetEffectiveAttack(battle) * caster.GetEffectiveSkillPower() * power));
        for (int hit = 0; hit < hits; hit++)
        foreach (BattleCharacter victim in victims)
        {
            if (victim == null || victim.isDead) continue;
            victim.TakeDamage(damage, battle, caster, isBasicAttack: false);
        }
        battle.AddLog($"{caster.data.characterName} の{(skill == SkillType.ScatterShot ? focused ? "集弾" : "散弾" : "攻撃")}！", Color.yellow);
        return true;
    }
}
