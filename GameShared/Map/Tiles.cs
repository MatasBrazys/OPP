// ./GameShared/Map/Tiles.cs
namespace GameShared.Map;

public record GrassTile(int X, int Y) : TileData(X, Y)
{
    public int id = 0;
    public override string TileType => "Grass";
    public override bool Passable => true;
}

public record TreeTile(int X, int Y) : TileData(X, Y)
{
    public int id = 1;
    public override string TileType => "Tree";
    public override bool Passable => false;

    public DateTime LastHarvest { get; private set; } = DateTime.MinValue;

    // server-side: check if can be harvested
    public bool CanHarvest() => (DateTime.UtcNow - LastHarvest).TotalMinutes > 1;

    public void Harvest()
    {
        LastHarvest = DateTime.UtcNow;
    }
}

public record HouseTile(int X, int Y) : TileData(X, Y)
{
    public int id = 2;
    public override string TileType => "House";
    public override bool Passable => true;

    // server-side: quests, interactions
    public List<string> AvailableQuests { get; init; } = new();
}

    
