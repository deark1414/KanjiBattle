using System.Collections.Generic;
using UnityEngine;

public enum FacilityState
{
    Locked,     // 未解放
    Available,  // 解放済み、強化可能
    Maxed       // 最大レベル到達
}

public class FacilityManager : MonoBehaviour
{
    public static FacilityManager Instance;

    [SerializeField] private List<FacilityData> facilities = new List<FacilityData>();
    private Dictionary<FacilityData, int> facilityLevels = new Dictionary<FacilityData, int>();
    private HashSet<FacilityData> unlockedFacilities = new HashSet<FacilityData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;

        foreach (var f in facilities)
        {
            if (!facilityLevels.ContainsKey(f))
                facilityLevels[f] = 0; // Lv0
        }
        unlockedFacilities = new HashSet<FacilityData>();
    }

    public int GetLevel(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility))
            return 0;
        return facilityLevels.TryGetValue(facility, out var lvl) ? lvl : 0;
    }

    public FacilityState GetState(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility))
            return FacilityState.Locked;
        int level = GetLevel(facility);
        if (level >= facility.maxLevel) return FacilityState.Maxed;
        return FacilityState.Available;
    }

    public int GetUpgradeCost(FacilityData facility)
    {
        return facility.GetUpgradeCost(GetLevel(facility));
    }

    public bool Upgrade(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility))
            return false;
        int currentLevel = GetLevel(facility);
        if (currentLevel >= facility.maxLevel) return false;

        int cost = facility.GetUpgradeCost(currentLevel);
        if (GameManager.Instance.GetGold() < cost) return false;

        GameManager.Instance.SpendGold(cost);
        facilityLevels[facility] = currentLevel + 1;
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
        Debug.Log($"{facility.facilityName} を解放しました！");
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
        if (clearedStageId < facility.levelCapUnlockStageId)
            return false;
        if (!GameManager.Instance.SpendStagePoints(facility.levelCapStagePointCost))
            return false;
        return true;
    }

    public bool UpgradeLevelCap(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility))
            return false;
        if (!GameManager.Instance.SpendStagePoints(facility.levelCapStagePointCost))
            return false;

        facility.maxLevel += facility.levelCapIncrease;
        Debug.Log($"{facility.facilityName} のレベル上限を {facility.levelCapIncrease} 増加させました！");
        return true;
    }

    public List<FacilityData> GetFacilities()
    {
        return facilities;
    }


    // Helper methods for FacilityUI and others:

    public bool IsUnlocked(FacilityData facility) => unlockedFacilities.Contains(facility);

    public bool IsMaxLevel(FacilityData facility)
    {
        if (!unlockedFacilities.Contains(facility)) return false;
        return GetLevel(facility) >= facility.maxLevel;
    }

    public int GetUnlockCost(FacilityData facility) => facility.unlockStagePointCost;

    public int GetLevelCapUnlockCost(FacilityData facility) => facility.levelCapStagePointCost;
}