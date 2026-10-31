using System.Numerics;
using FloorPlan.Domain;

namespace FloorPlan.Application;

public sealed class PlanLevel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Этаж 1";
    public float StoreyHeight { get; set; } = 3000;
    public List<WallSegment> Walls { get; } = [];
    public List<Opening> Openings { get; } = [];
    public List<FurnitureItem> Furniture { get; } = [];
    public List<Room> Rooms { get; } = [];
    public Dictionary<string, FloorFinish> Floors { get; } = new(StringComparer.Ordinal);
}

public sealed class PlanDocument
{
    public List<PlanLevel> Levels { get; } = [new()];
    private int activeLevelIndex;
    public int ActiveLevelIndex { get=>activeLevelIndex;set{if(value<0||value>=Levels.Count)throw new ArgumentOutOfRangeException(nameof(value));activeLevelIndex=value;} }
    public PlanLevel ActiveLevel => Levels[activeLevelIndex];
    public List<WallSegment> Walls => ActiveLevel.Walls;
    public List<Opening> Openings => ActiveLevel.Openings;
    public List<FurnitureItem> Furniture => ActiveLevel.Furniture;
    public List<Room> Rooms => ActiveLevel.Rooms;
    public Dictionary<string,FloorFinish> Floors => ActiveLevel.Floors;
    public FloorFinish FloorFor(Room room) => Floors.GetValueOrDefault(InteriorCatalog.RoomKey(room), InteriorCatalog.Floors[0].Finish);
    public void RefreshRooms() { Rooms.Clear(); Rooms.AddRange(RoomDetector.FindClosedRooms(Walls)); }
    public PlanDocument LevelDocument(int index)
    {
        var result=new PlanDocument();result.Levels.Clear();result.Levels.Add(Levels[index]);return result;
    }
}

public interface IPlanCommand { string Description { get; } void Execute(PlanDocument document); void Undo(PlanDocument document); }

public sealed class AddWallCommand(WallSegment wall) : IPlanCommand
{
    public string Description => "Добавить стену";
    public void Execute(PlanDocument d) { d.Walls.Add(wall); d.RefreshRooms(); }
    public void Undo(PlanDocument d) { d.Walls.Remove(wall); d.RefreshRooms(); }
}
public sealed class AddOpeningCommand(Opening opening) : IPlanCommand
{
    public string Description => "Добавить проём";
    public void Execute(PlanDocument d) => d.Openings.Add(opening);
    public void Undo(PlanDocument d) => d.Openings.Remove(opening);
}
public sealed class AddFurnitureCommand(FurnitureItem item) : IPlanCommand
{
    public string Description => "Разместить мебель";
    public void Execute(PlanDocument d) => d.Furniture.Add(item);
    public void Undo(PlanDocument d) => d.Furniture.Remove(item);
}
public sealed class MoveFurnitureCommand(FurnitureItem item, Vector2 before, Vector2 after) : IPlanCommand
{
    public string Description => "Переместить мебель";
    public void Execute(PlanDocument d) => item.Position = after;
    public void Undo(PlanDocument d) => item.Position = before;
}

public interface ILayoutStrategy { string Name { get; } string Description { get; } IReadOnlyList<FurnitureItem> Generate(Room room); }
public sealed class SpaceFirstStrategy : ILayoutStrategy
{
    public string Name => "Простор"; public string Description => "Минимум предметов и широкий проход";
    public IReadOnlyList<FurnitureItem> Generate(Room r) { var (min,max)=r.Bounds; return [Make("Диван",2200,950,FurnitureCategory.Seating,new(min.X+800,min.Y+700)),Make("Стол",1000,700,FurnitureCategory.Work,new(max.X-900,max.Y-700))]; }
    internal static FurnitureItem Make(string n,float w,float d,FurnitureCategory c,Vector2 p) { var f=new FurnitureItem(Guid.NewGuid(),n,w,d,750,c){Position=p}; return f; }
}
public sealed class StorageFirstStrategy : ILayoutStrategy
{
    public string Name => "Хранение"; public string Description => "Дополнительные места хранения вдоль стен";
    public IReadOnlyList<FurnitureItem> Generate(Room r) { var (min,max)=r.Bounds; return [SpaceFirstStrategy.Make("Шкаф",1800,600,FurnitureCategory.Storage,new((min.X+max.X)/2,min.Y+400)),SpaceFirstStrategy.Make("Диван",2000,900,FurnitureCategory.Seating,new(min.X+900,max.Y-800)),SpaceFirstStrategy.Make("Стол",1200,700,FurnitureCategory.Work,new(max.X-800,min.Y+1300))]; }
}
public sealed class BalancedStrategy : ILayoutStrategy
{
    public string Name => "Симметрия"; public string Description => "Сбалансированная парная композиция";
    public IReadOnlyList<FurnitureItem> Generate(Room r) { var (min,max)=r.Bounds; var cx=(min.X+max.X)/2; return [SpaceFirstStrategy.Make("Диван",2100,900,FurnitureCategory.Seating,new(cx,min.Y+900)),SpaceFirstStrategy.Make("Кресло",850,850,FurnitureCategory.Seating,new(cx-1450,min.Y+900)),SpaceFirstStrategy.Make("Кресло",850,850,FurnitureCategory.Seating,new(cx+1450,min.Y+900)),SpaceFirstStrategy.Make("Стол",1100,650,FurnitureCategory.Work,new(cx,max.Y-700))]; }
}
public sealed class LayoutGenerator(IEnumerable<ILayoutStrategy>? strategies = null)
{
    public IReadOnlyList<(string Name,string Description,IReadOnlyList<FurnitureItem> Items)> Generate(Room room) =>
        (strategies ?? [new SpaceFirstStrategy(),new StorageFirstStrategy(),new BalancedStrategy()]).Select(s => (s.Name,s.Description,s.Generate(room))).ToArray();

    public IReadOnlyList<(string Name,string Description,IReadOnlyList<FurnitureItem> Items)> Generate(Room room, RoomKind kind) =>
        Generate(room).Select(plan => (plan.Name, plan.Description, (IReadOnlyList<FurnitureItem>)plan.Items.Select((item,index) => Adapt(item,index,kind)).ToArray())).ToArray();

    private static FurnitureItem Adapt(FurnitureItem item,int index,RoomKind kind)
    {
        var replacement = kind switch
        {
            RoomKind.Bedroom when index == 0 => ("Кровать",1800f,2000f,FurnitureCategory.Sleeping),
            RoomKind.Kitchen when index == 0 => ("Кухонный остров",1600f,900f,FurnitureCategory.Kitchen),
            RoomKind.Office when index == 0 => ("Рабочий стол",1400f,750f,FurnitureCategory.Work),
            _ => (item.Name,item.Width,item.Depth,item.Category)
        };
        return new FurnitureItem(item.Id,replacement.Item1,replacement.Item2,replacement.Item3,item.Height,replacement.Item4)
            { Position=item.Position, RotationDegrees=item.RotationDegrees, Clearance=item.Clearance };
    }
}
