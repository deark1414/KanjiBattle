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
    public int numberPassiveBonus1 = 5;
    public int numberPassiveBonus2 = 10;
    public int numberPassiveBonus3 = 15;
    public int numberPassiveOneBonusPer = 5;
    public int numberPassiveOneBonusCap = 25;

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
