using System;
using UnityEngine;

public static class BattleRewardRegression
{
    public static void Run()
    {
        var cow = ScriptableObject.CreateInstance<CharacterData>();
        var bull = ScriptableObject.CreateInstance<CharacterData>();
        var ordinary = ScriptableObject.CreateInstance<CharacterData>();
        try
        {
            cow.skillType = bull.skillType = SkillType.ExperienceBonus;
            cow.skillPower = 1.5f; bull.skillPower = 2f;
            ordinary.skillPower = 100f;
            Check(BattleRewardRules.ArmyExperienceMultiplier(null) == 1f, "no party has no bonus");
            Check(BattleRewardRules.ArmyExperienceMultiplier(new[] { ordinary, null }) == 1f, "other powers ignored");
            Check(BattleRewardRules.ArmyExperienceMultiplier(new[] { cow, cow }) == 1.5f, "duplicate bonuses do not stack");
            Check(BattleRewardRules.ArmyExperienceMultiplier(new[] { cow, bull, cow }) == 2f, "highest multiplier only");
            Check(BattleRewardRules.CalculateExperience(3, 1f, new[] { cow }) == 5, "bonus rounded up once");
            Check(BattleRewardRules.CalculateExperience(2, 1.1f, new[] { cow }) == 4, "no intermediate rounding");
            Check(BattleRewardRules.CalculateExperience(10, 1.2f, null) == 12, "existing float facility rounding preserved");
            Check(BattleRewardRules.CalculateExperience(10, 1.2f, new[] { cow }) == 18, "facility and army multiply");
            Check(BattleRewardRules.CalculateExperience(int.MaxValue, 2f, new[] { bull }) == int.MaxValue, "reward cannot overflow");
            Check(BattleMovementRules.GetMaxSteps(CharacterCategory.Animal) == 2, "animal movement preserved");
            cow.characterName = "丑"; cow.category = CharacterCategory.Animal;
            Check(BattleFusionRules.Candidates(new[] { cow, cow }).Count == 0, "basic animals do not gain a fusion recipe");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cow);
            UnityEngine.Object.DestroyImmediate(bull);
            UnityEngine.Object.DestroyImmediate(ordinary);
        }
    }

    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException("[BattleRewardRegression] " + label);
    }
}
