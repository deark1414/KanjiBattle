using System.Collections.Generic;
using UnityEngine;

public static class FusionSkillExecutor
{
    public static bool Execute(BattleCharacter caster, BattleCharacter target, BattleManager battle)
    {
        if (caster == null || target == null || battle == null || caster.isDead || target.isDead
            || caster.isAlly == target.isAlly) return false;
        SkillType skill = caster.data.skillType;
        if (skill is not (SkillType.DoubleSlash or SkillType.ScatterShot)
            || !BattleRangeService.CanTargetCell(skill, caster.gridPos, target.gridPos, battle.Cols, battle.Rows)) return false;
        var victims = new List<BattleCharacter>();
        foreach (Vector2Int cell in BattleRangeService.GetEffectCells(skill, caster.gridPos, target.gridPos, battle.Cols, battle.Rows))
            if (battle.gridMap.TryGetValue(cell, out BattleCharacter victim) && victim != null
                && !victim.isDead && victim.isAlly != caster.isAlly) victims.Add(victim);
        if (victims.Count == 0) return false;
        caster.UpdateDirection(target.gridPos - caster.gridPos);
        battle.PlayFusionSkillVfx(caster, target, skill);
        int hits = skill == SkillType.DoubleSlash ? 2 : 1;
        float power = skill == SkillType.DoubleSlash ? 0.75f : 0.8f;
        int damage = Mathf.Max(1, Mathf.RoundToInt(caster.GetEffectiveAttack(battle) * caster.GetEffectiveSkillPower() * power));
        for (int hit = 0; hit < hits; hit++)
        foreach (BattleCharacter victim in victims)
        {
            if (victim == null || victim.isDead) continue;
            victim.TakeDamage(damage, battle, caster, isBasicAttack: false);
        }
        battle.AddLog($"{caster.data.characterName} の攻撃！", Color.yellow);
        return true;
    }
}
