using System.Numerics;

namespace FloorPlan.Domain;

public enum WallMaterial { Plaster, Concrete, Brick, Glass }
public enum FurnitureCategory { Seating, Sleeping, Storage, Work, Dining, Kitchen, Other, Bathroom, Appliances, Decor }
public enum RoomKind { LivingRoom, Bedroom, Kitchen, Office }

public sealed record WallSegment(Guid Id, Vector2 Start, Vector2 End, float Thickness = 120, float Height = 2700, WallMaterial Material = WallMaterial.Plaster)
{
    public float Length => Vector2.Distance(Start, End);
    public Vector2 Direction => Vector2.Normalize(End - Start);
}

public sealed class Room(Guid id, IReadOnlyList<Vector2> boundary, string name = "Комната")
{
    public Guid Id { get; } = id;
    public string Name { get; set; } = name;
    public IReadOnlyList<Vector2> Boundary { get; } = boundary;
    public double AreaSquareMeters => Math.Abs(SignedArea(Boundary)) / 1_000_000d;
    public double PerimeterMeters => Boundary.Select((p, i) => Vector2.Distance(p, Boundary[(i + 1) % Boundary.Count])).Sum() / 1000d;
    public (Vector2 Min, Vector2 Max) Bounds
    {
        get
        {
            if (Boundary.Count == 0) return (Vector2.Zero, Vector2.Zero);
            return (new(Boundary.Min(p => p.X), Boundary.Min(p => p.Y)), new(Boundary.Max(p => p.X), Boundary.Max(p => p.Y)));
        }
    }
    public static double SignedArea(IReadOnlyList<Vector2> points)
    {
        double area = 0;
        for (var i = 0; i < points.Count; i++) { var a = points[i]; var b = points[(i + 1) % points.Count]; area += a.X * b.Y - b.X * a.Y; }
        return area / 2;
    }
}

public abstract record Opening(Guid Id, Guid WallId, float Offset, float Width, float Height, float SillHeight = 0)
{
    public string StyleId { get; init; } = "standard";
    public string ColorHex { get; init; } = "#C3A17B";
}
public sealed record Door(Guid Id, Guid WallId, float Offset, float Width = 900, float Height = 2100, bool OpensInward = true) : Opening(Id, WallId, Offset, Width, Height);
public sealed record Window(Guid Id, Guid WallId, float Offset, float Width = 1200, float Height = 1400, float SillHeight = 900) : Opening(Id, WallId, Offset, Width, Height, SillHeight);

public sealed class FurnitureItem(Guid id, string name, float width, float depth, float height, FurnitureCategory category)
{
    public Guid Id { get; } = id;
    public string Name { get; set; } = name;
    public float Width { get; set; } = width;
    public float Depth { get; set; } = depth;
    public float Height { get; set; } = height;
    public float RotationDegrees { get; set; }
    public Vector2 Position { get; set; }
    public FurnitureCategory Category { get; } = category;
    public float Clearance { get; set; } = 700;
    public string CatalogId { get; set; } = "generic";
    public string ColorHex { get; set; } = "#82A899";
    public float CornerRadius { get; set; }
    public IReadOnlyList<Vector2> Footprint
    {
        get
        {
            var radius=Math.Clamp(CornerRadius,0,Math.Min(Width,Depth)/2);
            if(radius<1)return Corners;
            var points=new List<Vector2>();var a=RotationDegrees*MathF.PI/180;var cs=MathF.Cos(a);var sn=MathF.Sin(a);
            for(var corner=0;corner<4;corner++)
            {
                var center=corner switch{0=>new Vector2(Width/2-radius,Depth/2-radius),1=>new Vector2(-Width/2+radius,Depth/2-radius),2=>new Vector2(-Width/2+radius,-Depth/2+radius),_=>new Vector2(Width/2-radius,-Depth/2+radius)};
                for(var i=0;i<=5;i++){var t=(corner*90+i*18)*MathF.PI/180;var p=center+new Vector2(MathF.Cos(t),MathF.Sin(t))*radius;points.Add(Position+new Vector2(p.X*cs-p.Y*sn,p.X*sn+p.Y*cs));}
            }
            return points;
        }
    }
    public IReadOnlyList<Vector2> Corners
    {
        get
        {
            var r = RotationDegrees * MathF.PI / 180; var c = MathF.Cos(r); var s = MathF.Sin(r);
            return new[] { new Vector2(-Width/2,-Depth/2), new Vector2(Width/2,-Depth/2), new Vector2(Width/2,Depth/2), new Vector2(-Width/2,Depth/2) }
                .Select(p => Position + new Vector2(p.X*c-p.Y*s, p.X*s+p.Y*c)).ToArray();
        }
    }
}

public static class GeometryMath
{
    public static bool ContainsPoint(Vector2 p, IReadOnlyList<Vector2> polygon)
    {
        var inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            if (DistanceToSegment(p, polygon[j], polygon[i]) < .01f) return true;
            if ((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y) && p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X) inside = !inside;
        }
        return inside;
    }
    public static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        var d = b - a; var len = d.LengthSquared(); if (len < 0.001f) return Vector2.Distance(p, a);
        var t = Math.Clamp(Vector2.Dot(p-a, d) / len, 0, 1); return Vector2.Distance(p, a + t*d);
    }
    public static bool PolygonsIntersect(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b)
    {
        if (a.Count < 3 || b.Count < 3) return false;
        foreach (var polygon in new[] { a, b })
            for (var i = 0; i < polygon.Count; i++)
            {
                var edge = polygon[(i+1)%polygon.Count] - polygon[i]; if (edge.LengthSquared() < .0001f) continue; var axis = Vector2.Normalize(new(-edge.Y, edge.X));
                var pa = a.Select(p => Vector2.Dot(p, axis)); var pb = b.Select(p => Vector2.Dot(p, axis));
                if (pa.Max() < pb.Min() || pb.Max() < pa.Min()) return false;
            }
        return true;
    }
}
