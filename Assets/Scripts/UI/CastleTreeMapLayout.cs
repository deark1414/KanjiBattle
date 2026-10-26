using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class CastleTreeMapLayout
{
    public readonly Dictionary<string,Vector2> Positions=new();
    public readonly Vector2[] Hubs=new Vector2[5];
    public static float NodeSize(CastleTree.Node node,float zoom)=>Mathf.Max(46,(node.kind==CastleTree.Kind.Facility?96:76)*zoom);
    public static Vector2 HubSize(int branch)=>new(branch==2?90:62,32);
    public static readonly Vector2 OriginSize=new(96,96);
    public CastleTreeMapLayout(IReadOnlyList<CastleTree.Node> nodes)
    {
        for(int branch=0;branch<5;branch++)
        {
            float angle=(90-branch*72)*Mathf.Deg2Rad;
            Hubs[branch]=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*260;
        }
        for(int branch=0;branch<5;branch++)
        {
            float angle=(90-branch*72)*Mathf.Deg2Rad;
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
        float radius=420+depth*220;
        if(node.parent!=null&&Positions.TryGetValue(node.parent,out var parent))radius=Mathf.Max(radius,parent.magnitude+220);
        var direction=new Vector2(Mathf.Cos(theta),Mathf.Sin(theta));
        // The minimum zoom has the largest hit targets relative to the map.
        var size=Vector2.one*NodeSize(node,.5f)/.5f;
        while(Occupied(direction*radius,size,nodes))radius+=20;
        Positions[node.id]=direction*radius;
        foreach(var child in nodes.Where(n=>n.parent==node.id)){Place(child,nodes,left,total,depth+1,angle);left+=Span(child,nodes);}
    }
    public static bool Overlaps(Vector2 a,Vector2 aSize,Vector2 b,Vector2 bSize,float gap=48)=>
        Mathf.Abs(a.x-b.x)<(aSize.x+bSize.x)*.5f+gap&&Mathf.Abs(a.y-b.y)<(aSize.y+bSize.y)*.5f+gap;
    private bool Occupied(Vector2 point,Vector2 size,IReadOnlyList<CastleTree.Node> nodes)
    {
        if(Overlaps(point,size,Vector2.zero,OriginSize/.5f))return true;
        for(int b=0;b<5;b++)if(Overlaps(point,size,Hubs[b],HubSize(b)/.5f))return true;
        foreach(var other in nodes)
            if(Positions.TryGetValue(other.id,out var position)&&Overlaps(point,size,position,Vector2.one*NodeSize(other,.5f)/.5f))return true;
        return false;
    }
}
