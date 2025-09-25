using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public event Action onInventoryChanged;
    public event Action OnSummonableChanged;

    [SerializeField]
    private List<CharacterData> summonableCharacters = new List<CharacterData>();

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
            DontDestroyOnLoad(gameObject);
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
    }

    public int GetEffectiveLevelCap()
    {
        return baseLevelCap + globalLevelCapBonus;
    }

    public void AddLevelCapBonus(int value)
    {
        globalLevelCapBonus += value;
        Debug.Log($"新しいレベル上限: {GetEffectiveLevelCap()}");
    }

    public void UnlockCharacterForSummon(CharacterData data)
    {
        if (data == null) return;

        if (!summonableCharacters.Contains(data))
        {
            summonableCharacters.Add(data);
            onInventoryChanged?.Invoke(); // UI更新イベントがあれば
            OnSummonableChanged?.Invoke();
            Debug.Log($"[PlayerInventory] 召喚解放: {data.characterName}");
        }
    }

    // GameManager.AddCharacterUnlock(int id) から呼ばれる想定のオーバーロード
    public void UnlockCharacterForSummon(int characterId)
    {
        var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
        if (db == null)
        {
            Debug.LogError("[PlayerInventory] CharacterDatabase が見つかりません");
            return;
        }
        var data = db.GetById(characterId);
        if (data == null)
        {
            Debug.LogError($"[PlayerInventory] characterId={characterId} が見つかりません");
            return;
        }
        UnlockCharacterForSummon(data);
    }
}

[System.Serializable]
public class CharacterInfo
{
    public int level;
    public int count;
}