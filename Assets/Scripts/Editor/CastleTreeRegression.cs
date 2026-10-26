using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CastleTreeRegression
{
    public static void Run()
    {
        void Check(bool value,string message){if(!value)throw new Exception("Castle tree: "+message);}
        var nodes=CastleTree.Nodes;
        Check(nodes.Select(n=>n.id).Distinct().Count()==nodes.Count,"unique nodes");
        Check(nodes.Count(n=>n.kind==CastleTree.Kind.Character)==26,"26 normal characters");
        Check(nodes.Where(n=>n.kind==CastleTree.Kind.Slot).Select(n=>n.stage).SequenceEqual(new[]{5,10,20,35,30}),"five distributed chapter gates");
        foreach(var node in nodes)
        {
            Check(node.cost>=0,"nonnegative costs");
            var path=new System.Collections.Generic.HashSet<string>{node.id};var current=node;
            while(current.parent!=null)
            {
                current=nodes.Single(n=>n.id==current.parent);Check(path.Add(current.id),"acyclic prerequisites");
            }
        }
        var map=new CastleTreeMapLayout(nodes);
        Check(map.Positions.Count==nodes.Count,"all nodes placed on radial map");
        for(int a=0;a<nodes.Count;a++)for(int b=a+1;b<nodes.Count;b++)
            Check(Vector2.Distance(map.Positions[nodes[a].id],map.Positions[nodes[b].id])>=76,"map nodes do not overlap");
        foreach(var node in nodes.Where(n=>n.kind==CastleTree.Kind.Facility))
            Check(CastleTreeArt.Facility(CastleTree.Facility(node))!=null,"existing building art resolves");
        var stages=AssetDatabase.LoadAssetAtPath<StageDatabase>("Assets/ScriptableObjects/Stages/StageDatabase.asset");
        var rows=stages.stages.OrderBy(s=>s.stageId).ToArray();
        Check(rows.Length==40,"40 campaign stages");
        foreach(var stage in rows)
        {
            Check(!stage.restrictFormationSlots && stage.slotCount==8,"normal stages do not cap owned slots");
            Check(CampaignFormationRules.EnemyPositions(stage).Count==stage.enemyPool.Count,"enemy formation valid");
            Check(stage.enemyPool.Count<=10,"enemy count cap");
        }
        for(int i=1;i<rows.Length;i++)
        {
            Check(rows[i].enemyLevel-rows[i-1].enemyLevel<=1,"no level spike");
            Check(rows[i].enemyPool.Count-rows[i-1].enemyPool.Count<=1,"no headcount spike");
        }
        foreach(var group in rows.GroupBy(s=>s.chapterId))
            Check(group.Select(s=>string.Join(",",s.enemyPool.Select(c=>c.characterId))).Distinct().Count()==5,"varied chapter compositions");
        Debug.Log("[CastleTreeRegression] Passed.");
    }
}
