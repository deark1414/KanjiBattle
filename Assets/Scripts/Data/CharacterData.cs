using UnityEngine;

public enum SkillType
{
    None,
    Slash,
    StunBlow,
    Counter,
    AreaCounter,
    Armor,
    Heal,
    NumberPassive,
    Arrow,
    Gun,
    Spear,
    Stone,
    Shield,
    Wall,
    Soil,
    Fireball,
    WoodPush,
    WaterHeal,
    HorseCharge,
    BirdRetreat,
    TigerTwinClaw,
    Dragon
}

public enum CharacterCategory
{
    Other,
    Number
}

[CreateAssetMenu(fileName = "CharacterData_", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public Sprite icon;

    [Header("カテゴリ")]
    public CharacterCategory category = CharacterCategory.Other;

    [Header("ステータス")]
    [SerializeField] private int baseHP = 100;
    [SerializeField] private int baseAttack = 20;
    public int production = 0;

    [Header("スキル")]
    public SkillType skillType = SkillType.None;
    [Range(0, 100)] public int skillChance = 10;
    public float skillPower = 1.0f;

    [Header("成長")]
    public int level = 1;                 // 現在のレベル
    public int maxLevel = 50;             // 上限
    public int attackGrowth = 2;          // レベルごとの攻撃力増加量
    public int hpGrowth = 10;             // レベルごとのHP増加量

    public int GetMaxHP(int level)
    {
        return baseHP + hpGrowth * (level - 1);
    }

    public int GetAttack(int level)
    {
        return baseAttack + attackGrowth * (level - 1);
    }

    public int GetUpgradeCost(int currentLevel)
    {
        int baseCost = 100;
        float growthRate = 1.2f;
        return Mathf.RoundToInt(baseCost * Mathf.Pow(growthRate, currentLevel));
    }
}
