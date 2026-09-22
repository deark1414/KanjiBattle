using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private const string SavePrefix = "KanjiBattle.";
    private const string StagePointsKey = SavePrefix + "StagePoints";
    private const string HighestClearedStageKey = SavePrefix + "HighestClearedStageId";
    private const string UnlockedChapterKey = SavePrefix + "UnlockedChapter";
    private const string BattleSpeedIndexKey = SavePrefix + "BattleSpeedIndex";

    private int stagePoints;
    private int highestClearedStageId;
    [SerializeField] private int unlockedChapter = 1;
    private int selectedBattleSpeedIndex;
    private StageData selectedStage;

    private float stagePointMultiplier = 1f;
    private int facilityFormationSlots = 1;
    private int battleSpeedTier;

    public int StagePoints => stagePoints;
    public int UnlockedChapter => unlockedChapter;
    public int BattleSpeedTier => battleSpeedTier;
    public int SelectedBattleSpeedIndex => selectedBattleSpeedIndex;
    public event Action<int> OnStagePointsChanged;
    public event Action OnProgressionChanged;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        PlayerProgressStore.EnsureCurrentFormat();
        LoadProgress();
    }

    public void AddStagePoints(int amount)
    {
        if (amount <= 0) return;
        stagePoints += amount;
        OnStagePointsChanged?.Invoke(stagePoints);
        SaveProgress();
    }

    public bool SpendStagePoints(int amount)
    {
        if (amount < 0 || stagePoints < amount)
        {
            return false;
        }

        stagePoints -= amount;
        OnStagePointsChanged?.Invoke(stagePoints);
        SaveProgress();
        return true;
    }

    public int GetStagePoints() => stagePoints;

    public void SetSelectedStage(StageData stage) => selectedStage = stage;
    public StageData GetSelectedStage() => selectedStage;

    public void StartStage(StageData stage, System.Collections.Generic.List<CharacterData> allies)
    {
        if (stage == null || allies == null || !allies.Exists(character => character != null))
        {
            Debug.LogWarning("[GameManager] 編成が空のため、バトルを開始しませんでした。");
            UIManager.Instance?.ShowFormation();
            return;
        }

        UIManager.Instance.ShowBattle();
        BattleManager battleManager = FindAnyObjectByType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.StartBattle(allies, stage);
        }
        else
        {
            Debug.LogError("BattleManager not found in the scene.");
        }
    }

    public void RegisterClearedStage(int stageId)
    {
        if (stageId > highestClearedStageId)
        {
            highestClearedStageId = stageId;
        }

        if (stageId > 0 && stageId % 5 == 0)
        {
            UnlockChapter(Mathf.Min(8, (stageId / 5) + 1));
        }

        SaveProgress();
    }

    public bool IsStageCleared(int stageId) => stageId <= highestClearedStageId;
    public bool IsStageUnlocked(int stageId) => stageId <= 1 || IsStageCleared(stageId - 1);
    public int GetClearedStageId() => highestClearedStageId;
    public int GetHighestClearedStageId() => highestClearedStageId;

    public void ClearStage(StageData stage)
    {
        if (stage != null)
        {
            RegisterClearedStage(stage.stageId);
        }
    }

    public void UnlockChapter(int chapterId)
    {
        if (chapterId <= unlockedChapter)
        {
            return;
        }

        unlockedChapter = chapterId;
        OnProgressionChanged?.Invoke();
        SaveProgress();
    }

    public bool IsChapterUnlocked(int chapterId) => chapterId <= unlockedChapter;

    public void ApplyStagePointBoost(float effectValue) => stagePointMultiplier = 1f + Mathf.Max(0f, effectValue);
    public int GetEffectiveStagePointReward(int baseReward) => Mathf.Max(1, Mathf.FloorToInt(baseReward * stagePointMultiplier));

    public void ApplyFormationSlotIncrease(int slots) => facilityFormationSlots = Mathf.Max(facilityFormationSlots, 1 + Mathf.Max(0, slots));
    public int GetFacilityFormationSlots() => facilityFormationSlots;

    public void ApplyBattleSpeedTier(int tier) => battleSpeedTier = Mathf.Max(battleSpeedTier, Mathf.Clamp(tier, 0, 2));

    public void SetBattleSpeedIndex(int speedIndex)
    {
        selectedBattleSpeedIndex = Mathf.Clamp(speedIndex, 0, battleSpeedTier);
        SaveProgress();
    }

    public void ResetRuntimeFacilityEffects()
    {
        stagePointMultiplier = 1f;
        facilityFormationSlots = 1;
        battleSpeedTier = 0;
        selectedBattleSpeedIndex = Mathf.Clamp(selectedBattleSpeedIndex, 0, battleSpeedTier);
        OnProgressionChanged?.Invoke();
    }

    public void SaveProgress()
    {
        PlayerProgressStore.SetInt(StagePointsKey, stagePoints);
        PlayerProgressStore.SetInt(HighestClearedStageKey, highestClearedStageId);
        PlayerProgressStore.SetInt(UnlockedChapterKey, unlockedChapter);
        PlayerProgressStore.SetInt(BattleSpeedIndexKey, selectedBattleSpeedIndex);
        PlayerProgressStore.Save();
    }

    public void LoadProgress()
    {
        stagePoints = PlayerProgressStore.GetInt(StagePointsKey, 0);
        highestClearedStageId = PlayerProgressStore.GetInt(HighestClearedStageKey, 0);
        unlockedChapter = Mathf.Max(1, PlayerProgressStore.GetInt(UnlockedChapterKey, 1));
        selectedBattleSpeedIndex = Mathf.Max(0, PlayerProgressStore.GetInt(BattleSpeedIndexKey, 0));

        SaveProgress();
    }

    public void ResetProgress()
    {
        PlayerProgressStore.Delete(StagePointsKey);
        PlayerProgressStore.Delete(HighestClearedStageKey);
        PlayerProgressStore.Delete(UnlockedChapterKey);
        PlayerProgressStore.Delete(BattleSpeedIndexKey);
        stagePoints = 0;
        highestClearedStageId = 0;
        unlockedChapter = 1;
        selectedBattleSpeedIndex = 0;
        ResetRuntimeFacilityEffects();
        OnStagePointsChanged?.Invoke(stagePoints);
        SaveProgress();
    }
}
