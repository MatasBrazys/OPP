//./GameShared/Types/Map/TileEnterResult.cs
using GameShared.Strategies;

namespace GameShared.Types.Map;

public sealed class TileEnterResult
{
    public static readonly TileEnterResult None = new();

    public TileEnterResult(
        IMovementStrategy? strategyOverride = null,
        bool replaceWithGrass = false,
        bool spawnClone = false)
    {
        StrategyOverride = strategyOverride;
        ReplaceWithGrass = replaceWithGrass;
        SpawnClone = spawnClone;
    }

    public IMovementStrategy? StrategyOverride { get; }
    public bool ReplaceWithGrass { get; }
    public bool SpawnClone { get; }

    public TileEnterResult WithStrategyOverride(IMovementStrategy strategy)
    {
        return new TileEnterResult(strategy, ReplaceWithGrass, SpawnClone);
    }

    public TileEnterResult EnsureStrategy(IMovementStrategy fallbackStrategy)
    {
        return StrategyOverride != null ? this : new TileEnterResult(fallbackStrategy, ReplaceWithGrass, SpawnClone);
    }

    public TileEnterResult WithReplaceGrass()
    {
        return ReplaceWithGrass ? this : new TileEnterResult(StrategyOverride, true, SpawnClone);
    }

    public TileEnterResult WithSpawnClone()
    {
        return SpawnClone ? this : new TileEnterResult(StrategyOverride, ReplaceWithGrass, true);
    }
}
