using UnityEngine;

public enum FacilityEffectType
{
    GoldProduction,       // ゴールド生産効率UP
    SummonCostDown,       // 召喚コスト減少
    UpgradeCostDown,      // 強化コスト減少
    StagePointBoost,      // ステージポイント増加
    FormationSlot,        // 編成枠解放
    LevelCap,             // キャラのレベル上限解放
    CharacterUnlock,      // 新キャラ解放
}

public enum FacilityUnlockType
{
    Free,           // 最初から使用可能
    StagePoint  // 特定ステージクリア後にステージポイントで解放
}


[CreateAssetMenu(fileName = "FacilityData_", menuName = "Game/Facility Data")]
public class FacilityData : ScriptableObject
{
    public string facilityName;

    [Header("効果設定")]
    public FacilityEffectType effectType;

    [Header("解放条件")]
    public FacilityUnlockType unlockType = FacilityUnlockType.Free;
    public int requiredStageId = -1;       // 解放に必要なステージ
    public int unlockStagePointCost = 0;         // 解放コスト（ステージポイント）

    [Header("レベル制限")]
    public int maxLevel = 10;
    public int levelCapUnlockStageId = -1; // 上限解放に必要なステージ
    public int levelCapStagePointCost = 0; // 上限解放コスト（ステージポイント）

    [Header("コスト成長")]
    public int baseCost = 100;
    public float growthFactor = 1.2f;

    public int GetUpgradeCost(int currentLevel)
    {
        return Mathf.RoundToInt(baseCost * Mathf.Pow(growthFactor, currentLevel));
    }
}