using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class CharacterRecord
{
    public int id;
    // Sixths preserve equal shares for both two- and three-material recipes.
    public long damage, received, healed, prevented, guarded;
    public int wins;
    public string Summary => $"与ダメ {damage / 6d:0.#}　被ダメ {received / 6d:0.#}\n回復 {healed / 6d:0.#}　軽減 {prevented / 6d:0.#}\n肩代わり {guarded / 6d:0.#}";
}

public static class CharacterRecords
{
    private const string Key = "KanjiBattle.CharacterRecords.v1";
    [Serializable] private sealed class SaveData
    {
        public List<int> encountered = new();
        public List<CharacterRecord> records = new();
    }

    private static SaveData Load()
    {
        try
        {
            var data = JsonUtility.FromJson<SaveData>(PlayerProgressStore.GetString(Key));
            if (data?.encountered != null && data.records != null) return data;
        }
        catch (ArgumentException) { }
        return new SaveData();
    }

    public static CharacterRecord Get(int id) => Load().records.FirstOrDefault(row => row != null && row.id == id)
        ?? new CharacterRecord { id = id };
    public static bool HasEncountered(int id) => Load().encountered.Contains(id);
    public static void Encounter(int id)
    {
        if (id == 0 || id == -20000) return;
        SaveData data = Load();
        if (data.encountered.Contains(id)) return;
        data.encountered.Add(id); Save(data);
    }

    public static void AddVictory(IEnumerable<CharacterRecord> records)
    {
        SaveData data = Load();
        foreach (CharacterRecord row in records.Where(row => row.id > 0))
        {
            CharacterRecord total = data.records.FirstOrDefault(item => item != null && item.id == row.id);
            if (total == null) { total = new CharacterRecord { id = row.id }; data.records.Add(total); }
            total.damage = Add(total.damage, row.damage); total.received = Add(total.received, row.received);
            total.healed = Add(total.healed, row.healed); total.prevented = Add(total.prevented, row.prevented);
            total.guarded = Add(total.guarded, row.guarded);
            total.wins = total.wins == int.MaxValue ? int.MaxValue : total.wins + 1;
        }
        Save(data);
    }

    private static long Add(long left, long right) => left > long.MaxValue - Math.Max(0, right)
        ? long.MaxValue : Math.Max(0, left) + Math.Max(0, right);
    private static void Save(SaveData data)
    { PlayerProgressStore.SetString(Key, JsonUtility.ToJson(data)); PlayerProgressStore.Save(); }
    public static void ResetProgress() { PlayerProgressStore.Delete(Key); PlayerProgressStore.Save(); }
}

public sealed class BattleRecordBook
{
    public sealed class Entry
    {
        public int token;
        public string name;
        public int[] owners;
        public CharacterRecord result = new();
        public Sprite icon;
    }

    private readonly Dictionary<int, Entry> entries = new();
    private bool completed;
    public IEnumerable<Entry> Entries => entries.Values;
    public void Clear() { entries.Clear(); completed = false; }
    public void Register(int token, string name, Sprite icon, params int[] owners)
    {
        if (completed) return;
        entries[token] = new Entry { token = token, name = name, icon = icon, owners = owners ?? Array.Empty<int>() };
    }
    public void Remove(int token) => entries.Remove(token);
    public void Credit(int token, int damage = 0, int received = 0, int healed = 0, int prevented = 0, int guarded = 0)
    {
        if (completed || !entries.TryGetValue(token, out Entry row)) return;
        row.result.damage += Math.Max(0, damage) * 6L; row.result.received += Math.Max(0, received) * 6L;
        row.result.healed += Math.Max(0, healed) * 6L; row.result.prevented += Math.Max(0, prevented) * 6L;
        row.result.guarded += Math.Max(0, guarded) * 6L;
    }
    public List<CharacterRecord> MaterialTotals()
    {
        var totals = new Dictionary<int, CharacterRecord>();
        foreach (Entry entry in Entries.Where(entry => entry.owners.Length > 0))
            foreach (int id in entry.owners)
            {
                if (id <= 0) continue;
                if (!totals.TryGetValue(id, out CharacterRecord total)) totals[id] = total = new CharacterRecord { id = id };
                int divisor = entry.owners.Length;
                total.damage += entry.result.damage / divisor; total.received += entry.result.received / divisor;
                total.healed += entry.result.healed / divisor; total.prevented += entry.result.prevented / divisor;
                total.guarded += entry.result.guarded / divisor;
            }
        return totals.Values.ToList();
    }
    public bool Complete(bool victory, bool trial = false)
    {
        if (completed) return false;
        completed = true;
        if (victory && !trial) CharacterRecords.AddVictory(MaterialTotals());
        return true;
    }
}
