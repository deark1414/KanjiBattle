using UnityEngine;

public static class BattleTrapRules
{
    public const float SoilTrapAttackMultiplier = 1.25f;
    public const int MinimumSoilTrapDamage = 12;
    public const int SoilTrapCountPerSkill = 2;

    public static int GetSoilTrapDamage(int casterAttack)
    {
        return Mathf.Max(MinimumSoilTrapDamage, Mathf.CeilToInt(casterAttack * SoilTrapAttackMultiplier));
    }
}
