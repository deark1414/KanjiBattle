using System.Collections.Generic;
using UnityEngine;

public enum BattleDamageOrigin { Direct, Transferred, Reflected, Returned }

public static class BattleDamageRules
{
    public static bool HasShield(SkillType skill) => skill is SkillType.Shield or SkillType.Warrior or SkillType.Sentinel or SkillType.Fortress;
    public static int ReduceByShield(int damage,float effect=1) => damage <= 0 ? 0 : Mathf.Max(1, Mathf.FloorToInt(damage * (1-Mathf.Min(.5f,.2f*effect))));

    public const int ProjectileGuardDamagePercent = 70;

    public static int GuardShare(int damage, int protectorHP, float ratio, bool projectileGuard = false)
    {
        long capacity = Mathf.Max(0, protectorHP);
        if (projectileGuard) capacity = capacity * 100 / ProjectileGuardDamagePercent;
        // Preserve skill-tree percentages across float rounding (for example 65%).
        double requested = System.Math.Floor(Mathf.Max(0, damage) * System.Math.Round(Mathf.Clamp01(ratio), 6));
        return (int)System.Math.Min(capacity, System.Math.Min(Mathf.Max(0, damage), requested));
    }

    public static int GuardLoss(int shared, bool projectileGuard) => projectileGuard
        ? (int)((System.Math.Max(0, shared) * (long)ProjectileGuardDamagePercent + 99) / 100)
        : Mathf.Max(0, shared);

    public static BattleCharacter FindProtector(BattleCharacter target, IEnumerable<BattleCharacter> candidates, bool projectileHit = false)
    {
        if (target == null || candidates == null) return null;
        BattleCharacter best = null;
        foreach (BattleCharacter unit in candidates)
        {
            if (unit == null || unit == target || unit.isDead || unit.currentHP <= 0 || unit.data == null
                || unit.isAlly != target.isAlly
                || !(unit.data.skillType == SkillType.GuardAlly || (projectileHit && unit.data.skillType == SkillType.ProjectileGuard))
                || unit.GetEffectiveSkillPower() <= 0 || BattleRangeService.Distance(unit.gridPos, target.gridPos) != 1) continue;
            if (best == null || unit.GetEffectiveSkillPower() > best.GetEffectiveSkillPower()
                || (unit.GetEffectiveSkillPower() == best.GetEffectiveSkillPower()
                    && ((unit.data.skillType == SkillType.ProjectileGuard && best.data.skillType != SkillType.ProjectileGuard)
                        || (unit.data.skillType == best.data.skillType && unit.InstanceId < best.InstanceId)))) best = unit;
        }
        return best;
    }
}
