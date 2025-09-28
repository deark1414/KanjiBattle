using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum FacilityState
{
    Locked,     // 未解放
    Available,  // 解放済み、強化可能
    Maxed       // 最大レベル到達
}

public class FacilityManager : MonoBehaviour
{
    public static FacilityManager Instance;

    public event System.Action<CharacterCategory> OnSummonCategoryUnlocked;

    [SerializeField] private List<FacilityData> facilities = new List<FacilityData>();
    [SerializeField] private CharacterDatabase characterDatabase; // ★Inspectorで設定

    private Dictionary<FacilityData, int> facilityLevels = new Dictionary<FacilityData, int>();
    private HashSet<FacilityData> unlockedFacilities = new HashSet<FacilityData>();
    private Dictionary<FacilityData, int> facilityCapUnlockCount = new Dictionary<FacilityData, int>();

    private Queue<CharacterData> characterUnlockQueue = new Queue<CharacterData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;

        unlockedFacilities = new HashSet<FacilityData>();
        foreach (var f in facilities)
        {
            if (!facilityLevels.ContainsKey(f))
                facilityLevels[f] = 0; // Lv0
            if (!facilityCapUnlockCount.ContainsKey(f))
                facilityCapUnlockCount[f] = 0;

            // Auto-unlock Free facilities at startup
            if (f.unlockType == FacilityUnlockType.Free)
            {
                if (!unlockedFacilities.Contains(f))
                {
                    unlockedFacilities.Add(f);
                    if (!facilityLevels.ContainsKey(f))
                        facilityLevels[f] = 0;
                    Debug.Log($"[FacilityManager] {f.facilityName} was auto-unlocked as a Free facility at startup.");
                }
            }
        }

        // 初期化処理の後に
        if (characterDatabase != null && characterDatabase.characters != null)
        {
            foreach (var c in characterDatabase.characters
                                                .Where(x => x != null && !x.isBoss && x.characterId != 1)
                                                .OrderBy(x => x.characterId))
            {
                characterUnlockQueue.Enqueue(c);
            }
            Debug.Log($"[FacilityManager] キャラ解放キュー初期化: {characterUnlockQueue.Count} 件");
        }
        else
        {
            Debug.LogError("[FacilityManager] CharacterDatabase が設定されていない、または characters が空です");
        }
    }

    public int GetLevel(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility))
            return 0;
        return facilityLevels.TryGetValue(facility, out var lvl) ? lvl : 0;
    }

    public int GetCurrentFacilityMaxLevel(FacilityData facility)
    {
        int baseMax = facility.initialMaxLevel;
        int capCount = facilityCapUnlockCount.ContainsKey(facility) ? facilityCapUnlockCount[facility] : 0;
        int increasedMax = baseMax + capCount * facility.levelCapIncreasePerUnlock;
        return Mathf.Min(increasedMax, facility.finalMaxLevel);
    }

    public FacilityState GetState(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility))
            return FacilityState.Locked;
        int level = GetLevel(facility);
        int maxLevel = GetCurrentFacilityMaxLevel(facility);
        if (level >= maxLevel) return FacilityState.Maxed;
        return FacilityState.Available;
    }

    public int GetUpgradeCost(FacilityData facility)
    {
        return facility.GetUpgradeCost(GetLevel(facility));
    }

    public bool Upgrade(FacilityData facility)
    {
        int currentLevel = GetLevel(facility);
        int maxLevel = GetCurrentFacilityMaxLevel(facility);
        if (currentLevel >= maxLevel) return false;

        int cost = facility.GetUpgradeCost(currentLevel);
        if (!GameManager.Instance.SpendGold(cost)) return false;

        facilityLevels[facility] = currentLevel + 1;

        ApplyEffect(facility);

        Debug.Log($"{facility.facilityName} を Lv.{currentLevel + 1} に強化しました！");
        return true;
    }

    public bool Unlock(FacilityData facility)
    {
        int clearedStageId = GameManager.Instance.GetClearedStageId();
        if (unlockedFacilities.Contains(facility))
            return false;
        if (clearedStageId < facility.requiredStageId)
            return false;
        if (!GameManager.Instance.SpendStagePoints(facility.unlockStagePointCost))
            return false;

        unlockedFacilities.Add(facility);
        
        facilityLevels[facility] = 0;
        ApplyEffect(facility);
        Debug.Log($"{facility.facilityName} を解放しました！");

        if (facility.effectType == FacilityEffectType.SummonRateUp && facility.summonCategory != CharacterCategory.None)
        {
            OnSummonCategoryUnlocked?.Invoke(facility.summonCategory);
        }

        return true;
    }

    public bool CanUnlock(FacilityData facility)
    {
        int clearedStageId = GameManager.Instance.GetClearedStageId();
        if (unlockedFacilities.Contains(facility))
            return false;
        if (clearedStageId < facility.requiredStageId)
            return false;
        if (GameManager.Instance.GetStagePoints() < facility.unlockStagePointCost)
            return false;
        return true;
    }

    public bool CanUpgradeLevelCap(FacilityData facility)
    {
        int clearedStageId = GameManager.Instance.GetClearedStageId();
        if (!unlockedFacilities.Contains(facility))
            return false;

        int currentCapCount = facilityCapUnlockCount.ContainsKey(facility) ? facilityCapUnlockCount[facility] : 0;
        if (currentCapCount >= facility.facilityLevelCapUnlocks.Count)
            return false;

        var nextUnlock = facility.facilityLevelCapUnlocks[currentCapCount];
        if (clearedStageId >= nextUnlock.stageId && GameManager.Instance.GetStagePoints() >= nextUnlock.requiredStagePoints)
            return true;

        return false;
    }

    public bool UpgradeLevelCap(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility))
            return false;

        int clearedStageId = GameManager.Instance.GetClearedStageId();
        int currentCapCount = facilityCapUnlockCount.ContainsKey(facility) ? facilityCapUnlockCount[facility] : 0;
        if (currentCapCount >= facility.facilityLevelCapUnlocks.Count)
            return false;

        var nextUnlock = facility.facilityLevelCapUnlocks[currentCapCount];
        if (clearedStageId >= nextUnlock.stageId && GameManager.Instance.SpendStagePoints(nextUnlock.requiredStagePoints))
        {
            facilityCapUnlockCount[facility] = currentCapCount + 1;
            Debug.Log($"{facility.facilityName} のレベル上限を {facility.levelCapIncreasePerUnlock} 増加させました！");
            return true;
        }

        return false;
    }

    public List<FacilityData> GetFacilities()
    {
        return facilities;
    }

    public FacilityLevelCapRequirement GetNextFacilityLevelCapRequirement(FacilityData facility)
    {
        int currentCapCount = facilityCapUnlockCount.ContainsKey(facility) ? facilityCapUnlockCount[facility] : 0;
        if (currentCapCount >= facility.facilityLevelCapUnlocks.Count)
            return null; // No more upgrades
        return facility.facilityLevelCapUnlocks[currentCapCount];
    }

    public int GetLevelCapUnlockCost(FacilityData facility)
    {
        int currentCapCount = facilityCapUnlockCount.ContainsKey(facility) ? facilityCapUnlockCount[facility] : 0;
        if (currentCapCount >= facility.facilityLevelCapUnlocks.Count)
            return -1; // No more upgrades
        return facility.facilityLevelCapUnlocks[currentCapCount].requiredStagePoints;
    }

    // Helper methods for FacilityUI and others:

    public bool IsUnlocked(FacilityData facility) => unlockedFacilities.Contains(facility);

    public bool IsMaxLevel(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility)) return false;
        int maxLevel = GetCurrentFacilityMaxLevel(facility);
        return GetLevel(facility) >= maxLevel;
    }

    public int GetUnlockCost(FacilityData facility) => facility.unlockStagePointCost;

    // public int GetLevelCapUnlockCost(FacilityData facility) => facility.levelCapStagePointCost; // Removed as per instructions

    private void ApplyEffect(FacilityData facility)
    {
        int level = GetLevel(facility);
        switch (facility.effectType)
        {
            case FacilityEffectType.GoldProduction:
                GameManager.Instance.ApplyGoldProductionBoost(facility.GetEffectValue(level));
                break;
            case FacilityEffectType.SummonCostDown:
                GameManager.Instance.ApplySummonCostReduction(facility.GetEffectValue(level));
                break;
            case FacilityEffectType.UpgradeCostDown:
                GameManager.Instance.ApplyUpgradeCostReduction(facility.GetEffectValue(level));
                break;
            case FacilityEffectType.StagePointBoost:
                GameManager.Instance.ApplyStagePointBoost(facility.GetEffectValue(level));
                break;
            case FacilityEffectType.SummonRateUp:
                if (facility.summonCategory != CharacterCategory.None)
                {
                    float bonus = facility.summonRatePerLevel; // レベルアップごとに差分だけ加算
                    GameManager.Instance.AddSummonRateMultiplier(facility.summonCategory, bonus);
                    Debug.Log($"[FacilityManager] {facility.summonCategory} の召喚率を +{bonus * 100f}% (Lv.{level})");
                }
                break;
            case FacilityEffectType.FormationSlot:
                GameManager.Instance.ApplyFormationSlotIncrease(1);
                break;
            case FacilityEffectType.LevelCap:
                PlayerInventory.Instance.AddLevelCapBonus(facility.levelCapIncreasePerUnlock);
                break;
            case FacilityEffectType.CharacterUnlock:
                if (characterUnlockQueue.Count > 0)
                {
                    var nextChar = characterUnlockQueue.Dequeue();
                    PlayerInventory.Instance.UnlockCharacterForSummon(nextChar);
                    Debug.Log($"[FacilityManager] {nextChar.characterName} を解放しました。");
                }
                else
                {
                    Debug.LogWarning("[FacilityManager] キャラクター解放キューが空です。");
                }
                break;
            case FacilityEffectType.ChapterUnlock:
                if (level > 0)
                {
                    int chapterToUnlock = level + 1;
                    GameManager.Instance.UnlockChapter(chapterToUnlock);
                }
                break;
            case FacilityEffectType.BossUnlock:
                GameManager.Instance.UnlockBoss("龍");
                Debug.Log("[FacilityManager] ボスキャラ 龍 を解放しました（召喚不可）");
                break;
            default:
                Debug.LogWarning($"Unhandled FacilityEffectType: {facility.effectType}");
                break;
        }
    }


    public bool IsCategoryUnlocked(CharacterCategory category)
    {
        // FacilityData に「対象カテゴリ」を持たせておき、
        // そのカテゴリを解放する FacilityEffectType.SummonRateUp の施設が解放されているかチェック
        foreach (var facility in facilities)
        {
            if (facility.effectType == FacilityEffectType.SummonRateUp &&
                facility.summonCategory == category &&
                IsUnlocked(facility))
            {
                return true;
            }
        }
        return false;
    }
}
