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
    private const string StarterTrainingGrantedKey = SavePrefix + "PlayerTrainingGranted";
    private const int LevelCap = 99;
    public const int BaseBattleExperience = 10;
    private const int StarterTrainingExperience = BaseBattleExperience * 2;
    private const int ExperienceTierSize = 5;
    private const int MaximumExperienceTier = 5;

    public event Action onInventoryChanged;

    [SerializeField] private CharacterDatabase characterDatabase;
    private readonly Dictionary<CharacterData, CharacterInfo> ownedCharacters = new();
    private bool isLoadingProgress;
    private int playerLevel = 1;
    private int playerExperience;

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

    public Dictionary<CharacterData, CharacterInfo> GetOwnedCharacters() => ownedCharacters;
    public int PlayerLevel => playerLevel;
    public int PlayerExperience => playerExperience;

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

    public PlayerExperienceResult GrantBattleExperience()
    {
        return GrantPlayerExperience(GetEffectiveBattleExperienceReward());
    }

    public PlayerExperienceResult GrantPlayerExperience(int amount)
    {
        int previousLevel = playerLevel;
        if (amount <= 0 || playerLevel >= GetEffectiveLevelCap())
        {
            return new PlayerExperienceResult(0, previousLevel, playerLevel, playerExperience);
        }

        playerExperience += amount;
        int levelCap = GetEffectiveLevelCap();
        while (playerLevel < levelCap && playerExperience >= GetExperienceRequiredForLevel(playerLevel))
        {
            playerExperience -= GetExperienceRequiredForLevel(playerLevel);
            playerLevel++;
        }

        if (playerLevel >= levelCap)
        {
            playerLevel = levelCap;
            playerExperience = 0;
        }

        onInventoryChanged?.Invoke();
        SaveProgress();
        return new PlayerExperienceResult(amount, previousLevel, playerLevel, playerExperience);
    }

    public int GetEffectiveLevelCap()
    {
        int clearedStageId = GameManager.Instance != null ? GameManager.Instance.GetHighestClearedStageId() : 0;
        int unlockedBands = Mathf.Max(0, clearedStageId / 5);
        return Mathf.Clamp(5 + unlockedBands * 5, 5, LevelCap);
    }

    public void SetPlayerLevelForDebug(int level)
    {
        playerLevel = Mathf.Clamp(level, 1, GetEffectiveLevelCap());
        playerExperience = 0;
        onInventoryChanged?.Invoke();
        SaveProgress();
    }

    public int GetEffectiveBattleExperienceReward()
    {
        float multiplier = FacilityManager.Instance != null ? FacilityManager.Instance.GetExperienceMultiplier() : 1f;
        return Mathf.Max(1, Mathf.FloorToInt(BaseBattleExperience * multiplier));
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
        PlayerProgressStore.Save();
    }

    public void LoadProgress()
    {
        isLoadingProgress = true;
        DeserializeOwnedCharacters(PlayerProgressStore.GetString(OwnedKey));
        playerLevel = Mathf.Clamp(PlayerProgressStore.GetInt(PlayerLevelKey, 1), 1, GetEffectiveLevelCap());
        playerExperience = Mathf.Max(0, PlayerProgressStore.GetInt(PlayerExperienceKey, 0));
        isLoadingProgress = false;
        onInventoryChanged?.Invoke();
    }

    public void ResetProgress()
    {
        PlayerProgressStore.Delete(OwnedKey);
        PlayerProgressStore.Delete(PlayerLevelKey);
        PlayerProgressStore.Delete(PlayerExperienceKey);
        PlayerProgressStore.Delete(StarterTrainingGrantedKey);
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
        if (ownedCharacters.Count > 0)
        {
            return;
        }

        CharacterData starter = GetCharacterDatabase()?.GetById(1);
        if (starter != null)
        {
            ownedCharacters[starter] = new CharacterInfo();
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

        GrantPlayerExperience(StarterTrainingExperience);
        PlayerProgressStore.SetInt(StarterTrainingGrantedKey, 1);
    }

    private static int GetExperienceRequiredForLevel(int level)
    {
        // Five-level bands create clear growth milestones without overflowing the experience counter.
        int tier = Mathf.Clamp((Mathf.Max(1, level) - 1) / ExperienceTierSize, 0, MaximumExperienceTier);
        return BaseBattleExperience * (int)Mathf.Pow(10f, tier);
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
