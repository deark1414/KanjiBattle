using System;
using System.Collections.Generic;

public static class BattleRewardRules
{
    public static float ArmyExperienceMultiplier(IEnumerable<CharacterData> deployedParty)
    {
        float multiplier = 1f;
        if (deployedParty == null) return multiplier;
        foreach (CharacterData data in deployedParty)
            if (data != null && data.skillType == SkillType.ExperienceBonus)
                multiplier = Math.Max(multiplier, 1+(data.skillPower-1)*CharacterInvestment.Effect(data));
        return multiplier;
    }

    public static int CalculateExperience(int baseReward, float facilityMultiplier, IEnumerable<CharacterData> deployedParty)
    {
        float amount = Math.Max(0, baseReward) * Math.Max(0f, facilityMultiplier)
            * ArmyExperienceMultiplier(deployedParty);
        return (int)Math.Min(int.MaxValue, Math.Max(1d, Math.Ceiling(amount)));
    }
}
