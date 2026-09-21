using System.Collections.Generic;
using UnityEngine;

public static class NumberPassiveRules
{
    public static int GetStandardStrength(IEnumerable<string> livingNumberNames)
    {
        var names = new HashSet<string>();
        foreach (string name in livingNumberNames)
        {
            if (!string.IsNullOrEmpty(name)) names.Add(name);
        }
        return Mathf.Clamp(names.Count - 1, 0, 3);
    }

    public static int GetStrengthFromCount(int count)
    {
        return Mathf.Clamp(count, 0, 3);
    }

    public static int GetTierValue(int strength, int weak, int medium, int strong)
    {
        return strength switch
        {
            1 => weak,
            2 => medium,
            3 => strong,
            _ => 0
        };
    }

    public static int GetOneOnlyAttackBonus(int oneCount, int weak, int medium, int four, int fiveOrMore)
    {
        return oneCount switch
        {
            2 => weak,
            3 => medium,
            4 => four,
            >= 5 => fiveOrMore,
            _ => 0
        };
    }

    public static int GetCompoundedAttackBonus(int percentPerRound, int completedRounds, int capPercent)
    {
        if (percentPerRound <= 0 || completedRounds <= 0 || capPercent <= 0) return 0;
        float multiplier = Mathf.Pow(1f + percentPerRound / 100f, completedRounds);
        return Mathf.Min(capPercent, Mathf.RoundToInt((multiplier - 1f) * 100f));
    }
}
