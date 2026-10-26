using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Only paid character nodes belong to this ledger; permanent unlocks never do.
public static class CharacterInvestment
{
    private const string Key = "KanjiBattle.CharacterInvestment.Paid.v1";
    public enum Stat { Health, Attack, Technique, Defense }
    public static Stat Specialty(CharacterData data) => data != null && data.skillType is
        SkillType.NumberPassive or SkillType.Shield or SkillType.Wall or SkillType.GuardAlly
        or SkillType.ExperienceBonus or SkillType.WindShift or SkillType.ShadowCopy
        ? Stat.Defense : Stat.Technique;
    [Serializable] private sealed class Entry { public string id; public int paid; }
    [Serializable] private sealed class Ledger { public List<Entry> entries = new(); }
    private static string cachedJson;
    private static Ledger cachedLedger;
    private static Ledger Read()
    {
        string json = PlayerProgressStore.GetString(Key);
        if(cachedLedger!=null&&cachedJson==json)return cachedLedger;
        Ledger ledger;
        try { ledger = string.IsNullOrEmpty(json) ? new Ledger() : JsonUtility.FromJson<Ledger>(json); }
        catch (ArgumentException) { ledger = new Ledger(); }
        ledger ??= new Ledger();
        ledger.entries ??= new List<Entry>();
        ledger.entries = ledger.entries.Where(e => e != null && e.paid >= 0)
            .GroupBy(e => e.id).Select(g => g.First()).ToList();
        cachedJson=json;cachedLedger=ledger;
        return ledger;
    }
    public static bool Owned(string id) => Read().entries.Any(e => e.id == id);
    public static int Refund => Read().entries.Sum(e => e.paid);
    public static void Record(string id, int paid)
    {
        var ledger = Read();
        if (ledger.entries.Any(e => e.id == id)) return;
        ledger.entries.Add(new Entry { id = id, paid = Mathf.Max(0, paid) });
        cachedJson=JsonUtility.ToJson(ledger);
        PlayerProgressStore.SetString(Key, cachedJson);
        PlayerProgressStore.Save();
    }
    public static int Respec()
    {
        if (GameManager.Instance == null) return 0;
        int refund = Refund;
        Clear();
        GameManager.Instance.AddStagePoints(refund);
        CastleTree.NotifyChanged();
        return refund;
    }
    public static void Clear()
    { cachedLedger=null;cachedJson=null;PlayerProgressStore.Delete(Key);PlayerProgressStore.Save(); }
    public static string NodeId(int character, Stat stat) => $"u{character}_{(int)stat}";
    public static float Multiplier(CharacterData data, Stat stat)
    {
        if (data == null) return 1;
        if (data.InvestmentMaterials != null && data.InvestmentMaterials.Length > 0)
        {
            float weighted = 0, total = 0;
            foreach (var material in data.InvestmentMaterials)
            {
                float weight = stat == Stat.Health ? material.GetMaxHP(data.InvestmentLevel)
                    : stat == Stat.Attack ? material.GetAttack(data.InvestmentLevel) : 1;
                total += weight; weighted += weight * Multiplier(material, stat);
            }
            return total > 0 ? weighted / total : 1;
        }
        if (!Owned(NodeId(data.characterId, stat))) return 1;
        return stat == Stat.Health ? 1.20f : 1.15f;
    }
    public static string Description(Stat stat) => stat switch
    {
        Stat.Health => "最大HP +20%",
        Stat.Attack => "攻撃力 +15%",
        Stat.Defense => "防御力 +4",
        _ => "技能威力・発動率 ×1.15（確定発動は維持）"
    };
    public static string Label(Stat stat) => stat switch
    { Stat.Health => "耐久", Stat.Attack => "攻撃", Stat.Defense => "守備", _ => "技能" };
    public static int DefenseBonus(CharacterData data)
    {
        if(data==null)return 0;
        if(data.InvestmentMaterials!=null&&data.InvestmentMaterials.Length>0)
            return Mathf.RoundToInt((float)data.InvestmentMaterials.Average(DefenseBonus));
        return Owned(NodeId(data.characterId,Stat.Defense))?4:0;
    }
    public static int Attack(CharacterData data,int level)=>Mathf.RoundToInt(data.GetAttack(level)*Multiplier(data,Stat.Attack));
    public static int Defense(CharacterData data,int level)=>data.GetDefense(level)+DefenseBonus(data);
    public static int Health(CharacterData data, int level) => Mathf.Max(1,
        Mathf.RoundToInt(data.GetMaxHP(level) * Multiplier(data, Stat.Health)));
}
