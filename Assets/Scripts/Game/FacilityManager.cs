using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum FacilityState
{
    Locked,
    Available,
    Maxed
}

public class FacilityManager : MonoBehaviour
{
    public static FacilityManager Instance;

    private const string SavePrefix = "KanjiBattle.Facilities.";
    private const string UnlockedKey = SavePrefix + "Unlocked";
    private const string LevelsKey = SavePrefix + "Levels";
    private const string MigrationVersionKey = SavePrefix + "ProgressionMigrationVersion";
    private const int CurrentMigrationVersion = 3;

    [SerializeField] private FacilityDatabase facilityDatabase;
    private readonly Dictionary<FacilityData, int> facilityLevels = new();
    private readonly HashSet<FacilityData> unlockedFacilities = new();
    private bool isLoadingProgress;

    public event Action OnFacilitiesChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        PlayerProgressStore.EnsureCurrentFormat();
        InitializeFacilities();
        LoadProgress();
        ReapplyAllEffects();
    }

    private void InitializeFacilities()
    {
        foreach (FacilityData facility in GetFacilities())
        {
            if (facility == null) continue;
            facilityLevels[facility] = 0;
            if (facility.unlockType == FacilityUnlockType.Free)
            {
                unlockedFacilities.Add(facility);
            }
        }
    }

    public int GetLevel(FacilityData facility)
    {
        return facility != null && unlockedFacilities.Contains(facility) && facilityLevels.TryGetValue(facility, out int level)
            ? level
            : 0;
    }

    public int GetCurrentFacilityMaxLevel(FacilityData facility)
    {
        if (facility == null)
        {
            return 0;
        }

        int initialCap = Mathf.Clamp(facility.initialMaxLevel, 0, facility.finalMaxLevel);
        int unlockCount = GetAutomaticCapUnlockCount(facility);
        int capIncrease = Mathf.Max(1, facility.levelCapIncreasePerUnlock);
        return Mathf.Min(facility.finalMaxLevel, initialCap + unlockCount * capIncrease);
    }

    public FacilityState GetState(FacilityData facility)
    {
        if (!IsUnlocked(facility)) return FacilityState.Locked;
        return IsMaxLevel(facility) ? FacilityState.Maxed : FacilityState.Available;
    }

    public bool IsUnlocked(FacilityData facility) => facility != null && unlockedFacilities.Contains(facility);

    public bool IsMaxLevel(FacilityData facility)
    {
        return facility != null && IsUnlocked(facility) && GetLevel(facility) >= GetCurrentFacilityMaxLevel(facility);
    }

    public int GetUpgradeCost(FacilityData facility)
    {
        return facility == null ? 0 : facility.GetUpgradeCost(GetLevel(facility));
    }

    public int GetUnlockCost(FacilityData facility) => facility != null ? facility.unlockStagePointCost : 0;

    public bool CanUnlock(FacilityData facility)
    {
        if (facility == null || IsUnlocked(facility) || GameManager.Instance == null)
        {
            return false;
        }

        return GameManager.Instance.GetClearedStageId() >= facility.requiredStageId
            && GameManager.Instance.GetStagePoints() >= facility.unlockStagePointCost;
    }

    public bool Unlock(FacilityData facility)
    {
        if (!CanUnlock(facility) || !GameManager.Instance.SpendStagePoints(facility.unlockStagePointCost))
        {
            return false;
        }

        unlockedFacilities.Add(facility);
        facilityLevels[facility] = 0;
        ReapplyAllEffects();
        SaveProgress();
        OnFacilitiesChanged?.Invoke();
        return true;
    }

    public bool Upgrade(FacilityData facility)
    {
        if (facility == null || !IsUnlocked(facility) || IsMaxLevel(facility) || GameManager.Instance == null)
        {
            return false;
        }

        int cost = GetUpgradeCost(facility);
        if (!GameManager.Instance.SpendStagePoints(cost))
        {
            return false;
        }

        facilityLevels[facility] = GetLevel(facility) + 1;
        ReapplyAllEffects();
        SaveProgress();
        OnFacilitiesChanged?.Invoke();
        return true;
    }

    public List<FacilityData> GetFacilities() => facilityDatabase != null ? facilityDatabase.facilities : new List<FacilityData>();

    public int GetRecruitmentBondGain()
    {
        FacilityData recruitmentHall = FindFacility(FacilityEffectType.Recruitment);
        int level = recruitmentHall != null && IsUnlocked(recruitmentHall) ? GetLevel(recruitmentHall) : 0;
        return level switch
        {
            1 => 2,
            2 => 3,
            >= 3 => 5,
            _ => 1
        };
    }

    // A lucky victory completes one unfinished bond. It never recruits automatically;
    // the player still confirms the ally from the roster.
    public float GetLuckyBondCompletionChance()
    {
        FacilityData recruitmentHall = FindFacility(FacilityEffectType.Recruitment);
        int level = recruitmentHall != null && IsUnlocked(recruitmentHall) ? GetLevel(recruitmentHall) : 0;
        return level switch
        {
            1 => 0.01f,
            2 => 0.02f,
            >= 3 => 0.04f,
            _ => 0.005f
        };
    }

    public const float BaseBattleExperienceRate = 0.04f;
    public const float BaseMockTrainingExperienceRate = 0.0004f;

    public float GetBattleExperienceRate()
    {
        FacilityData trainingGround = FindFacility(FacilityEffectType.Training);
        int level = trainingGround != null && IsUnlocked(trainingGround) ? GetLevel(trainingGround) : 0;
        return level switch
        {
            1 => 0.06f,
            2 => 0.08f,
            3 => 0.10f,
            4 => 0.12f,
            >= 5 => 0.15f,
            _ => BaseBattleExperienceRate
        };
    }

    public float GetMockTrainingExperienceRate()
    {
        FacilityData trainingGround = FindFacility(FacilityEffectType.Training);
        int level = trainingGround != null && IsUnlocked(trainingGround) ? GetLevel(trainingGround) : 0;
        return level switch
        {
            1 => 0.0005f,
            2 => 0.0006f,
            3 => 0.00075f,
            4 => 0.0009f,
            >= 5 => 0.001f,
            _ => BaseMockTrainingExperienceRate
        };
    }

    public int GetMockTrainingCooldownSeconds()
    {
        FacilityData drillTower = FindFacility(FacilityEffectType.TrainingFrequency);
        int level = drillTower != null && IsUnlocked(drillTower) ? GetLevel(drillTower) : 0;
        return level switch
        {
            1 => 36,
            2 => 29,
            3 => 23,
            4 => 18,
            >= 5 => 14,
            _ => 45
        };
    }

    public float GetAttackMultiplier()
    {
        FacilityData martialHall = FindFacility(FacilityEffectType.AttackBoost);
        int level = martialHall != null && IsUnlocked(martialHall) ? GetLevel(martialHall) : 0;
        return 1f + Mathf.Clamp(level, 0, 5) * 0.04f;
    }

    public float GetSkillPowerMultiplier()
    {
        FacilityData strategyHall = FindFacility(FacilityEffectType.SkillPowerBoost);
        int level = strategyHall != null && IsUnlocked(strategyHall) ? GetLevel(strategyHall) : 0;
        return 1f + Mathf.Clamp(level, 0, 5) * 0.05f;
    }

    public float GetSkillChanceMultiplier()
    {
        FacilityData shrine = FindFacility(FacilityEffectType.SkillChanceBoost);
        int level = shrine != null && IsUnlocked(shrine) ? GetLevel(shrine) : 0;
        return 1f + Mathf.Clamp(level, 0, 5) * 0.2f;
    }

    public float GetHealthMultiplier()
    {
        FacilityData infirmary = FindFacility(FacilityEffectType.HealthBoost);
        int level = infirmary != null && IsUnlocked(infirmary) ? GetLevel(infirmary) : 0;
        return 1f + Mathf.Clamp(level, 0, 5) * 0.05f;
    }

    public int GetBattleSpeedTier()
    {
        FacilityData archive = FindFacility(FacilityEffectType.BattleSpeed);
        return archive != null && IsUnlocked(archive) ? Mathf.Clamp(GetLevel(archive), 0, 8) : 0;
    }

    public bool IsStageRetryUnlocked()
    {
        FacilityData retryHall = FindFacility(FacilityEffectType.StageRetry);
        return retryHall != null && IsUnlocked(retryHall);
    }

    public bool CanUpgradeLevelCap(FacilityData facility)
    {
        return false;
    }

    public bool UpgradeLevelCap(FacilityData facility)
    {
        // Kept as a compatibility entry point for debug callers. Level caps are
        // determined exclusively from cleared stages and cannot be purchased.
        return false;
    }

    public void RefreshAutomaticLevelCaps()
    {
        OnFacilitiesChanged?.Invoke();
    }

    public FacilityLevelCapRequirement GetNextFacilityLevelCapRequirement(FacilityData facility)
    {
        if (facility == null || !IsUnlocked(facility) || GetCurrentFacilityMaxLevel(facility) >= facility.finalMaxLevel)
        {
            return null;
        }

        int clearedStageId = GameManager.Instance != null ? GameManager.Instance.GetClearedStageId() : 0;
        return facility.facilityLevelCapUnlocks?
            .Where(requirement => requirement != null && requirement.stageId > clearedStageId)
            .OrderBy(requirement => requirement.stageId)
            .FirstOrDefault();
    }

    public int GetLevelCapUnlockCost(FacilityData facility)
    {
        return GetNextFacilityLevelCapRequirement(facility) != null ? 0 : -1;
    }
    public void SaveProgress()
    {
        if (isLoadingProgress) return;

        PlayerProgressStore.SetInt(MigrationVersionKey, CurrentMigrationVersion);
        PlayerProgressStore.SetString(UnlockedKey, string.Join(",", unlockedFacilities.Where(facility => facility != null).Select(facility => facility.facilityId)));
        PlayerProgressStore.SetString(LevelsKey, string.Join(",", facilityLevels.Where(entry => entry.Key != null).Select(entry => $"{entry.Key.facilityId}:{entry.Value}")));
        PlayerProgressStore.Save();
    }

    public void LoadProgress()
    {
        isLoadingProgress = true;
        bool isLegacySave = PlayerProgressStore.GetInt(MigrationVersionKey, 0) < CurrentMigrationVersion;
        if (!isLegacySave)
        {
            DeserializeUnlocked(PlayerProgressStore.GetString(UnlockedKey));
            DeserializeLevels(PlayerProgressStore.GetString(LevelsKey));
        }
        isLoadingProgress = false;
        SaveProgress();
    }

    public void ResetProgress()
    {
        PlayerProgressStore.Delete(UnlockedKey);
        PlayerProgressStore.Delete(LevelsKey);
        PlayerProgressStore.Delete(MigrationVersionKey);
        facilityLevels.Clear();
        unlockedFacilities.Clear();
        InitializeFacilities();
        ReapplyAllEffects();
        SaveProgress();
        OnFacilitiesChanged?.Invoke();
    }

    private void ReapplyAllEffects()
    {
        GameManager.Instance?.ResetRuntimeFacilityEffects();
        foreach (FacilityData facility in unlockedFacilities)
        {
            int level = GetLevel(facility);
            switch (facility.effectType)
            {
                case FacilityEffectType.StagePointBoost:
                    GameManager.Instance?.ApplyStagePointBoost(facility.GetEffectValue(level));
                    break;
                case FacilityEffectType.FormationSlot:
                    GameManager.Instance?.ApplyFormationSlotIncrease(level);
                    break;
                case FacilityEffectType.BattleSpeed:
                    GameManager.Instance?.ApplyBattleSpeedTier(level);
                    break;
            }
        }
    }

    private FacilityData FindFacility(FacilityEffectType effectType)
    {
        return GetFacilities().FirstOrDefault(facility => facility != null && facility.effectType == effectType);
    }

    private static int GetAutomaticCapUnlockCount(FacilityData facility)
    {
        if (facility == null || facility.facilityLevelCapUnlocks == null)
        {
            return 0;
        }

        int clearedStageId = GameManager.Instance != null ? GameManager.Instance.GetClearedStageId() : 0;
        return facility.facilityLevelCapUnlocks.Count(requirement => requirement != null && requirement.stageId <= clearedStageId);
    }

    private void DeserializeUnlocked(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized)) return;
        foreach (string value in serialized.Split(','))
        {
            if (!int.TryParse(value, out int id)) continue;
            FacilityData facility = GetFacilities().FirstOrDefault(candidate => candidate != null && candidate.facilityId == id);
            if (facility != null) unlockedFacilities.Add(facility);
        }
    }

    private void DeserializeLevels(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized)) return;
        foreach (string value in serialized.Split(','))
        {
            string[] parts = value.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int id) || !int.TryParse(parts[1], out int level)) continue;
            FacilityData facility = GetFacilities().FirstOrDefault(candidate => candidate != null && candidate.facilityId == id);
            if (facility != null)
            {
                facilityLevels[facility] = Mathf.Clamp(level, 0, GetCurrentFacilityMaxLevel(facility));
            }
        }
    }
}
