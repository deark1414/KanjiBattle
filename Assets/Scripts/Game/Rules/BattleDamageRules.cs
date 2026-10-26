using System.Collections.Generic;
using UnityEngine;

public enum BattleDamageOrigin { Direct, Transferred, Reflected, Returned }

public static class BattleDamageRules
{
    public static bool StopsProjectile(BattleCharacter unit) => unit != null && !unit.isDead
        && unit.currentHP > 0 && unit.data != null && unit.data.skillType == SkillType.Wall;
    public static int ReduceByWall(int damage, float reduction) =>
        Mathf.Max(0, Mathf.CeilToInt(damage * (1 - Mathf.Clamp01(reduction))));

    // Traverse from the muzzle, including the dead zone: an adjacent wall still stops a bullet.
    public static List<Vector2Int> GunLine(BattleCharacter caster, Vector2Int direction,
        IReadOnlyDictionary<Vector2Int, BattleCharacter> grid, int cols, int rows)
    {
        var cells = BattleRangeService.GetLineWithinBoard(caster.gridPos, direction, rows + cols, cols, rows);
        int blocker = cells.FindIndex(cell => grid.TryGetValue(cell, out var unit)
            && StopsProjectile(unit) && unit.isAlly != caster.isAlly);
        if (blocker >= 0) cells.RemoveRange(blocker + 1, cells.Count - blocker - 1);
        return cells;
    }

    public static bool HasShield(SkillType skill) => skill is SkillType.Shield or SkillType.Warrior or SkillType.Sentinel or SkillType.Fortress;
    public static int ReduceByShield(int damage,float effect=1) => damage <= 0 ? 0 : Mathf.Max(1, Mathf.FloorToInt(damage * (1-Mathf.Min(.5f,.2f*effect))));

    // Redirect 80% of the original hit; the ox pays 3/8 of that share (30% of the hit).
    private const int ProjectileGuardDamageNumerator = 375;
    private const int ProjectileGuardDamageScale = 1000;

    public static float GuardRatio(float ratio, bool projectileGuard) =>
        Mathf.Clamp(ratio, 0, projectileGuard ? .9f : 1f);

    public static int GuardShare(int damage, int protectorHP, float ratio, bool projectileGuard = false)
    {
        long capacity = Mathf.Max(0, protectorHP);
        if (projectileGuard) capacity = capacity * ProjectileGuardDamageScale / ProjectileGuardDamageNumerator;
        // Preserve skill-tree percentages across float rounding (for example 65%).
        double requested = System.Math.Floor(Mathf.Max(0, damage) * System.Math.Round(GuardRatio(ratio, projectileGuard), 6));
        return (int)System.Math.Min(capacity, System.Math.Min(Mathf.Max(0, damage), requested));
    }

    public static int GuardLoss(int shared, bool projectileGuard) => projectileGuard
        ? (int)((System.Math.Max(0, shared) * (long)ProjectileGuardDamageNumerator + ProjectileGuardDamageScale - 1) / ProjectileGuardDamageScale)
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
