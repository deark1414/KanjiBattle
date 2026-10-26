using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Skill investments share a refundable ledger; permanent unlocks never belong here.
public static class CharacterInvestment
{
    private const string Key = "KanjiBattle.CharacterInvestment.Paid.v1";
    // Keep old numeric IDs reserved so their purchases can be refunded, not reinterpreted.
    public enum Stat { Health, Attack, Technique, Defense, Effect, Chance, EffectII, ChanceII, Duration, DurationII }
    public static Stat[] Upgrades(CharacterData data)
    {
        if(data?.skillType==SkillType.Wall)return Array.Empty<Stat>();
        if(data?.skillType==SkillType.BellEcho)return new[]{Stat.Chance,Stat.ChanceII};
        if(data?.skillType==SkillType.WindShift)return new[]{Stat.Duration,Stat.DurationII};
        return HasChance(data?.skillType??SkillType.None)
            ? new[]{Stat.Effect,Stat.Chance,Stat.EffectII} : new[]{Stat.Effect,Stat.EffectII};
    }
    public static bool IsSecond(Stat stat)=>stat is Stat.EffectII or Stat.ChanceII or Stat.DurationII;
    public static bool HasChance(SkillType skill)=>skill is SkillType.StunBlow or SkillType.WoodPush
        or SkillType.Soil or SkillType.HorseCharge or SkillType.BirdRetreat or SkillType.TigerTwinClaw
        or SkillType.Counter or SkillType.AreaCounter or SkillType.BellEcho;
    public static int Branch(CharacterData data)=>data==null||data.isBoss?-1:
        data.characterId is 1 or 2 or 3 or 27 or 28 or 29?2:data.category switch
        {CharacterCategory.Weapon or CharacterCategory.Ranged=>0,CharacterCategory.Defense=>1,
            CharacterCategory.Nature=>3,CharacterCategory.Animal=>4,_=>-1};
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
    public static string[] Purchased=>Read().entries.Select(e=>e.id).ToArray();
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
    public static string CategoryId(int branch,Stat stat)=>$"k{branch}_{(int)stat}";
    public static int RefundLegacyStats()
    {
        if(GameManager.Instance==null)return 0;
        bool Legacy(Entry entry)
            => IsRetiredUpgrade(entry.id);
        var ledger=Read();int refund=ledger.entries.Where(Legacy).Sum(e=>e.paid);
        if(ledger.entries.RemoveAll(e=>Legacy(e))==0)return 0;
        cachedJson=JsonUtility.ToJson(ledger);PlayerProgressStore.SetString(Key,cachedJson);PlayerProgressStore.Save();
        GameManager.Instance.AddStagePoints(refund);CastleTree.NotifyChanged();return refund;
    }
    public static bool IsRetiredUpgrade(string id)
    {
        var parts=(id??"").Split('_');
        return parts.Length==2&&parts[0].StartsWith("u")&&int.TryParse(parts[1],out int value)
            && ((value>=0&&value<=3)||(parts[0]=="u15"&&value>=4&&value<=7));
    }
    public static float Effect(CharacterData data)
    {
        if(data==null)return 1;
        if(data.InvestmentMaterials?.Length>0)return data.InvestmentMaterials.Average(Effect);
        // Generated copies already contain resolved values and never inherit a second time.
        if(data.characterId<0||data.isBoss||data.skillType==SkillType.WindShift)return 1;
        float bonus=(Owned(NodeId(data.characterId,Stat.Effect))?.1f:0)
            +(Owned(NodeId(data.characterId,Stat.EffectII))?.1f:0);
        int branch=Branch(data);
        if(branch>=0)bonus+=(Owned(CategoryId(branch,Stat.Effect))?.05f:0)
            +(Owned(CategoryId(branch,Stat.EffectII))?.05f:0);
        return 1+bonus;
    }
    public static float ChanceBonus(CharacterData data)
    {
        if(data==null)return 0;
        if(data.InvestmentMaterials?.Length>0)return data.InvestmentMaterials.Average(ChanceBonus);
        if(data.characterId<0||!HasChance(data.skillType))return 0;
        return (Owned(NodeId(data.characterId,Stat.Chance))?5:0)+(Owned(NodeId(data.characterId,Stat.ChanceII))?5:0);
    }
    public static float WaterRecoveryRatio(CharacterData data,float effect)=>
        Mathf.Min(.6f,data.skillPower+Mathf.Max(0,effect-1));
    public static int AdjustChance(CharacterData source,SkillType skill,int chance,bool ally)
    {
        if(!ally||source?.skillType==SkillType.ShadowCopy)return chance;
        float factor=skill is SkillType.BellEcho or SkillType.WindChime?Effect(source):1;
        return Mathf.Clamp(Mathf.RoundToInt(chance*factor+ChanceBonus(source)),0,95);
    }
    public static int WindDuration(CharacterData data)=>1+
        (data!=null&&Owned(NodeId(data.characterId,Stat.Duration))?1:0)+
        (data!=null&&Owned(NodeId(data.characterId,Stat.DurationII))?1:0);
    public static string Label(Stat stat)=>stat switch
    {Stat.Chance=>"発動率 I",Stat.ChanceII=>"発動率 II",Stat.EffectII=>"効果 II",
        Stat.Duration=>"持続 I",Stat.DurationII=>"持続 II",_=>"効果 I"};
    public static string EffectLabel(CharacterData data)=>data?.skillType switch
    {
        SkillType.NumberPassive=>"結束の攻撃上昇量",SkillType.Shield=>"被害軽減率",
        SkillType.GuardAlly or SkillType.ProjectileGuard=>"肩代わり率",SkillType.AreaCounter or SkillType.Counter=>"反撃割合",
        SkillType.WaterHeal=>"回復量",SkillType.Soil=>"土罠ダメージ",SkillType.ShadowCopy=>"変身時の攻撃力",
        SkillType.ExperienceBonus=>"経験値ボーナス",_=>"攻撃スキルの威力"
    };
    public static string Description(Stat stat,CharacterData data=null)=>stat switch
    {
        Stat.Chance or Stat.ChanceII=>"発動率 +5ポイント",
        Stat.Duration or Stat.DurationII=>"追風・向風 +1ターン",
        _=>data?.skillType==SkillType.WaterHeal?"回復率 +10ポイント":EffectLabel(data)+" +10%"
    };
    public static string CategoryDescription(int branch)=>new[]{"攻撃スキル威力 +5%", "軽減・肩代わり・反撃割合 +5%",
        "結束・糸・変身・再響の効果 +5%", "攻撃・土罠 +5%／水の回復率 +5ポイント", "攻撃技能・遠距離肩代わりの効果 +5%"}[branch];
    public static string CurrentSummary(CharacterData data)
    {
        if(data==null)return "";
        float effect=Effect(data);
        return data.skillType switch
        {
            SkillType.BellEcho=> $"再発動率 {SkillExecutor.GetSkillChance(data,data.skillType,true)}%",
            SkillType.WindShift=>$"追風・向風 {WindDuration(data)}ターン",
            SkillType.WaterHeal=>$"回復 最大HPの{WaterRecoveryRatio(data,effect)*100:0.#}%",
            SkillType.Shield=>$"被害軽減 {20*effect:0.#}%",
            SkillType.GuardAlly=>$"肩代わり {data.skillPower*100*effect:0.#}%",
            SkillType.ProjectileGuard=>$"遠距離肩代わり {Mathf.Clamp01(data.skillPower*effect)*100:0.#}% / 引受分を30%軽減",
            SkillType.Wall=>$"遠距離軽減 {Mathf.Clamp01(data.skillPower)*100:0.#}% / 銃の貫通停止（固定）",
            SkillType.ExperienceBonus=>$"勝利経験値 ×{1+(data.skillPower-1)*effect:0.##}",
            SkillType.ShadowCopy=>$"変身時の攻撃力 ×{effect:0.##}",
            SkillType.Magnet or SkillType.Cocoon=>"距離・対象数・保護回数は固定",
            _=>$"スキル効果 ×{effect:0.##}"+(HasChance(data.skillType)?$" / 発動 {SkillExecutor.GetSkillChance(data,data.skillType,true)}%":"")
        };
    }
    // Base stats are solely determined by the shared level, not the skill tree.
    public static int Attack(CharacterData data,int level)=>data.GetAttack(level);
    public static int Defense(CharacterData data,int level)=>data.GetDefense(level);
    public static int Health(CharacterData data,int level)=>Mathf.Max(1,data.GetMaxHP(level));
}
