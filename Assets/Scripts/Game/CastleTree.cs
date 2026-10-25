using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CastleTree
{
    public const int InitialSlots = 3, MaximumSlots = 8;
    private const string SlotsKey = "KanjiBattle.CastleTree.Slots.v1";
    public enum Kind { Character, Facility, Slot }
    public sealed class Node
    {
        public string id, parent;
        public Kind kind;
        public int target, branch, stage, cost;
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
        F(8,0,"c10"); F(9,0,"c11");
        C(13,1,0,6); C(14,1,5,12,"c13"); S(2,1,10,18,"c14"); C(15,1,10,20,"c14");
        F(11,1,"c13"); F(12,1);
        C(1,2,0,0); C(2,2,0,6,"c1"); C(3,2,5,12,"c2");
        C(27,2,5,14,"c1"); C(28,2,10,20,"c27"); C(29,2,15,26,"c28");
        S(3,2,20,28,"c27"); F(10,2,"c27"); F(6,2);
        C(20,3,0,0); C(19,3,0,6,"c20"); C(21,3,5,12,"c19"); C(22,3,10,20,"c21");
        C(31,3,25,36,"c19"); C(32,3,30,42,"c31"); S(5,3,35,50,"c32"); C(33,3,35,48,"c31");
        F(13,3,"c19"); F(3,3);
        C(23,4,10,20); C(24,4,15,26,"c23"); C(25,4,20,30,"c23"); C(30,4,20,28,"c23");
        S(4,4,30,38,"c23"); F(4,4); F(7,4); F(5,4);
        return nodes;
    }
    public static CharacterData Character(Node node) => Resources.Load<CharacterDatabase>("CharacterDatabase")?.GetById(node.target);
    public static FacilityData Facility(Node node) => FacilityManager.Instance?.GetFacilities().FirstOrDefault(f=>f.facilityId==node.target);
    public static string Name(Node node) => node.kind switch
    {
        Kind.Character => Character(node)?.characterName ?? "？",
        Kind.Facility => Facility(node)?.facilityName ?? "施設",
        _ => $"編成 {InitialSlots+node.target}枠"
    };
    private static HashSet<int> ReadSlots() => PlayerProgressStore.GetString(SlotsKey).Split(',')
        .Select(value=>int.TryParse(value,out int id)?id:0).Where(id=>id>=1&&id<=5).ToHashSet();
    public static int Slots => InitialSlots + ReadSlots().Count;
    public static bool Owned(Node node) => node.kind switch
    {
        Kind.Character => PlayerInventory.Instance != null && PlayerInventory.Instance.IsOwned(Character(node)),
        Kind.Facility => FacilityManager.Instance != null && FacilityManager.Instance.IsUnlocked(Facility(node)),
        _ => ReadSlots().Contains(node.target)
    };
    public static int RequiredStage(Node node) => node.kind==Kind.Facility
        ? Mathf.CeilToInt(Mathf.Max(0,Facility(node)?.requiredStageId ?? 0)/5f)*5 : node.stage;
    public static int Cost(Node node)
    {
        if(node.kind!=Kind.Facility)return node.cost;
        var f=Facility(node);var manager=FacilityManager.Instance;
        return manager==null||f==null ? 0 : Owned(node)?manager.GetUpgradeCost(f):manager.GetUnlockCost(f);
    }
    public static string Blocked(Node node)
    {
        if(node==null)return "不明な解放先";
        if(Owned(node)&&node.kind!=Kind.Facility)return "解放済";
        // Existing unlocks remain usable even when a new tree prerequisite was added.
        if(!Owned(node))
        {
            int cleared=GameManager.Instance?.GetHighestClearedStageId()??0;
            if(cleared<RequiredStage(node))return $"第{RequiredStage(node)}局突破で解禁";
            if(node.parent!=null&&!Owned(Nodes.First(n=>n.id==node.parent)))return Name(Nodes.First(n=>n.id==node.parent))+"の解放が必要";
            if(node.kind==Kind.Slot&&node.target>1&&!ReadSlots().Contains(node.target-1))return "前の編成枠の解放が必要";
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
        if(node.kind==Kind.Character)PlayerInventory.Instance.AddCharacter(Character(node));
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
    public static void ResetProgress(){PlayerProgressStore.Delete(SlotsKey);PlayerProgressStore.Save();Changed?.Invoke();}
}
