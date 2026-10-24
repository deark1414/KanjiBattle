using UnityEngine;
using System.Collections.Generic;

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
    Dragon,
    DoubleSlash,
    ScatterShot,
    BellEcho,
    ThreadPull,
    ShadowCopy,
    GuardAlly,
    ExperienceBonus,
    ChainLightning,
    WindShift,
    IronAdapt,
    Warrior,
    Sentinel,
    Forge,
    Wave, Magnet, Mountain, Coal, Fortress, Dice, Rain, Sand, Grass, Cocoon, WindChime, Lantern
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
    public bool isEvolved;
    public Sprite icon;
    public Sprite enemyIcon;

    [Header("カテゴリ")]
    public CharacterCategory category = CharacterCategory.None;

    [Header("ステータス")]
    [SerializeField] private int baseHP = 100;
    [SerializeField] private int baseAttack = 20;
    [SerializeField] private int baseDefense = 0;
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
    public int defenseGrowth = 1; // レベルごとの防御力増加量

    public int GetMaxHP(int level)
    {
        return baseHP + hpGrowth * (level - 1);
    }

    public int GetAttack(int level)
    {
        return baseAttack + attackGrowth * (level - 1);
    }

    public int GetDefense(int level)
    {
        return baseDefense + defenseGrowth * (level - 1);
    }

    public int GetUpgradeCost(int currentLevel)
    {
        int baseCost = 100;
        float growthRate = 1.2f;
        return Mathf.RoundToInt(baseCost * Mathf.Pow(growthRate, currentLevel));
    }

    public static CharacterData CreateBattleFusion(BattleFusionRules.Recipe recipe, IReadOnlyList<CharacterData> materials, int battleLevel)
    {
        if (recipe == null || materials == null || materials.Count != recipe.Materials.Length || battleLevel < 1)
            throw new System.ArgumentException("Invalid fusion materials or battle level.");
        foreach (CharacterData material in materials)
            if (material == null) throw new System.ArgumentException("Missing fusion material.");
        var result = CreateInstance<CharacterData>();
        result.hideFlags = HideFlags.HideAndDontSave;
        result.characterId = recipe.Id;
        result.characterName = recipe.Name;
        result.category = recipe.Category;
        result.skillType = recipe.Skill;
        result.skillChance = recipe.Skill == SkillType.WindChime ? 15 : 100;
        result.skillPower = 1f;
        float hp = 0, attack = 0, defense = 0;
        foreach (CharacterData material in materials)
        {
            hp += material.GetMaxHP(battleLevel);
            attack += material.GetAttack(battleLevel);
            defense += material.GetDefense(battleLevel);
        }
        Vector3 multipliers = recipe.StatMultipliers;
        result.baseHP = Mathf.Max(1, Mathf.RoundToInt(hp / materials.Count * multipliers.x));
        result.baseAttack = Mathf.Max(1, Mathf.RoundToInt(attack / materials.Count * multipliers.y));
        result.baseDefense = Mathf.Max(0, Mathf.RoundToInt(defense / materials.Count * multipliers.z));
        result.hpGrowth = result.attackGrowth = result.defenseGrowth = 0;
        CharacterArtCatalog.Apply(result);
        return result;
    }

    public static CharacterData CreateBattleCopy(string name, SkillType skill, int hp, int attack, int defense)
    {
        var result = CreateInstance<CharacterData>();
        result.hideFlags = HideFlags.HideAndDontSave;
        result.characterName = name; result.characterId = -20000;
        result.category = CharacterCategory.Nature; result.skillType = skill;
        result.baseHP = Mathf.Max(1, hp); result.baseAttack = Mathf.Max(1, attack);
        result.baseDefense = Mathf.Max(0, defense); result.skillChance = 0;
        result.hpGrowth = result.attackGrowth = result.defenseGrowth = 0;
        CharacterArtCatalog.Apply(result);
        return result;
    }
}
