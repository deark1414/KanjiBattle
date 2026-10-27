using System;

public static class PlayerExperienceCurve
{
    public const int Version = 1;
    public const int MaximumLevel = 50;
    private const double GrowthPerLevel = 1.16;
    private const double BattleRewardRate = 0.05;
    private const double EarlyBattleRewardBonus = 8;
    private const double MaximumBattleRewardRate = 0.30;

    public static int Required(int level)
    {
        level = Math.Max(1, Math.Min(MaximumLevel, level));
        return (int)Math.Round(10d * Math.Pow(GrowthPerLevel, level - 1), MidpointRounding.AwayFromZero);
    }

    public static int BattleReward(int enemyLevel)
    {
        double cost = Required(enemyLevel);
        // Keep early recruitment approachable without paying a full level every
        // couple of victories during the longer late-game recruitment loops.
        double reward = Math.Min(cost * MaximumBattleRewardRate, cost * BattleRewardRate + EarlyBattleRewardBonus);
        return Math.Max(2, (int)Math.Round(reward, MidpointRounding.AwayFromZero));
    }

    public static int LegacyRequired(int level)
    {
        int tier = Math.Max(0, Math.Min(9, (Math.Max(1, level) - 1) / 5));
        return 10 * (int)Math.Pow(3, tier);
    }

    public static int ConvertLegacyExperience(int level, int experience)
    {
        long remaining = Math.Max(0, experience);
        double converted = 0d;
        level = Math.Max(1, Math.Min(MaximumLevel, level));
        // Banked experience can span several locked levels. Preserve those whole
        // levels first, then the fractional progress toward the following level.
        while (level < MaximumLevel && remaining >= LegacyRequired(level))
        {
            remaining -= LegacyRequired(level);
            converted += Required(level);
            level++;
        }
        converted += remaining * (double)Required(level) / LegacyRequired(level);
        return (int)Math.Min(int.MaxValue, Math.Ceiling(converted));
    }
}
