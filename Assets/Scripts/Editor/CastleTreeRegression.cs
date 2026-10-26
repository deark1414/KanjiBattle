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
        Check(CastleTreeArt.Castle()!=null,"central castle art resolves");
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
        var completeMap=new CastleTreeMapLayout(nodes);
        var roadBounds=nodes.ToDictionary(n=>n.id,n=>CastleTreeMapLayout.Bounds(completeMap.Positions[n.id],Vector2.one*CastleTreeMapLayout.NodeSize(n,.5f)/.5f+Vector2.one*32));
        roadBounds["origin"]=CastleTreeMapLayout.Bounds(Vector2.zero,CastleTreeMapLayout.OriginSize/.5f+Vector2.one*32);
        for(int branch=0;branch<5;branch++)roadBounds["branch_"+branch]=CastleTreeMapLayout.Bounds(completeMap.Hubs[branch],CastleTreeMapLayout.HubSize(branch)/.5f+Vector2.one*32);
        foreach(var road in completeMap.Roads)
        {
            string source=road.Key.StartsWith("branch_")?"origin":nodes.Single(n=>n.id==road.Key).parent??"branch_"+nodes.Single(n=>n.id==road.Key).branch;
            for(int segment=1;segment<road.Value.Length;segment++)foreach(var obstacle in roadBounds)
                if(obstacle.Key!=source&&obstacle.Key!=road.Key)
                    Check(!CastleTreeMapLayout.CrossesRect(road.Value[segment-1],road.Value[segment],obstacle.Value),$"road {road.Key} crosses {obstacle.Key}");
        }
        foreach(int stage in Enumerable.Range(0,9).Select(i=>i*5))
        {
            var visible=nodes.Where(n=>n.stage<=stage).ToArray();
            var map=new CastleTreeMapLayout(visible);
            Check(map.Positions.Count==visible.Length,"all nodes placed on radial map");
            foreach(var node in visible)Check(map.Positions[node.id]==completeMap.Positions[node.id],"chapter reveals keep existing positions stable");
            foreach(float zoom in new[]{.5f,.75f,1f,1.25f})
            {
                var bounds=new System.Collections.Generic.List<(string name,Vector2 point,Vector2 size)>();
                bounds.Add(("origin",Vector2.zero,CastleTreeMapLayout.OriginSize));
                foreach(int branch in Enumerable.Range(0,5).Where(b=>visible.Any(n=>n.branch==b)))
                    bounds.Add(("branch "+branch,map.Hubs[branch]*zoom,CastleTreeMapLayout.HubSize(branch)));
                foreach(var node in visible)
                    bounds.Add((node.id,map.Positions[node.id]*zoom,Vector2.one*CastleTreeMapLayout.NodeSize(node,zoom)));
                for(int a=0;a<bounds.Count;a++)for(int b=a+1;b<bounds.Count;b++)
                    Check(!CastleTreeMapLayout.Overlaps(bounds[a].point,bounds[a].size,bounds[b].point,bounds[b].size,31.9f),
                        $"stage {stage}, zoom {zoom}: {bounds[a].name} overlaps {bounds[b].name}");
            }
        }
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
