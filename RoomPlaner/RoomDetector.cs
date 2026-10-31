using System.Numerics;
using FloorPlan.Domain;
namespace FloorPlan.Application;

/// <summary>Walks bounded faces of a planar wall graph, splitting T junctions and crossings.</summary>
public static class RoomDetector
{
    public static IReadOnlyList<Room> FindClosedRooms(IReadOnlyList<WallSegment> walls)
    {
        const float tolerance = 1;
        var nodes = new List<Vector2>();
        int Node(Vector2 p) { var i = nodes.FindIndex(q => Vector2.Distance(p,q) <= tolerance); if(i >= 0)return i; nodes.Add(p); return nodes.Count-1; }
        var edges = new HashSet<(int,int)>();
        float Cross(Vector2 a, Vector2 b) => a.X*b.Y-a.Y*b.X;
        foreach(var w in walls)
        {
            var delta=w.End-w.Start; var length=delta.LengthSquared(); if(length<1)continue;
            var cuts=new List<float>{0,1};
            foreach(var other in walls)
            {
                foreach(var p in new[]{other.Start,other.End})
                { var t=Vector2.Dot(p-w.Start,delta)/length; if(t>0&&t<1&&GeometryMath.DistanceToSegment(p,w.Start,w.End)<=tolerance)cuts.Add(t); }
                var v=other.End-other.Start; var cross=Cross(delta,v);
                if(Math.Abs(cross)<.001f)continue;
                var t1=Cross(other.Start-w.Start,v)/cross;var t2=Cross(other.Start-w.Start,delta)/cross;
                if(t1>0&&t1<1&&t2>=0&&t2<=1)cuts.Add(t1);
            }
            var sorted=cuts.Order().ToArray();
            for(var i=1;i<sorted.Length;i++)
            {var a=Node(w.Start+delta*sorted[i-1]);var b=Node(w.Start+delta*sorted[i]);if(a!=b){edges.Add((a,b));edges.Add((b,a));}}
        }
        var adjacent=edges.GroupBy(e=>e.Item1).ToDictionary(g=>g.Key,g=>g.Select(e=>e.Item2).OrderBy(i=>MathF.Atan2(nodes[i].Y-nodes[g.Key].Y,nodes[i].X-nodes[g.Key].X)).ToArray());
        var visited=new HashSet<(int,int)>();var result=new List<Room>();
        foreach(var first in edges)
        {
            if(visited.Contains(first))continue;
            var current=first;var boundary=new List<Vector2>();var closed=false;
            for(var step=0;step<=edges.Count;step++)
            {
                if(!visited.Add(current)){closed=current==first;break;}
                boundary.Add(nodes[current.Item1]);var neighbors=adjacent[current.Item2];var reverse=Array.IndexOf(neighbors,current.Item1);
                current=(current.Item2,neighbors[(reverse-1+neighbors.Length)%neighbors.Length]);
            }
            // The unbounded face has negative winding. Ignore zero-area branches.
            if(closed&&boundary.Count>=3&&Room.SignedArea(boundary)>10_000)
                result.Add(new Room(Guid.NewGuid(),boundary,$"Комната {result.Count+1}"));
        }
        return result;
    }
}
