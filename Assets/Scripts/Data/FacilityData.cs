using UnityEngine;
using System.Collections.Generic;

public enum FacilityEffectType
{
    StagePointBoost = 3,
    FormationSlot = 4,
    Recruitment = 10,
    Training = 11,
    BattleSpeed = 12,
    StageRetry = 13
}

public enum FacilityUnlockType
{
    Free,          // 最初から使用可能
    StagePoint,    // 特定ステージクリア後にステージポイントで解放
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
    public int facilityId;
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

    [Tooltip("各レベルへの強化に必要なStage Point。指定時はbaseCost/growthFactorより優先します。")]
    public List<int> upgradeStagePointCosts = new List<int>();

    [Header("効果値")]
    [Tooltip("1レベルあたりの効果値")]
    public float effectPerLevel = 0.1f;

    public int GetUpgradeCost(int currentLevel)
    {
        if (upgradeStagePointCosts != null && currentLevel >= 0 && currentLevel < upgradeStagePointCosts.Count)
        {
            return Mathf.Max(0, upgradeStagePointCosts[currentLevel]);
        }

        return Mathf.RoundToInt(baseCost * Mathf.Pow(growthFactor, currentLevel));
    }

    public float GetEffectValue(int level)
    {
        return effectType == FacilityEffectType.FormationSlot
            ? Mathf.RoundToInt(effectPerLevel * level)
            : effectPerLevel * level;
    }
}
