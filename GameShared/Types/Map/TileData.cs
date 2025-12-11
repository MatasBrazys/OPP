// .GameShared/Map/TileData.cs
using GameShared.Strategies;
using GameShared.Types;
using GameShared.Types.Players;

namespace GameShared.Types.Map;

public abstract class TileData:Entity
{
    private static readonly NormalMovement DefaultMovementStrategy = new();

    public virtual bool Passable => true;
    public virtual bool Plantable => false;
    public abstract string TileType { get; }
    public override string EntityType => TileType;
    protected virtual IMovementStrategy MovementStrategy => DefaultMovementStrategy;

    protected TileData(int x, int y)
    {
        X = x;
        Y = y;
    }

    public virtual TileEnterResult OnEnter(PlayerRole player)
    {
        var strategy = MovementStrategy;
        return strategy != null
            ? TileEnterResult.None.WithStrategyOverride(strategy)
            : TileEnterResult.None;
    }
}
