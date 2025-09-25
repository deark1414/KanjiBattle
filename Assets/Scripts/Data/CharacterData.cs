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
    NumberPassive,  // 対象: Number1, Number2, Number3
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
    None, 
    Number1,   // 1,2,3
    Number2,   // 4,5,6
    Number3,   // 7,8,9
    Weapon,    // 剣, 槌 など
    Defense,   // 盾, 城
    Ranged,    // 石, 矢, 弓, 銃
    Nature,    // 火, 水, 木, 山
    Animal,    // 馬, 鳥, 虎
    Boss       // ボス専用（龍含む）
}

[CreateAssetMenu(fileName = "CharacterData_", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    public string characterName;
    public int characterId;
    public bool isBoss;
    public Sprite icon;

    [Header("カテゴリ")]
    public CharacterCategory category = CharacterCategory.None;

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
