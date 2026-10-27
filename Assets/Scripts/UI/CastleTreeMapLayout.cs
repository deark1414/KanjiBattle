using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class CastleTreeMapLayout
{
    private static Dictionary<string,Vector2> completePositions;
    private static Dictionary<string,Vector2[]> roads;
    private static Vector2[] completeHubs;
    public IReadOnlyDictionary<string,Vector2[]> Roads=>roads;
    public readonly Dictionary<string,Vector2> Positions=new();
    public readonly Vector2[] Hubs=new Vector2[5];
    public static float NodeSize(CastleTree.Node node,float zoom)=>Mathf.Max(46,(node.kind==CastleTree.Kind.Facility?96:76)*zoom);
    public static Vector2 HubSize(int branch)=>new(branch==2?90:62,32);
    public static readonly Vector2 OriginSize=new(96,96);
    public CastleTreeMapLayout(IReadOnlyList<CastleTree.Node> nodes)
    {
        var visibleIds=new HashSet<string>(nodes.Select(n=>n.id));
        if(completePositions!=null)
        {
            foreach(var id in visibleIds)Positions[id]=completePositions[id];
            completeHubs.CopyTo(Hubs,0);return;
        }
        // Reserve the complete tree so revealing a chapter never moves existing nodes.
        nodes=CastleTree.Nodes;
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
        var obstacles=nodes.ToDictionary(n=>n.id,n=>Bounds(Positions[n.id],Vector2.one*NodeSize(n,.5f)/.5f));
        obstacles["origin"]=Bounds(Vector2.zero,OriginSize/.5f);
        for(int b=0;b<5;b++)obstacles["branch_"+b]=Bounds(Hubs[b],HubSize(b)/.5f);
        var router=new RoadRouter(obstacles);roads=new Dictionary<string,Vector2[]>();
        for(int b=0;b<5;b++)roads["branch_"+b]=router.Route(Vector2.zero,Hubs[b],"origin","branch_"+b);
        foreach(var node in nodes)
        {
            string source=node.parent??"branch_"+node.branch;
            Vector2 start=node.parent!=null?Positions[node.parent]:Hubs[node.branch];
            roads[node.id]=router.Route(start,Positions[node.id],source,node.id);
        }
        completePositions=new Dictionary<string,Vector2>(Positions);completeHubs=(Vector2[])Hubs.Clone();
        foreach(var id in Positions.Keys.Where(id=>!visibleIds.Contains(id)).ToArray())Positions.Remove(id);
    }
    private static float Span(CastleTree.Node node,IReadOnlyList<CastleTree.Node> nodes)=>Mathf.Max(1,nodes.Where(n=>n.parent==node.id).Sum(n=>Span(n,nodes)));
    private void Place(CastleTree.Node node,IReadOnlyList<CastleTree.Node> nodes,float left,float total,int depth,float angle)
    {
        float t=(left+Span(node,nodes)*.5f)/total-.5f;
        float theta=angle+t*56*Mathf.Deg2Rad;
        float radius=420+depth*260;
        if(node.parent!=null&&Positions.TryGetValue(node.parent,out var parent))radius=Mathf.Max(radius,parent.magnitude+260);
        var direction=new Vector2(Mathf.Cos(theta),Mathf.Sin(theta));
        // The minimum zoom has the largest hit targets relative to the map.
        var size=Vector2.one*NodeSize(node,.5f)/.5f;
        while(Occupied(direction*radius,size,nodes))radius+=20;
        Positions[node.id]=direction*radius;
        foreach(var child in nodes.Where(n=>n.parent==node.id)){Place(child,nodes,left,total,depth+1,angle);left+=Span(child,nodes);}
    }
    public static bool Overlaps(Vector2 a,Vector2 aSize,Vector2 b,Vector2 bSize,float gap=64)=>
        Mathf.Abs(a.x-b.x)<(aSize.x+bSize.x)*.5f+gap&&Mathf.Abs(a.y-b.y)<(aSize.y+bSize.y)*.5f+gap;
    private bool Occupied(Vector2 point,Vector2 size,IReadOnlyList<CastleTree.Node> nodes)
    {
        if(Overlaps(point,size,Vector2.zero,OriginSize/.5f))return true;
        for(int b=0;b<5;b++)if(Overlaps(point,size,Hubs[b],HubSize(b)/.5f))return true;
        foreach(var other in nodes)
            if(Positions.TryGetValue(other.id,out var position)&&Overlaps(point,size,position,Vector2.one*NodeSize(other,.5f)/.5f))return true;
        return false;
    }
    public static Rect Bounds(Vector2 center,Vector2 size)=>new(center-size*.5f,size);
    public static bool CrossesRect(Vector2 start,Vector2 end,Rect rect)
    {
        float low=0,high=1;
        for(int axis=0;axis<2;axis++)
        {
            float min=rect.min[axis]+.01f,max=rect.max[axis]-.01f,delta=end[axis]-start[axis];
            if(Mathf.Abs(delta)<.0001f){if(start[axis]<=min||start[axis]>=max)return false;continue;}
            float a=(min-start[axis])/delta,b=(max-start[axis])/delta;
            low=Mathf.Max(low,Mathf.Min(a,b));high=Mathf.Min(high,Mathf.Max(a,b));
            if(low>=high)return false;
        }
        return high>0&&low<1;
    }
    private sealed class RoadRouter
    {
        private readonly Dictionary<string,Rect> obstacles;
        private readonly Vector2[] corners;
        private readonly List<(int target,float cost)>[] links;
        public RoadRouter(Dictionary<string,Rect> bounds)
        {
            obstacles=bounds.ToDictionary(p=>p.Key,p=>new Rect(p.Value.min-Vector2.one*24,p.Value.size+Vector2.one*48));
            corners=obstacles.Values.SelectMany(r=>new[]{r.min,new Vector2(r.xMin,r.yMax),r.max,new Vector2(r.xMax,r.yMin)}).ToArray();
            links=corners.Select(_=>new List<(int,float)>()).ToArray();
            for(int a=0;a<corners.Length;a++)for(int b=a+1;b<corners.Length;b++)
                if(Clear(corners[a],corners[b]))
                {float cost=Vector2.Distance(corners[a],corners[b]);links[a].Add((b,cost));links[b].Add((a,cost));}
        }
        private bool Clear(Vector2 a,Vector2 b,string source=null,string target=null)=>
            obstacles.All(p=>p.Key==source||p.Key==target||!CrossesRect(a,b,p.Value));
        public Vector2[] Route(Vector2 start,Vector2 end,string source,string target)
        {
            if(Clear(start,end,source,target))return new[]{start,end};
            int count=corners.Length,best=-1;
            var distance=new float[count];var previous=new int[count];var visited=new bool[count];
            var goal=new float[count];float bestCost=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                distance[i]=Clear(start,corners[i],source,target)?Vector2.Distance(start,corners[i]):float.PositiveInfinity;
                goal[i]=Clear(corners[i],end,source,target)?Vector2.Distance(corners[i],end):float.PositiveInfinity;
                previous[i]=-1;
            }
            for(int step=0;step<count;step++)
            {
                int current=-1;float cost=bestCost;
                for(int i=0;i<count;i++)if(!visited[i]&&distance[i]<cost){current=i;cost=distance[i];}
                if(current<0)break;
                visited[current]=true;
                if(cost+goal[current]<bestCost){bestCost=cost+goal[current];best=current;}
                foreach(var link in links[current])if(cost+link.cost<distance[link.target])
                {distance[link.target]=cost+link.cost;previous[link.target]=current;}
            }
            if(best<0)throw new System.InvalidOperationException("No clear castle tree road: "+source+" -> "+target);
            var path=new List<Vector2>{end};
            for(int i=best;i>=0;i=previous[i])path.Add(corners[i]);
            path.Add(start);path.Reverse();
            var simplified=new List<Vector2>{start};int index=0;
            while(index<path.Count-1)
            {
                int next=path.Count-1;while(next>index+1&&!Clear(path[index],path[next],source,target))next--;
                simplified.Add(path[next]);index=next;
            }
            return simplified.ToArray();
        }
    }
}
