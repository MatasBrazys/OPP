// ./GameShared/Map/Tiles.cs
namespace GameShared.Types.Map;

public class GrassTile : TileData
{
    public int tileId = 0;
    public override string TileType => "Grass";
    public override bool Passable => true;
    public GrassTile(int x, int y) : base(x, y) { }
}

public class TreeTile : TileData
{
    public int tileId = 1;
    public override string TileType => "Tree";
    public override bool Passable => false;

    public DateTime LastHarvest { get; private set; } = DateTime.MinValue;

    public TreeTile(int x, int y) : base(x, y) { }

    // server-side: check if can be harvested
    public bool CanHarvest() => (DateTime.UtcNow - LastHarvest).TotalMinutes > 1;

    public void Harvest()
    {
        LastHarvest = DateTime.UtcNow;
    }
}

public class HouseTile : TileData
{
    public int tileId = 2;
    public override string TileType => "House";
    public override bool Passable => true;

    public List<string> AvailableQuests { get; init; } = new();

    public HouseTile(int x, int y) : base(x, y) { }
}

public class AppleTile : TileData
{
    public int tileId = 3;
    public override string TileType => "Apple";
    public override bool Passable => true;

    public AppleTile(int x, int y) : base(x, y) { }
}

public class FishTile : TileData
{
    public int tileId = 4;
    public override string TileType => "Fish";
    public override bool Passable => true;

    public FishTile(int x, int y) : base(x, y) { }
}

public class WaterTile : TileData
{
    public int tileId = 5;
    public override string TileType => "Water";
    public override bool Passable => false;

    public WaterTile(int x, int y) : base(x, y) { }
}

public class SandTile : TileData
{
    public int tileId = 6;
    public override string TileType => "Sand";
    public override bool Passable => true;

    public SandTile(int x, int y) : base(x, y) { }
}
