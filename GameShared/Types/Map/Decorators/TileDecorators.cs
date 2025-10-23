using GameShared.Strategies;
using GameShared.Types.Players;

namespace GameShared.Types.Map.Decorators;

public abstract class TileDecorator : TileData
{
    protected TileData Inner { get; }

    protected TileDecorator(TileData inner) : base(inner.X, inner.Y)
    {
        Inner = inner;
    }

    public override bool Passable => Inner.Passable;
    public override string TileType => Inner.TileType;

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        return Inner.OnEnter(player);
    }
}

public sealed class DefaultMovementTileDecorator : TileDecorator
{
    private static readonly NormalMovement DefaultStrategy = new();

    public DefaultMovementTileDecorator(TileData inner) : base(inner)
    {
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        var innerResult = base.OnEnter(player);
        return innerResult.EnsureStrategy(DefaultStrategy);
    }
}

public sealed class SpeedBoostTileDecorator : TileDecorator
{
    private static readonly AppleBoostMovement BoostStrategy = new();

    public SpeedBoostTileDecorator(TileData inner) : base(inner)
    {
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        var result = base.OnEnter(player);
        return result.WithStrategyOverride(BoostStrategy);
    }
}

public sealed class SandDragTileDecorator : TileDecorator
{
    private static readonly SandMovement SandStrategy = new();

    public SandDragTileDecorator(TileData inner) : base(inner)
    {
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        var result = base.OnEnter(player);
        return result.WithStrategyOverride(SandStrategy);
    }
}

public sealed class FishMovementTileDecorator : TileDecorator
{
    private static readonly FishSwimMovement SwimStrategy = new();

    public FishMovementTileDecorator(TileData inner) : base(inner)
    {
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        var result = base.OnEnter(player);
        return result.WithStrategyOverride(SwimStrategy);
    }
}

public sealed class CherryCloneTileDecorator : TileDecorator
{
    public CherryCloneTileDecorator(TileData inner) : base(inner)
    {
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        var result = base.OnEnter(player);

        if (Inner is GameShared.Types.Map.CherryTile cherry && cherry.CanBeEaten())
        {
            cherry.Eat();
            result = result.WithSpawnClone();
            result = result.WithReplaceGrass();
        }

        return result;
    }
}
