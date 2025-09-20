using System;
using UnityEngine;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance;

    public event Action onInventoryChanged;

    [SerializeField]
    private List<CharacterData> summonableCharacters = new List<CharacterData>();

    private Dictionary<CharacterData, CharacterInfo> ownedCharacters = new();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
        int total = 100;
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
        int cost = entry.level * 10; // 仮のコスト計算
        if (GameManager.Instance.Gold < cost) return;

        GameManager.Instance.SpendGold(cost);
        entry.level++;

        Debug.Log($"{data.characterName} のレベルが {entry.level} になった！");

        // 🔑 UIへ通知
        onInventoryChanged?.Invoke();
    }
}

[System.Serializable]
public class CharacterInfo
{
    public int level;
    public int count;
}