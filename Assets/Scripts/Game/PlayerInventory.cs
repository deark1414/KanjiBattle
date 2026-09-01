using System;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;
    private const string SavePrefix = "KanjiBattle.Inventory.";
    private const string OwnedKey = SavePrefix + "Owned";
    private const string SummonableKey = SavePrefix + "Summonable";
    private const string LevelCapBonusKey = SavePrefix + "LevelCapBonus";
    private bool isLoadingProgress;

    public event Action onInventoryChanged;
    public event Action OnSummonableChanged;

    [SerializeField]
    private List<CharacterData> summonableCharacters = new List<CharacterData>();
    [SerializeField]
    private CharacterDatabase characterDatabase;
    private List<CharacterData> initialSummonableCharacters = new List<CharacterData>();

    [SerializeField] private int baseLevelCap = 5;
    private int globalLevelCapBonus = 0;

    // --- Facility effect fields removed ---

    private Dictionary<CharacterData, CharacterInfo> ownedCharacters = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCostModifiersChanged += HandleCostModifiersChanged;
            }
            initialSummonableCharacters = new List<CharacterData>(summonableCharacters);
            DontDestroyOnLoad(gameObject);
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCostModifiersChanged -= HandleCostModifiersChanged;
        }
    }

    private void HandleCostModifiersChanged()
    {
        // 通知: 強化コストや召喚コストが変化した際にUIを更新
        onInventoryChanged?.Invoke();
        OnSummonableChanged?.Invoke();
    }

    public Dictionary<CharacterData, CharacterInfo> GetOwnedCharacters()
    {
        return ownedCharacters;
    }

    public List<CharacterData> GetUnlockedCharacters()
    {
        return new List<CharacterData>(ownedCharacters.Keys);
    }

    public void AddCharacter(CharacterData data)
    {
        if (ownedCharacters.ContainsKey(data))
        {
            ownedCharacters[data].count++;
        }
        else
        {
            ownedCharacters[data] = new CharacterInfo { level = 1, count = 1 };
        }
        onInventoryChanged?.Invoke();
        SaveProgress();
    }

    public List<CharacterData> GetSummonableCharacters()
    {
        return summonableCharacters;
    }
    
    public int GetTotalProduction()
    {
        int total = 0;
        foreach (var kvp in ownedCharacters)
        {
            var data = kvp.Key;
            var info = kvp.Value;
            total += data.production * info.count;
        }
        return total;
    }

    public void UpgradeCharacter(CharacterData data)
    {
        if (!ownedCharacters.ContainsKey(data)) return;

        var entry = ownedCharacters[data];
        if (entry.level >= GetEffectiveLevelCap())
        {
            Debug.Log($"{data.characterName} のレベルは上限({GetEffectiveLevelCap()})に達しています。");
            return;
        }
        int baseCost = data.GetUpgradeCost(entry.level);
        int effectiveCost = GameManager.Instance.GetEffectiveUpgradeCost(baseCost);
        if (!GameManager.Instance.SpendGold(effectiveCost)) return;
        entry.level++;

        Debug.Log($"{data.characterName} のレベルが {entry.level} になった！");

        // 🔑 UIへ通知
        onInventoryChanged?.Invoke();
        SaveProgress();
    }

    public int GetEffectiveLevelCap()
    {
        return baseLevelCap + globalLevelCapBonus;
    }

    public void AddLevelCapBonus(int value)
    {
        globalLevelCapBonus += value;
        Debug.Log($"新しいレベル上限: {GetEffectiveLevelCap()}");
        SaveProgress();
    }

    public bool IsSummonable(CharacterData data)
    {
        return data != null && summonableCharacters.Contains(data);
    }

    public bool UnlockCharacterForSummon(CharacterData data)
    {
        if (data == null) return false;

        if (summonableCharacters.Contains(data))
        {
            return false;
        }

        summonableCharacters.Add(data);
        onInventoryChanged?.Invoke();
        OnSummonableChanged?.Invoke();
        Debug.Log($"[PlayerInventory] 召喚解放: {data.characterName}");
        SaveProgress();
        return true;
    }

    // GameManager.AddCharacterUnlock(int id) から呼ばれる想定のオーバーロード
    public bool UnlockCharacterForSummon(int characterId)
    {
        var db = GetCharacterDatabase();
#if UNITY_EDITOR
        if (db == null)
        {
            db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/ScriptableObjects/Characters/CharacterDatabase.asset");
        }
#endif
        if (db == null)
        {
            Debug.LogError("[PlayerInventory] CharacterDatabase が見つかりません");
            return false;
        }

        var data = db.GetById(characterId);
        if (data == null)
        {
            Debug.LogError($"[PlayerInventory] characterId={characterId} が見つかりません");
            return false;
        }

        return UnlockCharacterForSummon(data);
    }

    public void SaveProgress()
    {
        if (isLoadingProgress) return;

        PlayerPrefs.SetInt(LevelCapBonusKey, globalLevelCapBonus);
        PlayerPrefs.SetString(OwnedKey, SerializeOwnedCharacters());
        PlayerPrefs.SetString(SummonableKey, string.Join(",", summonableCharacters.FindAll(c => c != null).ConvertAll(c => c.characterId.ToString())));
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        isLoadingProgress = true;
        globalLevelCapBonus = PlayerPrefs.GetInt(LevelCapBonusKey, globalLevelCapBonus);
        DeserializeOwnedCharacters(PlayerPrefs.GetString(OwnedKey, ""));
        DeserializeSummonableCharacters(PlayerPrefs.GetString(SummonableKey, ""));
        isLoadingProgress = false;
        onInventoryChanged?.Invoke();
        OnSummonableChanged?.Invoke();
    }

    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(OwnedKey);
        PlayerPrefs.DeleteKey(SummonableKey);
        PlayerPrefs.DeleteKey(LevelCapBonusKey);
        ownedCharacters.Clear();
        summonableCharacters.Clear();
        summonableCharacters.AddRange(initialSummonableCharacters.FindAll(c => c != null));
        globalLevelCapBonus = 0;
        onInventoryChanged?.Invoke();
        OnSummonableChanged?.Invoke();
        PlayerPrefs.Save();
    }

    private string SerializeOwnedCharacters()
    {
        var entries = new List<string>();
        foreach (var kvp in ownedCharacters)
        {
            if (kvp.Key == null || kvp.Value == null) continue;
            entries.Add($"{kvp.Key.characterId}:{kvp.Value.level}:{kvp.Value.count}");
        }
        return string.Join(",", entries);
    }

    private void DeserializeOwnedCharacters(string saved)
    {
        if (string.IsNullOrWhiteSpace(saved)) return;

        ownedCharacters.Clear();
        foreach (string entry in saved.Split(','))
        {
            string[] parts = entry.Split(':');
            if (parts.Length != 3) continue;
            if (!int.TryParse(parts[0], out int id)) continue;
            if (!int.TryParse(parts[1], out int level)) continue;
            if (!int.TryParse(parts[2], out int count)) continue;

            CharacterData data = FindCharacterById(id);
            if (data == null)
            {
                Debug.LogWarning($"[PlayerInventory] 保存済み所持キャラ characterId={id} を復元できませんでした。");
                continue;
            }
            ownedCharacters[data] = new CharacterInfo { level = Mathf.Max(1, level), count = Mathf.Max(0, count) };
        }
    }

    private void DeserializeSummonableCharacters(string saved)
    {
        if (string.IsNullOrWhiteSpace(saved)) return;

        summonableCharacters.Clear();
        foreach (string part in saved.Split(','))
        {
            if (!int.TryParse(part, out int id)) continue;
            CharacterData data = FindCharacterById(id);
            if (data != null && !summonableCharacters.Contains(data))
            {
                summonableCharacters.Add(data);
            }
            else if (data == null)
            {
                Debug.LogWarning($"[PlayerInventory] 保存済み召喚解放キャラ characterId={id} を復元できませんでした。");
            }
        }
    }

    private CharacterData FindCharacterById(int id)
    {
        foreach (var character in summonableCharacters)
        {
            if (character != null && character.characterId == id) return character;
        }
        foreach (var character in ownedCharacters.Keys)
        {
            if (character != null && character.characterId == id) return character;
        }

        var db = GetCharacterDatabase();
#if UNITY_EDITOR
        if (db == null)
        {
            db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/ScriptableObjects/Characters/CharacterDatabase.asset");
        }
#endif
        return db != null ? db.GetById(id) : null;
    }

    private CharacterDatabase GetCharacterDatabase()
    {
        if (characterDatabase != null)
        {
            return characterDatabase;
        }

        return Resources.Load<CharacterDatabase>("CharacterDatabase");
    }
}

[System.Serializable]
public class CharacterInfo
{
    public int level;
    public int count;
}
