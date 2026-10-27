using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    private const string SavePrefix = "KanjiBattle.Inventory.";
    private const string OwnedKey = SavePrefix + "Owned";
    private const string PlayerLevelKey = SavePrefix + "PlayerLevel";
    private const string PlayerExperienceKey = SavePrefix + "PlayerExperience";
    private const string ExperienceCurveVersionKey = SavePrefix + "ExperienceCurveVersion";
    private const string StarterTrainingGrantedKey = SavePrefix + "PlayerTrainingGranted";
    private const string LastMockTrainingUtcKey = SavePrefix + "LastMockTrainingUtc";
    public const int BaseBattleExperience = 10;
    public const int BaseMockTrainingExperience = 1;
    private const float MockTrainingPollIntervalSeconds = 1f;
    private const int MaximumOfflineTrainingCycles = 80;

    public event Action onInventoryChanged;
    public event Action<PlayerExperienceResult> onMockTrainingCompleted;

    [SerializeField] private CharacterDatabase characterDatabase;
    private readonly Dictionary<CharacterData, CharacterInfo> ownedCharacters = new();
    private bool isLoadingProgress;
    private int playerLevel = 1;
    private int playerExperience;
    private float nextMockTrainingPollAt;
    private FacilityManager subscribedFacilityManager;

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
        EnsureStarterCharacter();
        GrantStarterTraining();
        SaveProgress();
    }

    private void Start()
    {
        SubscribeToFacilityChanges();
        ApplyOfflineMockTraining();
        nextMockTrainingPollAt = Time.unscaledTime + MockTrainingPollIntervalSeconds;
    }

    private void OnDestroy()
    {
        if (subscribedFacilityManager != null)
        {
            subscribedFacilityManager.OnFacilitiesChanged -= ApplyStoredExperienceAfterLevelCapChange;
        }
    }

    private void Update()
    {
        SubscribeToFacilityChanges();

        // Mock training belongs to the player's progression, not to the roster
        // screen. Poll from this persistent owner so it continues while marching,
        // forming a party, or browsing facilities. The persisted cooldown keeps
        // the visual drill and this background check from ever awarding twice.
        if (Time.unscaledTime < nextMockTrainingPollAt)
        {
            return;
        }

        nextMockTrainingPollAt = Time.unscaledTime + MockTrainingPollIntervalSeconds;
        GrantMockTrainingExperience();
        // The shared header owns the countdown display, so refresh it on the
        // same one-second cadence that advances mock training.
        ModernWafuuPresentation.RefreshGlobalStatus();
    }

    public Dictionary<CharacterData, CharacterInfo> GetOwnedCharacters() => ownedCharacters;
    public int PlayerLevel => playerLevel;
    public int PlayerExperience => playerExperience;
    public bool IsAtEffectiveLevelCap => playerLevel >= GetEffectiveLevelCap();

    public List<CharacterData> GetUnlockedCharacters() => ownedCharacters.Keys.ToList();

    public bool IsOwned(CharacterData data) => data != null && ownedCharacters.ContainsKey(data);

    public bool AddCharacter(CharacterData data)
    {
        if (data == null || data.isBoss || ownedCharacters.ContainsKey(data))
        {
            return false;
        }

        ownedCharacters[data] = new CharacterInfo();
        onInventoryChanged?.Invoke();
        SaveProgress();
        return true;
    }

    public int GetExperienceToNextPlayerLevel()
    {
        return GetExperienceRequiredForLevel(playerLevel);
    }

    public int GetMockTrainingIntervalSeconds()
    {
        return FacilityManager.Instance != null
            ? FacilityManager.Instance.GetMockTrainingCooldownSeconds()
            : 45;
    }

    public int GetMockTrainingExperienceReward()
    {
        return GetEffectiveMockTrainingExperienceReward();
    }

    public int GetSecondsUntilNextMockTraining()
    {
        long previous = PlayerProgressStore.GetInt(LastMockTrainingUtcKey, 0);
        if (previous <= 0)
        {
            return GetMockTrainingIntervalSeconds();
        }

        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long elapsed = Math.Max(0, now - previous);
        return Mathf.Max(0, GetMockTrainingIntervalSeconds() - (int)elapsed);
    }

    public int GetPlayerMaxHP(CharacterData character, int level)
    {
        if (character == null)
        {
            return 0;
        }

        return CharacterInvestment.Health(character,level);
    }

    public PlayerExperienceResult GrantBattleExperience(StageData stage, IEnumerable<CharacterData> deployedParty = null)
    {
        return GrantPlayerExperience(GetEffectiveBattleExperienceReward(stage, deployedParty));
    }

    public PlayerExperienceResult GrantMockTrainingExperience()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long previous = PlayerProgressStore.GetInt(LastMockTrainingUtcKey, 0);
        // A completed drill may award a tiny amount, but a screen toggle must not
        // become a faster progression route than actually clearing a stage.
        int cooldownSeconds = GetMockTrainingIntervalSeconds();
        if (previous <= 0)
        {
            PlayerProgressStore.SetInt(LastMockTrainingUtcKey, (int)Math.Min(now, int.MaxValue));
            PlayerProgressStore.Save();
            return new PlayerExperienceResult(0, playerLevel, playerLevel, playerExperience);
        }

        if (now - previous < cooldownSeconds)
        {
            return new PlayerExperienceResult(0, playerLevel, playerLevel, playerExperience);
        }

        PlayerExperienceResult result = GrantPlayerExperience(GetEffectiveMockTrainingExperienceReward());
        // Training continues at the level cap, storing experience until the
        // rank tower makes the next level band available.
        PlayerProgressStore.SetInt(LastMockTrainingUtcKey, (int)Math.Min(now, int.MaxValue));
        PlayerProgressStore.Save();
        onMockTrainingCompleted?.Invoke(result);
        return result;
    }

    public PlayerExperienceResult GrantPlayerExperience(int amount)
    {
        return GrantPlayerExperienceInternal(amount, true);
    }

    private PlayerExperienceResult GrantPlayerExperienceInternal(int amount, bool saveProgress)
    {
        int previousLevel = playerLevel;
        if (amount <= 0)
        {
            return new PlayerExperienceResult(0, previousLevel, playerLevel, playerExperience);
        }

        playerExperience = (int)Math.Min(int.MaxValue, (long)playerExperience + amount);
        ApplyStoredExperienceToCurrentCap();

        // Incremental experience should not rebuild the roster while its practice
        // animation is active. A level-up still refreshes every shared stat display.
        if (playerLevel != previousLevel)
        {
            onInventoryChanged?.Invoke();
        }
        if (saveProgress)
        {
            SaveProgress();
        }
        ModernWafuuPresentation.RefreshGlobalStatus();
        return new PlayerExperienceResult(amount, previousLevel, playerLevel, playerExperience);
    }

    private void ApplyStoredExperienceToCurrentCap()
    {
        int levelCap = GetEffectiveLevelCap();
        while (playerLevel < levelCap && playerExperience >= GetExperienceRequiredForLevel(playerLevel))
        {
            playerExperience -= GetExperienceRequiredForLevel(playerLevel);
            playerLevel++;
        }
    }

    private void ApplyStoredExperienceAfterLevelCapChange()
    {
        int previousLevel = playerLevel;
        ApplyStoredExperienceToCurrentCap();

        if (playerLevel != previousLevel)
        {
            onInventoryChanged?.Invoke();
            SaveProgress();
        }

        ModernWafuuPresentation.RefreshGlobalStatus();
    }

    private void SubscribeToFacilityChanges()
    {
        FacilityManager currentManager = FacilityManager.Instance;
        if (ReferenceEquals(subscribedFacilityManager, currentManager))
        {
            return;
        }

        if (subscribedFacilityManager != null)
        {
            subscribedFacilityManager.OnFacilitiesChanged -= ApplyStoredExperienceAfterLevelCapChange;
        }

        subscribedFacilityManager = currentManager;
        if (subscribedFacilityManager == null)
        {
            return;
        }

        subscribedFacilityManager.OnFacilitiesChanged += ApplyStoredExperienceAfterLevelCapChange;
        ApplyStoredExperienceAfterLevelCapChange();
    }

    private void ApplyOfflineMockTraining()
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long previous = PlayerProgressStore.GetInt(LastMockTrainingUtcKey, 0);
        if (previous <= 0)
        {
            PlayerProgressStore.SetInt(LastMockTrainingUtcKey, (int)Math.Min(now, int.MaxValue));
            PlayerProgressStore.Save();
            return;
        }

        int cooldown = GetMockTrainingIntervalSeconds();
        int completedCycles = Mathf.Min(MaximumOfflineTrainingCycles, Mathf.Max(0, (int)((now - previous) / cooldown)));
        if (completedCycles <= 0)
        {
            return;
        }

        for (int i = 0; i < completedCycles; i++)
        {
            GrantPlayerExperienceInternal(GetEffectiveMockTrainingExperienceReward(), false);
        }

        // Consuming the elapsed period here prevents reopening the game from
        // repeatedly claiming the same offline interval.
        PlayerProgressStore.SetInt(LastMockTrainingUtcKey, (int)Math.Min(now, int.MaxValue));
        SaveProgress();
        ModernWafuuPresentation.RefreshGlobalStatus();
    }

    public int GetEffectiveLevelCap()
    {
        if (FacilityManager.Instance != null)
        {
            return FacilityManager.Instance.GetPlayerLevelCap();
        }

        // During initial scene startup the facility manager may not exist yet.
        // Preserve the stage-derived cap until the rank tower is ready.
        int clearedStageId = GameManager.Instance != null ? GameManager.Instance.GetHighestClearedStageId() : 0;
        int unlockedBands = Mathf.Max(0, clearedStageId / 5);
        return Mathf.Clamp(5 + unlockedBands * 6, 5, 50);
    }

    public void SetPlayerLevelForDebug(int level)
    {
        playerLevel = Mathf.Clamp(level, 1, GetEffectiveLevelCap());
        playerExperience = 0;
        onInventoryChanged?.Invoke();
        SaveProgress();
    }

    public int GetEffectiveBattleExperienceReward(StageData stage, IEnumerable<CharacterData> deployedParty = null)
    {
        int baseReward = GetStageBattleExperienceReward(stage);
        float multiplier = FacilityManager.Instance != null
            ? FacilityManager.Instance.GetBattleExperienceMultiplier()
            : 1f;
        return BattleRewardRules.CalculateExperience(baseReward, multiplier, deployedParty);
    }

    private int GetEffectiveMockTrainingExperienceReward()
    {
        float rate = FacilityManager.Instance != null
            ? FacilityManager.Instance.GetMockTrainingExperienceRate()
            : FacilityManager.BaseMockTrainingExperienceRate;
        return GetExperienceRewardFromCurrentRequirement(rate);
    }

    public void SaveProgress()
    {
        if (isLoadingProgress)
        {
            return;
        }

        PlayerProgressStore.SetString(OwnedKey, SerializeOwnedCharacters());
        PlayerProgressStore.SetInt(PlayerLevelKey, playerLevel);
        PlayerProgressStore.SetInt(PlayerExperienceKey, playerExperience);
        PlayerProgressStore.SetInt(ExperienceCurveVersionKey, PlayerExperienceCurve.Version);
        PlayerProgressStore.Save();
    }

    public void LoadProgress()
    {
        isLoadingProgress = true;
        DeserializeOwnedCharacters(PlayerProgressStore.GetString(OwnedKey));
        playerLevel = Mathf.Clamp(PlayerProgressStore.GetInt(PlayerLevelKey, 1), 1, GetEffectiveLevelCap());
        playerExperience = Mathf.Max(0, PlayerProgressStore.GetInt(PlayerExperienceKey, 0));
        bool migrateExperience = PlayerProgressStore.GetInt(ExperienceCurveVersionKey, 0) < PlayerExperienceCurve.Version;
        if (migrateExperience)
        {
            playerExperience = PlayerExperienceCurve.ConvertLegacyExperience(playerLevel, playerExperience);
        }
        isLoadingProgress = false;
        if (migrateExperience) SaveProgress();
        onInventoryChanged?.Invoke();
    }

    public void ResetProgress()
    {
        PlayerProgressStore.Delete(OwnedKey);
        PlayerProgressStore.Delete(PlayerLevelKey);
        PlayerProgressStore.Delete(PlayerExperienceKey);
        PlayerProgressStore.Delete(StarterTrainingGrantedKey);
        PlayerProgressStore.Delete(LastMockTrainingUtcKey);
        ownedCharacters.Clear();
        playerLevel = 1;
        playerExperience = 0;
        EnsureStarterCharacter();
        GrantStarterTraining();
        onInventoryChanged?.Invoke();
        SaveProgress();
    }

    private void EnsureStarterCharacter()
    {
        foreach(int id in new[]{1,10,20})
        {
            CharacterData starter = GetCharacterDatabase()?.GetById(id);
            if (starter != null && !ownedCharacters.ContainsKey(starter)) ownedCharacters[starter] = new CharacterInfo();
        }
    }

    private void GrantStarterTraining()
    {
        if (PlayerProgressStore.GetInt(StarterTrainingGrantedKey, 0) != 0)
        {
            return;
        }

        if (ownedCharacters.Count == 0)
        {
            return;
        }

        GrantPlayerExperience(GetExperienceRequiredForLevel(1) + GetExperienceRequiredForLevel(2));
        PlayerProgressStore.SetInt(StarterTrainingGrantedKey, 1);
    }

    private static int GetExperienceRequiredForLevel(int level)
    {
        return PlayerExperienceCurve.Required(level);
    }

    private int GetExperienceRewardFromCurrentRequirement(float rate)
    {
        return Mathf.Max(1, Mathf.CeilToInt(GetExperienceRequiredForLevel(playerLevel) * rate));
    }

    private static int GetStageBattleExperienceReward(StageData stage)
    {
        return PlayerExperienceCurve.BattleReward(stage != null ? stage.enemyLevel : 1);
    }

    private string SerializeOwnedCharacters()
    {
        return string.Join(",", ownedCharacters
            .Where(entry => entry.Key != null && entry.Value != null)
            .Select(entry => entry.Key.characterId.ToString()));
    }

    private void DeserializeOwnedCharacters(string saved)
    {
        if (string.IsNullOrWhiteSpace(saved))
        {
            return;
        }

        ownedCharacters.Clear();
        foreach (string entry in saved.Split(','))
        {
            string[] parts = entry.Split(':');
            if (parts.Length < 1 || !int.TryParse(parts[0], out int id))
            {
                continue;
            }

            CharacterData data = FindCharacterById(id);
            if (data == null || data.isBoss)
            {
                continue;
            }

            ownedCharacters[data] = new CharacterInfo();
        }
    }

    private CharacterData FindCharacterById(int id) => GetCharacterDatabase()?.GetById(id);

    private CharacterDatabase GetCharacterDatabase()
    {
        if (characterDatabase != null)
        {
            return characterDatabase;
        }

#if UNITY_EDITOR
        characterDatabase = AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/ScriptableObjects/Characters/CharacterDatabase.asset");
#endif
        return characterDatabase != null ? characterDatabase : Resources.Load<CharacterDatabase>("CharacterDatabase");
    }
}

[Serializable]
public class CharacterInfo
{
}

public readonly struct PlayerExperienceResult
{
    public readonly int experience;
    public readonly int previousLevel;
    public readonly int level;
    public readonly int experienceIntoLevel;

    public PlayerExperienceResult(int experience, int previousLevel, int level, int experienceIntoLevel)
    {
        this.experience = experience;
        this.previousLevel = previousLevel;
        this.level = level;
        this.experienceIntoLevel = experienceIntoLevel;
    }
}
