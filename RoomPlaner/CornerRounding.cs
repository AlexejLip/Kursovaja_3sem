using System.Numerics;
using FloorPlan.Domain;
namespace FloorPlan.Application;

public sealed record CornerEdit(IReadOnlyList<WallSegment> Walls,IReadOnlyList<Opening> Openings);

public static class CornerRounding
{
    /// <summary>Build a tangent circular fillet without mutating the document on failure.</summary>
    public static CornerEdit? Prepare(PlanDocument document,Guid wallId,bool atEnd,float radius,out string message)
    {
        message="";
        var wall=document.Walls.Find(w=>w.Id==wallId);
        if(wall is null){message="Выберите стену.";return null;}
        if(!float.IsFinite(radius)||radius<50||radius>10000){message="Радиус должен быть от 50 до 10 000 мм.";return null;}
        var corner=atEnd?wall.End:wall.Start;
        var joined=document.Walls.Where(w=>w.Id!=wall.Id&&(Near(w.Start,corner)||Near(w.End,corner))).ToArray();
        if(joined.Length!=1){message="Для скругления в вершине должны соединяться ровно две стены.";return null;}
        var other=joined[0];
        if(wall.Material!=other.Material||Math.Abs(wall.Thickness-other.Thickness)>1||Math.Abs(wall.Height-other.Height)>1){message="У соединённых стен должны совпадать толщина, высота и материал.";return null;}
        var a=atEnd?wall.Start:wall.End;var b=Near(other.Start,corner)?other.End:other.Start;
        var u=Vector2.Normalize(a-corner);var v=Vector2.Normalize(b-corner);
        var angle=MathF.Acos(Math.Clamp(Vector2.Dot(u,v),-1,1));
        if(angle<.12f||MathF.PI-angle<.12f){message="Выберите выраженный угол между стенами.";return null;}
        var tangent=radius/MathF.Tan(angle/2);
        if(tangent>Math.Min(wall.Length,other.Length)-100){message="Радиус слишком большой для этих стен.";return null;}
        var p=corner+u*tangent;var q=corner+v*tangent;
        var center=corner+Vector2.Normalize(u+v)*(radius/MathF.Sin(angle/2));
        var start=MathF.Atan2(p.Y-center.Y,p.X-center.X);var from=p-center;var to=q-center;
        var sweep=MathF.Atan2(from.X*to.Y-from.Y*to.X,Vector2.Dot(from,to));
        var segments=Math.Clamp((int)MathF.Ceiling(MathF.Abs(sweep)/.12f),3,32);
        var newWalls=document.Walls.Select(w=>w.Id==wall.Id?(atEnd?w with{End=p}:w with{Start=p}):w.Id==other.Id?(Near(other.Start,corner)?w with{Start=q}:w with{End=q}):w).ToList();
        var openings=new List<Opening>();
        foreach(var o in document.Openings)
        {
            var original=document.Walls.First(w=>w.Id==o.WallId);var changed=newWalls.First(w=>w.Id==o.WallId);
            var removed=Vector2.Distance(original.Start,changed.Start);var offset=o.Offset-removed;
            if(offset<-.1f||offset+o.Width>changed.Length+.1f){message="Скругление затрагивает дверь или окно. Уменьшите радиус или перенесите проём.";return null;}
            openings.Add(o is Door door?door with{Offset=Math.Max(0,offset)}:((Window)o) with{Offset=Math.Max(0,offset)});
        }
        var previous=p;
        for(var i=1;i<=segments;i++)
        {
            var t=start+sweep*i/segments;var next=i==segments?q:center+new Vector2(MathF.Cos(t),MathF.Sin(t))*radius;
            // Reject arcs that would cross unrelated walls, preserving planar room topology.
            foreach(var unrelated in document.Walls.Where(w=>w.Id!=wall.Id&&w.Id!=other.Id))
                if(Crosses(previous,next,unrelated.Start,unrelated.End)){message="Скругление пересекает другую стену. Уменьшите радиус.";return null;}
            newWalls.Add(new(Guid.NewGuid(),previous,next,wall.Thickness,wall.Height,wall.Material));previous=next;
        }
        if(newWalls.Count>2000){message="Достигнут предел количества стен.";return null;}
        message=$"Угол скруглён. Радиус {radius:F0} мм. Ctrl+Z — отменить.";
        return new(newWalls,openings);
    }
    private static bool Near(Vector2 a,Vector2 b)=>Vector2.DistanceSquared(a,b)<1;
    private static bool Crosses(Vector2 a,Vector2 b,Vector2 c,Vector2 d)
    {
        static float Cross(Vector2 u,Vector2 v)=>u.X*v.Y-u.Y*v.X;
        var cross=Cross(b-a,d-c);if(Math.Abs(cross)<.001f)return false;
        var t=Cross(c-a,d-c)/cross;var s=Cross(c-a,b-a)/cross;return t>=0&&t<=1&&s>=0&&s<=1;
    }
}
