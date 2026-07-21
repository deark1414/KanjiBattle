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

    [SerializeField] private FacilityDatabase facilityDatabase;
    [SerializeField] private CharacterDatabase characterDatabase; // ★Inspectorで設定

    private Dictionary<FacilityData, int> facilityLevels = new Dictionary<FacilityData, int>();
    private HashSet<FacilityData> unlockedFacilities = new HashSet<FacilityData>();
    private Dictionary<FacilityData, int> facilityCapUnlockCount = new Dictionary<FacilityData, int>();

    private struct CharacterUnlockStep
    {
        public readonly int characterId;
        public readonly int requiredStageId;

        public CharacterUnlockStep(int characterId, int requiredStageId)
        {
            this.characterId = characterId;
            this.requiredStageId = requiredStageId;
        }
    }

    private readonly List<CharacterUnlockStep> characterUnlockPlan = new List<CharacterUnlockStep>();

    private void Awake()
    {
        if (Instance == null) Instance = this;

        var facilities = facilityDatabase != null ? facilityDatabase.facilities : null;
        unlockedFacilities = new HashSet<FacilityData>();
        if (facilities == null)
        {
            Debug.LogError("[FacilityManager] FacilityDatabase が設定されていない、または facilities が空です");
            facilities = new List<FacilityData>();
        }

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

        InitializeCharacterUnlockPlan();
    }

    private void InitializeCharacterUnlockPlan()
    {
        characterUnlockPlan.Clear();

        int[,] steps =
        {
            { 2, 2 }, { 3, 3 },
            { 10, 6 }, { 11, 7 }, { 12, 8 },
            { 13, 11 }, { 14, 12 }, { 15, 13 },
            { 4, 16 }, { 5, 17 }, { 6, 18 },
            { 16, 21 }, { 17, 22 }, { 18, 23 },
            { 19, 26 }, { 20, 27 }, { 21, 28 }, { 22, 29 },
            { 23, 31 }, { 24, 32 }, { 25, 33 },
            { 7, 36 }, { 8, 37 }, { 9, 38 }
        };

        for (int i = 0; i < steps.GetLength(0); i++)
        {
            characterUnlockPlan.Add(new CharacterUnlockStep(steps[i, 0], steps[i, 1]));
        }

        Debug.Log($"[FacilityManager] 研究所キャラ解放計画初期化: {characterUnlockPlan.Count} 件");
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
        if (facility.effectType == FacilityEffectType.CharacterUnlock && !HasAvailableResearchCharacter()) return FacilityState.Maxed;
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
        if (facility.effectType == FacilityEffectType.CharacterUnlock && !HasAvailableResearchCharacter()) return false;

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
        return facilityDatabase != null ? facilityDatabase.facilities : new List<FacilityData>();
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
                if (level > 0)
                {
                    GameManager.Instance.ApplyFormationSlotIncrease(1);
                }
                break;
            case FacilityEffectType.LevelCap:
                PlayerInventory.Instance.AddLevelCapBonus(facility.levelCapIncreasePerUnlock);
                break;
            case FacilityEffectType.CharacterUnlock:
                UnlockNextResearchCharacter();
                break;
            case FacilityEffectType.ChapterUnlock:
                if (level > 0)
                {
                    int chapterToUnlock = level + 1;
                    GameManager.Instance.UnlockChapter(chapterToUnlock);
                }
                break;
            case FacilityEffectType.BossUnlock:
                GameManager.Instance.UnlockBoss("竜");
                Debug.Log("[FacilityManager] ボスキャラ 竜 を解放しました（召喚不可）");
                break;
            default:
                Debug.LogWarning($"Unhandled FacilityEffectType: {facility.effectType}");
                break;
        }
    }

    private bool HasAvailableResearchCharacter()
    {
        return TryGetNextResearchCharacter(out _, out _);
    }

    private bool TryGetNextResearchCharacter(out CharacterData character, out int requiredStageId)
    {
        character = null;
        requiredStageId = 0;

        if (PlayerInventory.Instance == null || characterDatabase == null)
        {
            return false;
        }

        int clearedStageId = GameManager.Instance != null ? GameManager.Instance.GetClearedStageId() : 0;

        foreach (CharacterUnlockStep step in characterUnlockPlan)
        {
            CharacterData candidate = characterDatabase.GetById(step.characterId);
            if (candidate == null || PlayerInventory.Instance.IsSummonable(candidate))
            {
                continue;
            }

            character = candidate;
            requiredStageId = step.requiredStageId;
            return clearedStageId >= step.requiredStageId;
        }

        return false;
    }

    private void UnlockNextResearchCharacter()
    {
        if (PlayerInventory.Instance == null || characterDatabase == null)
        {
            Debug.LogWarning("[FacilityManager] 研究所解放に必要な参照がありません。");
            return;
        }

        if (!TryGetNextResearchCharacter(out CharacterData character, out int requiredStageId))
        {
            if (character != null)
            {
                Debug.Log($"[FacilityManager] 次の研究解放 {character.characterName} は Stage {requiredStageId} クリア後です。");
            }
            else
            {
                Debug.Log("[FacilityManager] 研究所で解放できるキャラクターはありません。");
            }
            return;
        }

        if (PlayerInventory.Instance.UnlockCharacterForSummon(character))
        {
            Debug.Log($"[FacilityManager] 研究所で {character.characterName} を解放しました。");
        }
    }

    public bool IsCategoryUnlocked(CharacterCategory category)
    {


        // FacilityData に「対象カテゴリ」を持たせておき、
        // そのカテゴリを解放する FacilityEffectType.SummonRateUp の施設が解放されているかチェック
        var facilities = facilityDatabase != null ? facilityDatabase.facilities : null;
        if (facilities == null)
        {
            return false;
        }

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
