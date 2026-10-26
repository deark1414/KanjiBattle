using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class CastleTreeMapLayout
{
    public readonly Dictionary<string,Vector2> Positions=new();
    public readonly Vector2[] Hubs=new Vector2[5];
    public CastleTreeMapLayout(IReadOnlyList<CastleTree.Node> nodes)
    {
        for(int branch=0;branch<5;branch++)
        {
            float angle=(90-branch*72)*Mathf.Deg2Rad;
            Hubs[branch]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*125;
            var roots=nodes.Where(n=>n.branch==branch&&(n.parent==null||!nodes.Any(p=>p.id==n.parent))).ToList();
            float total=Mathf.Max(1,roots.Sum(n=>Span(n,nodes))),left=0;
            foreach(var root in roots){Place(root,nodes,left,total,0,angle);left+=Span(root,nodes);}
        }
    }
    private static float Span(CastleTree.Node node,IReadOnlyList<CastleTree.Node> nodes)=>Mathf.Max(1,nodes.Where(n=>n.parent==node.id).Sum(n=>Span(n,nodes)));
    private void Place(CastleTree.Node node,IReadOnlyList<CastleTree.Node> nodes,float left,float total,int depth,float angle)
    {
        float t=(left+Span(node,nodes)*.5f)/total-.5f;
        float theta=angle+t*56*Mathf.Deg2Rad;
        float radius=Mathf.Max(235,total*100)+depth*145;
        Positions[node.id]=new Vector2(Mathf.Cos(theta),Mathf.Sin(theta))*radius;
        foreach(var child in nodes.Where(n=>n.parent==node.id)){Place(child,nodes,left,total,depth+1,angle);left+=Span(child,nodes);}
    }
}
