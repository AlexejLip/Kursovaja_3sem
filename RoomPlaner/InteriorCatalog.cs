using System.Globalization;
using System.Numerics;

namespace FloorPlan.Domain;

public enum FloorPattern { Planks, Parquet, Tile, Stone, Concrete }
public sealed record FloorFinish(string Name, string ColorHex, FloorPattern Pattern);
public sealed record ColorOption(string Name, string Hex)
{
    public override string ToString() => Name;
}
public sealed record FurnitureModel(string Id, string Family, string Name, float Width, float Depth, float Height, FurnitureCategory Category, string ColorHex, float CornerRadius = 0)
{
    public FurnitureItem Create(Vector2 position) => new(Guid.NewGuid(), Name, Width, Depth, Height, Category) { CatalogId = Id, ColorHex = ColorHex, Position = position, CornerRadius=CornerRadius };
    public string Dimensions => $"{Width:F0} × {Depth:F0} × {Height:F0} мм";
    public string Symbol => Category switch{FurnitureCategory.Sleeping=>"▰",FurnitureCategory.Storage=>"▥",FurnitureCategory.Seating=>"▣",FurnitureCategory.Decor=>"✿",FurnitureCategory.Bathroom=>"◡",FurnitureCategory.Appliances=>"▤",_=>"▦"};
    public override string ToString() => Name;
}
public sealed record OpeningModel(string Id, string Name, bool IsDoor, float Width, float Height, float Sill = 0)
{
    public override string ToString() => Name;
}
public sealed record FloorOption(string Id, string Name, string ColorHex, FloorPattern Pattern)
{
    public FloorFinish Finish => new(Name, ColorHex, Pattern);
    public override string ToString() => Name;
}

public static class InteriorCatalog
{
    public static readonly ColorOption[] Colors = [new("Шалфей", "#82A899"), new("Песочный", "#D3BA97"), new("Терракота", "#BB7862"), new("Графит", "#535F6C"), new("Глубокий синий", "#56779A"), new("Молочный", "#E5DFD2"), new("Горчица", "#C7A34F"), new("Орех", "#87634E"), new("Лаванда", "#AAA0BE"), new("Оливковый", "#8D986A")];
    public static readonly FloorOption[] Floors = [new("oak", "Дуб натуральный", "#D9BD94", FloorPattern.Planks), new("oak-light", "Дуб выбеленный", "#E7DDC9", FloorPattern.Planks), new("walnut", "Орех тёмный", "#9E7B5E", FloorPattern.Planks), new("parquet", "Паркет · ёлочка", "#C7A87F", FloorPattern.Parquet), new("tile", "Керамогранит", "#D7D6D0", FloorPattern.Tile), new("marble", "Светлый камень", "#E4E2DC", FloorPattern.Stone), new("concrete", "Микроцемент", "#BFC6C1", FloorPattern.Concrete)];
    public static readonly OpeningModel[] Doors = [new("hinged", "Распашная", true, 900, 2100), new("double-door", "Двухстворчатая", true, 1500, 2200), new("sliding", "Раздвижная", true, 1600, 2200), new("arch", "Открытый портал", true, 1200, 2400)];
    public static readonly OpeningModel[] Windows = [new("single", "Одностворчатое", false, 800, 1400, 900), new("double-window", "Двухстворчатое", false, 1400, 1400, 900), new("triple", "Трёхстворчатое", false, 2100, 1500, 800), new("panoramic", "Панорамное", false, 2600, 2300, 100)];
    public static readonly FurnitureModel[] Furniture =
    [
        new("sofa-2", "Диван", "Диван · двухместный", 1700, 900, 850, FurnitureCategory.Seating, "#82A899"),
        new("sofa-3", "Диван", "Диван · трёхместный", 2300, 950, 850, FurnitureCategory.Seating, "#82A899"),
        new("sofa-corner", "Диван", "Диван · угловой", 2700, 1700, 850, FurnitureCategory.Seating, "#56779A"),
        new("sofa-modular", "Диван", "Диван · модульный", 3000, 1100, 780, FurnitureCategory.Seating, "#D3BA97"),
        new("bed-single", "Кровать", "Кровать · односпальная", 1000, 2050, 850, FurnitureCategory.Sleeping, "#E5DFD2"),
        new("bed-double", "Кровать", "Кровать · двуспальная", 1800, 2100, 950, FurnitureCategory.Sleeping, "#AAA0BE"),
        new("bed-king", "Кровать", "Кровать · king size", 2100, 2200, 1050, FurnitureCategory.Sleeping, "#D3BA97"),
        new("wardrobe", "Шкаф", "Шкаф · распашной", 1600, 600, 2200, FurnitureCategory.Storage, "#D3BA97"),
        new("wardrobe-slide", "Шкаф", "Шкаф · купе", 2400, 650, 2400, FurnitureCategory.Storage, "#E5DFD2"),
        new("shelf", "Шкаф", "Стеллаж · открытый", 1200, 350, 1800, FurnitureCategory.Storage, "#87634E"),
        new("desk", "Стол", "Стол · письменный", 1400, 700, 750, FurnitureCategory.Work, "#D3BA97"),
        new("desk-corner", "Стол", "Стол · угловой", 1600, 1200, 750, FurnitureCategory.Work, "#87634E"),
        new("desk-compact", "Стол", "Стол · компактный", 1000, 600, 750, FurnitureCategory.Work, "#E5DFD2"),
        new("armchair", "Кресло", "Кресло · мягкое", 850, 850, 850, FurnitureCategory.Seating, "#BB7862"),
        new("chair-office", "Кресло", "Кресло · рабочее", 650, 650, 1100, FurnitureCategory.Seating, "#535F6C"),
        new("pouf", "Кресло", "Пуф · круглый", 650, 650, 450, FurnitureCategory.Seating, "#C7A34F"),
        new("kitchen-line", "Кухня", "Кухня · линейная", 3000, 600, 900, FurnitureCategory.Kitchen, "#E5DFD2"),
        new("kitchen-corner", "Кухня", "Кухня · угловая", 2600, 1800, 900, FurnitureCategory.Kitchen, "#82A899"),
        new("island", "Кухонный остров", "Остров · стандартный", 1600, 900, 900, FurnitureCategory.Kitchen, "#87634E"),
        new("island-bar", "Кухонный остров", "Остров · барная стойка", 1800, 700, 1100, FurnitureCategory.Kitchen, "#535F6C"),
        new("dining", "Обеденный стол", "Стол · на 4 персоны", 1400, 850, 750, FurnitureCategory.Dining, "#D3BA97"),
        new("dining-large", "Обеденный стол", "Стол · на 6 персон", 1900, 950, 750, FurnitureCategory.Dining, "#87634E"),
        new("dining-round", "Обеденный стол", "Стол · круглый", 1100, 1100, 750, FurnitureCategory.Dining, "#E5DFD2"),
        new("nightstand", "Тумба", "Тумба · прикроватная", 500, 450, 500, FurnitureCategory.Storage, "#87634E"),
        new("tv-unit", "Тумба", "Тумба · под телевизор", 1800, 400, 550, FurnitureCategory.Storage, "#D3BA97"),
        new("coffee", "Журнальный стол", "Столик · прямоугольный", 1000, 550, 420, FurnitureCategory.Other, "#87634E"),
        new("coffee-round", "Журнальный стол", "Столик · круглый", 700, 700, 420, FurnitureCategory.Other, "#D3BA97"),
        new("sofa-soft", "Диван", "Диван · облако", 2400, 1050, 800, FurnitureCategory.Seating, "#E5DFD2", 180),
        new("chaise", "Кресло", "Кушетка · шезлонг", 900, 1700, 800, FurnitureCategory.Seating, "#82A899", 100),
        new("bench", "Кресло", "Банкетка · мягкая", 1300, 450, 450, FurnitureCategory.Seating, "#BB7862", 100),
        new("bed-queen", "Кровать", "Кровать · queen", 1600, 2100, 950, FurnitureCategory.Sleeping, "#D3BA97", 60),
        new("bed-child", "Кровать", "Кровать · детская", 900, 1800, 750, FurnitureCategory.Sleeping, "#AAA0BE", 70),
        new("dresser", "Тумба", "Комод · 4 ящика", 1400, 500, 850, FurnitureCategory.Storage, "#D3BA97", 40),
        new("console", "Тумба", "Консоль · прихожая", 1100, 350, 800, FurnitureCategory.Storage, "#87634E", 40),
        new("shelf-narrow", "Шкаф", "Стеллаж · узкий", 600, 350, 2000, FurnitureCategory.Storage, "#E5DFD2"),
        new("dining-oval", "Обеденный стол", "Стол · овальный", 1800, 950, 750, FurnitureCategory.Dining, "#D3BA97", 475),
        new("coffee-soft", "Журнальный стол", "Столик · скруглённый", 1100, 600, 400, FurnitureCategory.Other, "#87634E", 220),
        new("dining-chair", "Стул", "Стул · обеденный", 480, 520, 850, FurnitureCategory.Seating, "#D3BA97", 40),
        new("bar-stool", "Стул", "Стул · барный", 450, 450, 1050, FurnitureCategory.Seating, "#535F6C", 150),
        new("bathtub", "Санузел", "Ванна · отдельностоящая", 1700, 750, 600, FurnitureCategory.Bathroom, "#E5DFD2", 330),
        new("shower", "Санузел", "Душ · угловой", 900, 900, 2100, FurnitureCategory.Bathroom, "#A6C8CE", 40),
        new("toilet", "Санузел", "Унитаз · подвесной", 400, 700, 800, FurnitureCategory.Bathroom, "#E5DFD2", 190),
        new("vanity", "Санузел", "Раковина · с тумбой", 800, 500, 850, FurnitureCategory.Bathroom, "#82A899", 60),
        new("fridge", "Техника", "Холодильник · двухкамерный", 650, 700, 1950, FurnitureCategory.Appliances, "#B5C2C6", 35),
        new("washer", "Техника", "Стиральная машина", 600, 600, 850, FurnitureCategory.Appliances, "#E5DFD2", 35),
        new("oven", "Техника", "Духовой шкаф", 600, 600, 850, FurnitureCategory.Appliances, "#535F6C", 25),
        new("plant", "Декор", "Растение · напольное", 600, 600, 1400, FurnitureCategory.Decor, "#71966A", 280),
        new("lamp", "Декор", "Торшер · напольный", 450, 450, 1650, FurnitureCategory.Decor, "#D3BA97", 220),
        new("rug", "Декор", "Ковёр · прямоугольный", 2400, 1600, 15, FurnitureCategory.Decor, "#AAA0BE", 80),
        new("rug-round", "Декор", "Ковёр · круглый", 1800, 1800, 15, FurnitureCategory.Decor, "#D3BA97", 900)
    ];
    public static FurnitureModel Model(string id) => Furniture.First(f => f.Id == id);
    public static bool IsColor(string? value) => value is { Length: 7 } && value[0] == '#' && value.Skip(1).All(Uri.IsHexDigit);
    public static string RoomKey(Room room) => string.Join(";", room.Boundary.Select(p => $"{p.X.ToString("0.0", CultureInfo.InvariantCulture)},{p.Y.ToString("0.0", CultureInfo.InvariantCulture)}").Order(StringComparer.Ordinal));
}
