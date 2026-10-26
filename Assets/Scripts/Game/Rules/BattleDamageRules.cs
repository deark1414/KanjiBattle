using System.Collections.Generic;
using UnityEngine;

public enum BattleDamageOrigin { Direct, Transferred, Reflected, Returned }

public static class BattleDamageRules
{
    public static bool HasShield(SkillType skill) => skill is SkillType.Shield or SkillType.Warrior or SkillType.Sentinel or SkillType.Fortress;
    public static int ReduceByShield(int damage,float effect=1) => damage <= 0 ? 0 : Mathf.Max(1, Mathf.FloorToInt(damage * (1-Mathf.Min(.5f,.2f*effect))));

    public static int GuardShare(int damage, int protectorHP, float ratio) =>
        Mathf.Min(Mathf.Max(0, protectorHP), Mathf.FloorToInt(Mathf.Max(0, damage) * Mathf.Clamp01(ratio)));

    public static BattleCharacter FindProtector(BattleCharacter target, IEnumerable<BattleCharacter> candidates)
    {
        if (target == null || candidates == null) return null;
        BattleCharacter best = null;
        foreach (BattleCharacter unit in candidates)
        {
            if (unit == null || unit == target || unit.isDead || unit.currentHP <= 0 || unit.data == null
                || unit.isAlly != target.isAlly || unit.data.skillType != SkillType.GuardAlly
                || unit.GetEffectiveSkillPower() <= 0 || BattleRangeService.Distance(unit.gridPos, target.gridPos) != 1) continue;
            if (best == null || unit.GetEffectiveSkillPower() > best.GetEffectiveSkillPower()
                || (unit.GetEffectiveSkillPower() == best.GetEffectiveSkillPower() && unit.InstanceId < best.InstanceId)) best = unit;
        }
        return best;
    }
}
