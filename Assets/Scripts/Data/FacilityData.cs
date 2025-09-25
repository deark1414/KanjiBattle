using UnityEngine;
using System.Collections.Generic;

public enum FacilityEffectType
{
    GoldProduction,        // ゴールド生産効率UP
    SummonCostDown,        // 召喚コスト減少
    UpgradeCostDown,       // 強化コスト減少
    StagePointBoost,       // ステージポイント増加
    FormationSlot,         // 編成枠解放
    LevelCap,              // キャラのレベル上限解放
    CharacterUnlock,       // 新キャラ解放
    ChapterUnlock,         // 章解放
    BossUnlock,
    SummonRateUp  // 召喚確率上昇
}

public enum FacilityUnlockType
{
    Free,          // 最初から使用可能
    StagePoint,    // 特定ステージクリア後にステージポイントで解放
}

[System.Serializable]
public class LevelCapUnlockRequirement
{
    [Tooltip("レベル上限解放が可能になるステージID")]
    public int stageId = -1;

    [Tooltip("レベル上限解放に必要なステージポイント")]
    public int requiredStagePoints = 0;
}

[System.Serializable]
public class FacilityLevelCapRequirement
{
    [Tooltip("レベル上限解放が可能になるステージID")]
    public int stageId = -1;

    [Tooltip("レベル上限解放に必要なステージポイント")]
    public int requiredStagePoints = 0;
}

[CreateAssetMenu(fileName = "FacilityData_", menuName = "Game/Facility Data")]
public class FacilityData : ScriptableObject
{
    public string facilityName;

    [Header("効果設定")]
    /// <summary>
    /// 効果の種類を指定します。
    /// </summary>
    public FacilityEffectType effectType;

    [Header("解放条件")]
    /// <summary>
    /// 解放タイプを指定します。
    /// </summary>
    public FacilityUnlockType unlockType = FacilityUnlockType.Free;

    [Tooltip("解放に必要なステージID")]
    public int requiredStageId = -1;       // 解放に必要なステージ

    [Tooltip("解放コスト（ステージポイント）")]
    public int unlockStagePointCost = 0;         // 解放コスト（ステージポイント）

    [Header("レベル制限")]
    [Tooltip("施設の初期最大レベル")]
    public int initialMaxLevel = 1;

    [Tooltip("施設の最終最大レベル（絶対上限）")]
    public int finalMaxLevel = 10;

    [Header("施設レベル上限設定")]
    [Tooltip("1回の解放で増加するレベル上限の値（固定）")]
    public int levelCapIncreasePerUnlock = 5;

    [Header("レベル上限解放条件")]
    [Tooltip("ステージクリアとステージポイント消費で施設レベル上限を解放する条件リスト")]
    public List<FacilityLevelCapRequirement> facilityLevelCapUnlocks = new List<FacilityLevelCapRequirement>();

    [Header("コスト成長")]
    /// <summary>
    /// 施設強化の基礎コスト。
    /// </summary>
    public int baseCost = 100;

    /// <summary>
    /// コストの成長率（倍率）。
    /// </summary>
    public float growthFactor = 1.2f;

    [Header("章解放用")]
    [Tooltip("FacilityEffectTypeがChapterUnlockの場合に使用する章解放条件リスト")]
    public List<FacilityLevelCapRequirement> chapterUnlockRequirements = new List<FacilityLevelCapRequirement>();

    [Header("効果値")]
    [Tooltip("1レベルあたりの効果値")]
    public float effectPerLevel = 0.1f;


    [Header("召喚確率施設")]
    [Tooltip("召喚確率を上昇させるカテゴリ")]
    public CharacterCategory summonCategory = CharacterCategory.None;

    [Tooltip("1レベルあたりの召喚確率上昇値")]
    public float summonRatePerLevel = 0.01f; // Lvごとに +1% など

    public int GetUpgradeCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(growthFactor, currentLevel));
    }

    public float GetEffectValue(int level)
    {
        switch (effectType)
        {
            case FacilityEffectType.FormationSlot:
                return Mathf.RoundToInt(effectPerLevel * level);
            case FacilityEffectType.LevelCap:
                return levelCapIncreasePerUnlock * level;
            case FacilityEffectType.CharacterUnlock:
            case FacilityEffectType.ChapterUnlock:
                return -1f;
            default:
                return effectPerLevel * level;
        }
    }
}