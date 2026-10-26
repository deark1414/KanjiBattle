using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CastleTree
{
    public const int InitialSlots = 3, MaximumSlots = 8;
    private const string SlotsKey = "KanjiBattle.CastleTree.Slots.v1";
    public enum Kind { Character, Facility, Slot, Upgrade, CategoryUpgrade }
    public sealed class Node
    {
        public string id, parent;
        public Kind kind;
        public int target, branch, stage, cost;
        public CharacterInvestment.Stat stat;
    }
    public static readonly string[] Branches = { "武具", "守備", "数・支援", "自然", "動物" };
    public static readonly IReadOnlyList<Node> Nodes = CreateNodes();
    public static event Action Changed;
    private static List<Node> CreateNodes()
    {
        var nodes = new List<Node>();
        void C(int id,int branch,int stage,int cost,string parent=null) => nodes.Add(new Node
            {id="c"+id,kind=Kind.Character,target=id,branch=branch,stage=stage,cost=cost,parent=parent});
        void S(int id,int branch,int stage,int cost,string parent) => nodes.Add(new Node
            {id="s"+id,kind=Kind.Slot,target=id,branch=branch,stage=stage,cost=cost,parent=parent});
        void F(int id,int branch,string parent=null) => nodes.Add(new Node
            {id="f"+id,kind=Kind.Facility,target=id,branch=branch,parent=parent});
        C(10,0,0,0); C(11,0,0,6,"c10"); S(1,0,5,10,"c11");
        C(12,0,5,12,"c11"); C(16,0,0,6,"c10"); C(17,0,5,14,"c16"); C(18,0,10,22,"c17");
        C(13,1,0,6); C(14,1,5,12,"c13"); S(2,1,10,18,"c14"); C(15,1,10,20,"c14");
        F(12,1);
        C(1,2,0,0); C(2,2,0,6,"c1"); C(3,2,5,12,"c2");
        C(27,2,5,14,"c1"); C(28,2,10,20,"c27"); C(29,2,15,26,"c28");
        S(3,2,20,28,"c27"); F(6,2);
        C(20,3,0,0); C(19,3,0,6,"c20"); C(21,3,5,12,"c19"); C(22,3,10,20,"c21");
        C(31,3,25,36,"c19"); C(32,3,30,42,"c31"); S(5,3,35,50,"c32"); C(33,3,35,48,"c31");
        F(13,3,"c19"); F(3,3);
        C(23,4,10,20); C(24,4,15,26,"c23"); C(25,4,20,30,"c23"); C(30,4,20,28,"c23");
        S(4,4,30,38,"c23"); F(4,4); F(7,4); F(5,4);
        for(int branch=0;branch<5;branch++)
            foreach(var stat in new[]{CharacterInvestment.Stat.Effect,CharacterInvestment.Stat.EffectII})
                nodes.Add(new Node{id=CharacterInvestment.CategoryId(branch,stat),kind=Kind.CategoryUpgrade,
                    parent=stat==CharacterInvestment.Stat.EffectII?CharacterInvestment.CategoryId(branch,CharacterInvestment.Stat.Effect):null,
                    target=branch,branch=branch,stat=stat,stage=stat==CharacterInvestment.Stat.Effect?10:25,
                    cost=stat==CharacterInvestment.Stat.Effect?10:18});
        return nodes;
    }
    public static int SkillLevel(Node node)=>CharacterInvestment.Upgrades(Character(node)).Count(stat=>CharacterInvestment.Owned(CharacterInvestment.NodeId(node.target,stat)));
    public static Node NextUpgrade(Node character)
    {
        foreach(var stat in CharacterInvestment.Upgrades(Character(character)))
        {
            string id=CharacterInvestment.NodeId(character.target,stat);
            if(CharacterInvestment.Owned(id))continue;
            bool second=CharacterInvestment.IsSecond(stat);
            bool chance=stat==CharacterInvestment.Stat.Chance&&Character(character)?.skillType!=SkillType.BellEcho;
            return new Node{id=id,target=character.target,kind=Kind.Upgrade,branch=character.branch,stat=stat,
                stage=Mathf.Max(character.stage,second?20:chance?10:5),cost=second?8:chance?6:4};
        }
        return null;
    }
    public static CharacterData Character(Node node) => Resources.Load<CharacterDatabase>("CharacterDatabase")?.GetById(node.target);
    public static FacilityData Facility(Node node) => FacilityManager.Instance?.GetFacilities().FirstOrDefault(f=>f.facilityId==node.target);
    public static string Name(Node node) => node.kind switch
    {
        Kind.Character => Character(node)?.characterName ?? "？",
        Kind.Facility => Facility(node)?.facilityName ?? "施設",
        Kind.Upgrade => (Character(node)?.characterName??"？")+"・"+CharacterInvestment.Label(node.stat),
        Kind.CategoryUpgrade => Branches[node.branch]+"・"+CharacterInvestment.Label(node.stat),
        _ => "編成＋1"
    };
    private static HashSet<int> ReadSlots() => PlayerProgressStore.GetString(SlotsKey).Split(',')
        .Select(value=>int.TryParse(value,out int id)?id:0).Where(id=>id>=1&&id<=5).ToHashSet();
    public static int Slots => InitialSlots + ReadSlots().Count;
    public static bool Owned(Node node) => node.kind switch
    {
        Kind.Character => PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(Character(node)),
        Kind.Facility => FacilityManager.Instance != null && FacilityManager.Instance.IsUnlocked(Facility(node)),
        Kind.Upgrade or Kind.CategoryUpgrade => CharacterInvestment.Owned(node.id),
        _ => ReadSlots().Contains(node.target)
    };
    public static int RequiredStage(Node node) => node.kind==Kind.Facility
        ? Mathf.CeilToInt(Mathf.Max(0,Facility(node)?.requiredStageId ?? 0)/5f)*5
        : node.kind==Kind.Character&&Owned(node)?NextUpgrade(node)?.stage??node.stage:node.stage;
    public static bool Visible(Node node) => Owned(node)||
        (RequiredStage(node)<=(GameManager.Instance?.GetHighestClearedStageId()??0)
        &&(node.kind!=Kind.Upgrade||PlayerInventory.Instance!=null&&PlayerInventory.Instance.IsOwned(Character(node))));
    public static int Cost(Node node)
    {
        if(node.kind==Kind.Character&&Owned(node))return NextUpgrade(node)?.cost??0;
        if(node.kind!=Kind.Facility)return node.cost;
        var f=Facility(node);var manager=FacilityManager.Instance;
        return manager==null||f==null ? 0 : Owned(node)?manager.GetUpgradeCost(f):manager.GetUnlockCost(f);
    }
    public static string Blocked(Node node)
    {
        if(node==null)return "不明な解放先";
        if(Owned(node)&&node.kind==Kind.Character&&NextUpgrade(node)==null)
            return Character(node)?.skillType==SkillType.Wall?"固定スキル":"スキル強化完了";
        if(Owned(node)&&node.kind is not (Kind.Facility or Kind.Character))return "解放済";
        if(node.kind==Kind.Character&&Owned(node)&&(GameManager.Instance?.GetHighestClearedStageId()??0)<RequiredStage(node))return $"第{RequiredStage(node)}局突破で次の強化";
        // Existing unlocks remain usable even when a new tree prerequisite was added.
        if(!Owned(node))
        {
            int cleared=GameManager.Instance?.GetHighestClearedStageId()??0;
            if(cleared<RequiredStage(node))return $"第{RequiredStage(node)}局突破で解禁";
            if(node.parent!=null&&!Owned(Nodes.First(n=>n.id==node.parent)))return Name(Nodes.First(n=>n.id==node.parent))+"の解放が必要";
        }
        if(node.kind==Kind.Facility && Owned(node) && FacilityManager.Instance.IsMaxLevel(Facility(node)))
        {
            var f=Facility(node);
            int next=FacilityManager.Instance.GetNextFacilityLevelCapRequirement(f)?.stageId ?? -1;
            return next>0?$"上限到達・第{next}局突破で拡張":"最大強化済";
        }
        return (GameManager.Instance?.StagePoints??0)<Cost(node)?$"戦果不足　必要 {Cost(node)}":"";
    }
    public static string Status(Node node)
    {
        string blocked=Blocked(node);
        if(blocked.Length>0)return blocked;
        return $"{(Owned(node)?"強化":"解放")}可能　戦果 {Cost(node)}";
    }
    public static bool Purchase(string id)
    {
        var node=Nodes.FirstOrDefault(n=>n.id==id);
        if(node==null||Blocked(node).Length!=0)return false;
        if(node.kind==Kind.Facility)
        {
            bool success=Owned(node)?FacilityManager.Instance.Upgrade(Facility(node)):FacilityManager.Instance.Unlock(Facility(node));
            if(success)Changed?.Invoke();return success;
        }
        if(node.kind==Kind.Character && (Character(node)==null||Character(node).isBoss))return false;
        if(!GameManager.Instance.SpendStagePoints(Cost(node)))return false;
        if(node.kind==Kind.Character&&Owned(node))
        {var upgrade=NextUpgrade(node);CharacterInvestment.Record(upgrade.id,upgrade.cost);}
        else if(node.kind==Kind.Character)PlayerInventory.Instance.AddCharacter(Character(node));
        else if(node.kind is Kind.Upgrade or Kind.CategoryUpgrade)CharacterInvestment.Record(node.id,Cost(node));
        else
        {
            var slots=ReadSlots();slots.Add(node.target);
            PlayerProgressStore.SetString(SlotsKey,string.Join(",",slots.OrderBy(x=>x)));PlayerProgressStore.Save();
        }
        Changed?.Invoke();return true;
    }
    public static void AdoptLegacySlots(int total)
    {
        var slots=ReadSlots();
        int previous=slots.Count;
        for(int i=1;i<=Mathf.Clamp(total-InitialSlots,0,5);i++)slots.Add(i);
        if(slots.Count==previous)return;
        PlayerProgressStore.SetString(SlotsKey,string.Join(",",slots.OrderBy(x=>x)));PlayerProgressStore.Save();
    }
    public static void NotifyChanged()=>Changed?.Invoke();
    public static void ResetProgress(){CharacterInvestment.Clear();PlayerProgressStore.Delete(SlotsKey);PlayerProgressStore.Save();Changed?.Invoke();}
}
