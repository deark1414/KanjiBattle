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
    private const string CapUnlocksKey = SavePrefix + "CapUnlocks";
    private const string MigrationVersionKey = SavePrefix + "ProgressionMigrationVersion";
    private const int CurrentMigrationVersion = 3;

    [SerializeField] private FacilityDatabase facilityDatabase;
    private readonly Dictionary<FacilityData, int> facilityLevels = new();
    private readonly Dictionary<FacilityData, int> facilityCapUnlockCounts = new();
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
            facilityCapUnlockCounts[facility] = 0;
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
        int unlockCount = facilityCapUnlockCounts.TryGetValue(facility, out int count) ? count : 0;
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

    public float GetEarlyRecruitmentChance()
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

    public float GetExperienceMultiplier()
    {
        FacilityData trainingGround = FindFacility(FacilityEffectType.Training);
        int level = trainingGround != null && IsUnlocked(trainingGround) ? GetLevel(trainingGround) : 0;
        return level switch
        {
            1 => 1.15f,
            2 => 1.30f,
            3 => 1.50f,
            4 => 1.75f,
            >= 5 => 2f,
            _ => 1f
        };
    }

    public int GetBattleSpeedTier()
    {
        FacilityData archive = FindFacility(FacilityEffectType.BattleSpeed);
        return archive != null && IsUnlocked(archive) ? Mathf.Clamp(GetLevel(archive), 0, 2) : 0;
    }

    public bool IsStageRetryUnlocked()
    {
        FacilityData retryHall = FindFacility(FacilityEffectType.StageRetry);
        return retryHall != null && IsUnlocked(retryHall);
    }

    public bool CanUpgradeLevelCap(FacilityData facility)
    {
        FacilityLevelCapRequirement requirement = GetNextFacilityLevelCapRequirement(facility);
        return facility != null
            && IsUnlocked(facility)
            && IsMaxLevel(facility)
            && requirement != null
            && GameManager.Instance != null
            && GameManager.Instance.GetClearedStageId() >= requirement.stageId;
    }

    public bool UpgradeLevelCap(FacilityData facility)
    {
        FacilityLevelCapRequirement requirement = GetNextFacilityLevelCapRequirement(facility);
        if (!CanUpgradeLevelCap(facility) || requirement == null)
        {
            return false;
        }

        facilityCapUnlockCounts[facility] = facilityCapUnlockCounts.TryGetValue(facility, out int count) ? count + 1 : 1;
        SaveProgress();
        OnFacilitiesChanged?.Invoke();
        return true;
    }

    public FacilityLevelCapRequirement GetNextFacilityLevelCapRequirement(FacilityData facility)
    {
        if (facility == null || !IsUnlocked(facility) || GetCurrentFacilityMaxLevel(facility) >= facility.finalMaxLevel)
        {
            return null;
        }

        int unlockCount = facilityCapUnlockCounts.TryGetValue(facility, out int count) ? count : 0;
        return facility.facilityLevelCapUnlocks != null && unlockCount < facility.facilityLevelCapUnlocks.Count
            ? facility.facilityLevelCapUnlocks[unlockCount]
            : null;
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
        PlayerProgressStore.SetString(CapUnlocksKey, string.Join(",", facilityCapUnlockCounts.Where(entry => entry.Key != null).Select(entry => $"{entry.Key.facilityId}:{entry.Value}")));
        PlayerProgressStore.Save();
    }

    public void LoadProgress()
    {
        isLoadingProgress = true;
        bool isLegacySave = PlayerProgressStore.GetInt(MigrationVersionKey, 0) < CurrentMigrationVersion;
        if (!isLegacySave)
        {
            DeserializeUnlocked(PlayerProgressStore.GetString(UnlockedKey));
            DeserializeCapUnlocks(PlayerProgressStore.GetString(CapUnlocksKey));
            DeserializeLevels(PlayerProgressStore.GetString(LevelsKey));
        }
        isLoadingProgress = false;
        SaveProgress();
    }

    public void ResetProgress()
    {
        PlayerProgressStore.Delete(UnlockedKey);
        PlayerProgressStore.Delete(LevelsKey);
        PlayerProgressStore.Delete(CapUnlocksKey);
        PlayerProgressStore.Delete(MigrationVersionKey);
        facilityLevels.Clear();
        facilityCapUnlockCounts.Clear();
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

    private void DeserializeCapUnlocks(string serialized)
    {
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return;
        }

        foreach (string entry in serialized.Split(','))
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], out int facilityId)
                || !int.TryParse(parts[1], out int unlockCount))
            {
                continue;
            }

            FacilityData facility = GetFacilities().FirstOrDefault(candidate => candidate != null && candidate.facilityId == facilityId);
            if (facility != null)
            {
                facilityCapUnlockCounts[facility] = Mathf.Clamp(unlockCount, 0, facility.facilityLevelCapUnlocks?.Count ?? 0);
            }
        }
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
