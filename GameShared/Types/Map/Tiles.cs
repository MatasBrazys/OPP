// ./GameShared/Map/Tiles.cs
using GameShared.Strategies;
using GameShared.Types.Players;

namespace GameShared.Types.Map;

public class GrassTile : TileData
{
    public int tileId = 0;
    public override string TileType => "Grass";
    public override bool Passable => true;
    public override bool Plantable => true;
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
    private static readonly AppleBoostMovement BoostStrategy = new();
    protected override IMovementStrategy MovementStrategy => BoostStrategy;

    public AppleTile(int x, int y) : base(x, y) { }
}


public class FishTile : TileData
{
    public int tileId = 4;
    public override string TileType => "Fish";
    public override bool Passable => true;
    private static readonly FishSwimMovement SwimStrategy = new();
    protected override IMovementStrategy MovementStrategy => SwimStrategy;

    public FishTile(int x, int y) : base(x, y) { }
}

public class WaterTile : TileData
{
    public int tileId = 5;
    public override string TileType => "Water";
    public override bool Passable => false;
    private static readonly FishSwimMovement SwimStrategy = new();
    protected override IMovementStrategy MovementStrategy => SwimStrategy;

    public WaterTile(int x, int y) : base(x, y) { }
}

public class SandTile : TileData
{
    public int tileId = 6;
    public override string TileType => "Sand";
    public override bool Passable => true;
    private static readonly SandMovement SandStrategy = new();
    protected override IMovementStrategy MovementStrategy => SandStrategy;

    public SandTile(int x, int y) : base(x, y) { }
}

public class CherryTile : TileData
{
    public int tileId = 7;
    public override string TileType => "Cherry";
    public override bool Passable =>    true;
    public CherryTile(int x, int y): base(x, y) { }
    public bool IsEaten { get; private set; }
    public void Eat()
    {
        IsEaten = true;
    }
    public bool CanBeEaten()
    {
        return !IsEaten;
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        var result = base.OnEnter(player);

        if (CanBeEaten())
        {
            Eat();
            result = result.WithSpawnClone();
            result = result.WithReplaceGrass();
        }

        return result;
    }
   
}
public class WheatTile : TileData
{
    public int tileId = 8;
    public override string TileType => "Wheat";
    public override bool Passable => true;
    public WheatTile(int x, int y) : base(x, y) { }
}

public class WheatPlantTile : TileData
{
    public int tileId = 9;
    public override string TileType => "WheatPlant";
    public override bool Passable => true;
    public WheatPlantTile(int x, int y) : base(x, y) { }
}

public class CarrotTile : TileData
{
    public int tileId = 10;
    public override string TileType => "Carrot";
    public override bool Passable => true;
    public CarrotTile(int x, int y) : base(x, y) { }
}

public class CarrotPlantTile : TileData
{
    public int tileId = 11;
    public override string TileType => "CarrotPlant";
    public override bool Passable => true;
    public CarrotPlantTile(int x, int y) : base(x, y) { }
}

public class PotatoTile : TileData
{
    public int tileId = 12;
    public override string TileType => "Potato";
    public override bool Passable => true;
    public PotatoTile(int x, int y) : base(x, y) { }
}

public class PotatoPlantTile : TileData
{
    public int tileId = 13;
    public override string TileType => "PotatoPlant";
    public override bool Passable => true;
    public PotatoPlantTile(int x, int y) : base(x, y) { }
}