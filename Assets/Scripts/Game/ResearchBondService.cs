using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ResearchBondService
{
    private const string SavePrefix = "KanjiBattle.ResearchBond.";
    private const string BondKey = SavePrefix + "Bond";
    private const string DefeatedKey = SavePrefix + "Defeated";

    private static readonly Lazy<ResearchBondService> LazyInstance = new(() => new ResearchBondService());
    public static ResearchBondService Instance => LazyInstance.Value;

    private readonly Dictionary<int, int> requiredStageByCharacter = new()
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

    private readonly Dictionary<int, int> bondByCharacterId = new();
    private readonly HashSet<int> defeatedCharacterIds = new();
    private CharacterDatabase characterDatabase;
    private bool loaded;

    public event Action OnChanged;

    private ResearchBondService() { }

    public IReadOnlyList<CharacterData> GetDefeatedCandidates()
    {
        EnsureLoaded();
        return defeatedCharacterIds
            .Select(GetCharacter)
            .Where(data => data != null && !data.isBoss && !IsOwned(data))
            .OrderBy(data => requiredStageByCharacter.TryGetValue(data.characterId, out int stageId) ? stageId : int.MaxValue)
            .ToList();
    }

    public void RegisterDefeated(CharacterData data)
    {
        EnsureLoaded();
        if (data == null || data.isBoss || IsOwned(data) || !requiredStageByCharacter.ContainsKey(data.characterId))
        {
            return;
        }

        if (defeatedCharacterIds.Add(data.characterId))
        {
            Save();
            OnChanged?.Invoke();
        }
    }

    public int GetBond(CharacterData data)
    {
        EnsureLoaded();
        return data != null && bondByCharacterId.TryGetValue(data.characterId, out int bond) ? bond : 0;
    }

    public int GetBondThreshold(CharacterData data)
    {
        int chapter = GetCharacterChapter(data);
        return chapter switch
        {
            1 => 5,
            2 => 10,
            3 => 20,
            4 => 35,
            5 => 50,
            6 => 65,
            7 => 80,
            8 => 100,
            _ => 0
        };
    }

    /// <summary>
    /// A completed bond is intentionally not an automatic recruitment. The player
    /// confirms the new ally from the roster, just like a facility unlock.
    /// </summary>
    public bool CanRecruit(CharacterData data)
    {
        EnsureLoaded();
        return data != null
            && !data.isBoss
            && !IsOwned(data)
            && defeatedCharacterIds.Contains(data.characterId)
            && GetBondThreshold(data) > 0
            && GetBond(data) >= GetBondThreshold(data);
    }

    public bool Recruit(CharacterData data)
    {
        EnsureLoaded();
        if (!CanRecruit(data) || PlayerInventory.Instance == null || !PlayerInventory.Instance.AddCharacter(data))
        {
            return false;
        }

        bondByCharacterId.Remove(data.characterId);
        Save();
        OnChanged?.Invoke();
        return true;
    }

    public IReadOnlyList<RecruitmentResult> ResolveVictory(IReadOnlyCollection<int> encounteredCharacterIds)
    {
        EnsureLoaded();
        if (encounteredCharacterIds == null || encounteredCharacterIds.Count == 0)
        {
            return Array.Empty<RecruitmentResult>();
        }

        int bondGain = FacilityManager.Instance != null ? FacilityManager.Instance.GetRecruitmentBondGain() : 1;
        float luckyCompletionChance = FacilityManager.Instance != null
            ? FacilityManager.Instance.GetLuckyBondCompletionChance()
            : 0.005f;
        List<RecruitmentResult> results = new();

        // A bond belongs to the enemy met in this victorious battle. Keeping this
        // set battle-local prevents earlier encounters from progressing elsewhere.
        foreach (int characterId in encounteredCharacterIds.OrderBy(id => id))
        {
            if (!defeatedCharacterIds.Contains(characterId))
            {
                continue;
            }

            CharacterData data = GetCharacter(characterId);
            if (data == null || IsOwned(data))
            {
                continue;
            }

            int previousBond = GetBond(data);
            int threshold = GetBondThreshold(data);
            bool luckyCompletion = previousBond < threshold && UnityEngine.Random.value < luckyCompletionChance;
            int updatedBond = luckyCompletion
                ? threshold
                : Mathf.Min(threshold, previousBond + bondGain);
            bondByCharacterId[characterId] = updatedBond;
            bool readyToRecruit = previousBond < threshold && updatedBond >= threshold;
            results.Add(new RecruitmentResult(data, updatedBond - previousBond, updatedBond, threshold, readyToRecruit, luckyCompletion));
        }

        Save();
        if (results.Count > 0)
        {
            OnChanged?.Invoke();
        }

        return results;
    }

    public void ResetProgress()
    {
        bondByCharacterId.Clear();
        defeatedCharacterIds.Clear();
        PlayerProgressStore.Delete(BondKey);
        PlayerProgressStore.Delete(DefeatedKey);
        PlayerProgressStore.Save();
        OnChanged?.Invoke();
    }

    private bool IsOwned(CharacterData data) => PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(data);

    private int GetCharacterChapter(CharacterData data)
    {
        if (data == null || !requiredStageByCharacter.TryGetValue(data.characterId, out int stageId))
        {
            return 0;
        }

        return ((stageId - 1) / 5) + 1;
    }

    private CharacterData GetCharacter(int characterId)
    {
        EnsureDatabase();
        return characterDatabase != null ? characterDatabase.GetById(characterId) : null;
    }

    private void EnsureLoaded()
    {
        PlayerProgressStore.EnsureCurrentFormat();
        EnsureDatabase();
        if (loaded)
        {
            return;
        }

        DeserializeDictionary(PlayerProgressStore.GetString(BondKey), bondByCharacterId);
        DeserializeSet(PlayerProgressStore.GetString(DefeatedKey), defeatedCharacterIds);
        loaded = true;
    }

    private void EnsureDatabase()
    {
        if (characterDatabase == null)
        {
            characterDatabase = Resources.Load<CharacterDatabase>("CharacterDatabase");
        }
    }

    private void Save()
    {
        PlayerProgressStore.SetString(BondKey, string.Join(",", bondByCharacterId.OrderBy(entry => entry.Key).Select(entry => $"{entry.Key}:{entry.Value}")));
        PlayerProgressStore.SetString(DefeatedKey, string.Join(",", defeatedCharacterIds.OrderBy(id => id)));
        PlayerProgressStore.Save();
    }

    private static void DeserializeSet(string serialized, ISet<int> destination)
    {
        if (string.IsNullOrWhiteSpace(serialized)) return;
        foreach (string value in serialized.Split(','))
        {
            if (int.TryParse(value, out int id)) destination.Add(id);
        }
    }

    private static void DeserializeDictionary(string serialized, IDictionary<int, int> destination)
    {
        if (string.IsNullOrWhiteSpace(serialized)) return;
        foreach (string value in serialized.Split(','))
        {
            string[] parts = value.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int id) && int.TryParse(parts[1], out int bond))
            {
                destination[id] = Mathf.Max(0, bond);
            }
        }
    }
}

public readonly struct RecruitmentResult
{
    public readonly CharacterData character;
    public readonly int bondGained;
    public readonly int bond;
    public readonly int threshold;
    public readonly bool readyToRecruit;
    public readonly bool luckyCompletion;

    public RecruitmentResult(CharacterData character, int bondGained, int bond, int threshold, bool readyToRecruit, bool luckyCompletion)
    {
        this.character = character;
        this.bondGained = bondGained;
        this.bond = bond;
        this.threshold = threshold;
        this.readyToRecruit = readyToRecruit;
        this.luckyCompletion = luckyCompletion;
    }
}
