using System.Collections.Generic;

[System.Serializable]
public class SkillEffectData
{
    public string effectTypeName;
    public SkillEffectType effectType;
    public float powerMultiplier = 1f;
    public int value = 0;
    public bool ignoreDefense = false;
}

[System.Serializable]
public class SkillData
{
    public string skillTypeName;
    public SkillType skillType;
    public float powerMultiplier = 1f;
    public int chanceOverride = -1;
    public string logMessage;
    public List<SkillEffectData> effects = new();

    // NumberPassive tuning (percent values).
    // Standard tier strength is the number of other living numeric types.
    // A party made entirely of 一 uses the dedicated 2-5 ally table instead.
    public int numberPassiveBonus1 = 15;
    public int numberPassiveBonus2 = 30;
    public int numberPassiveBonus3 = 50;
    public int numberPassiveOneBonus4 = 150;
    public int numberPassiveOneBonus5 = 300;

    // Mid: self-heal at the start of its side's turn.
    public int numberPassiveMidHealWeak = 5;
    public int numberPassiveMidHealMedium = 10;
    public int numberPassiveMidHealStrong = 15;

    // High: compounded attack bonus per completed round, capped by active tier.
    public int numberPassiveHighBonusPerRoundWeak = 3;
    public int numberPassiveHighBonusPerRoundMedium = 5;
    public int numberPassiveHighBonusPerRoundStrong = 10;
    public int numberPassiveHighBonusCapWeak = 100;
    public int numberPassiveHighBonusCapMedium = 200;
    public int numberPassiveHighBonusCapStrong = 300;

    // Dragon tuning.
    public int dragonRoarChance = -1;
}

public enum SkillEffectType
{
    Damage,
    Heal,
    Stun,
    PushBack,
    SoilTrap,
    Retreat,
    MultiHit,
    Charge,
    Counter,
    AreaCounter,
    DamageReduction,
    DragonBreath,
    DragonRoar
}
