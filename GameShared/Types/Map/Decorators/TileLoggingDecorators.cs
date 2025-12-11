//./GameShared/Types/Map/Decorators/TileLoggingDecorators.cs
using System;
using System.Threading;
using GameShared.Types.Players;

namespace GameShared.Types.Map.Decorators;

public static class TileLogSink
{
    private static bool _isEnabled = true;

    public static Action<string>? Logger { get; set; }

    public static bool IsEnabled
    {
        get => Volatile.Read(ref _isEnabled);
        set => Volatile.Write(ref _isEnabled, value);
    }

    public static void Log(string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        Logger?.Invoke(message);
    }
}

public abstract class TileLoggingDecorator : TileData
{
    public TileData Inner { get; }

    protected TileLoggingDecorator(TileData inner) : base(inner.X, inner.Y)
    {
        Inner = inner;
    }

    public override bool Passable => Inner.Passable;
    public override bool Plantable => Inner.Plantable;
    public override string TileType => Inner.TileType;

    protected void LogMessage(string message)
    {
        TileLogSink.Log($"[{DateTime.UtcNow:O}] Tile({TileType}) at ({X},{Y}) - {message}");
    }

    protected static string DescribePlayer(PlayerRole player)
    {
        if (player == null)
        {
            return "unknown player";
        }

        var role = string.IsNullOrWhiteSpace(player.RoleType) ? "UnknownRole" : player.RoleType;
        return $"player#{player.Id} ({role})";
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        return Inner.OnEnter(player);
    }
}

public sealed class TileEntryLoggerDecorator : TileLoggingDecorator
{
    public TileEntryLoggerDecorator(TileData inner) : base(inner)
    {
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        LogMessage($"Entering tile {Inner.X}x{Inner.Y} by {DescribePlayer(player)}");
        var result = base.OnEnter(player);
        LogMessage($"Completed enter for {DescribePlayer(player)}");
        return result;
    }
}

public sealed class TileStrategyLoggerDecorator : TileLoggingDecorator
{
    public TileStrategyLoggerDecorator(TileData inner) : base(inner)
    {
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        var result = base.OnEnter(player);
        if (result.StrategyOverride != null)
        {
            LogMessage($"Applied strategy {result.StrategyOverride.GetType().Name} for {DescribePlayer(player)}");
        }
        else
        {
            LogMessage($"No strategy override for {DescribePlayer(player)}");
        }

        if (result.SpawnClone)
        {
            LogMessage("Tile requested spawn clone");
        }

        if (result.ReplaceWithGrass)
        {
            LogMessage("Tile requested replace with grass");
        }

        return result;
    }
}

public sealed class TileMetricsDecorator : TileLoggingDecorator
{
    private int _enterCount;

    public TileMetricsDecorator(TileData inner) : base(inner)
    {
    }

    public override TileEnterResult OnEnter(PlayerRole player)
    {
        var count = Interlocked.Increment(ref _enterCount);
       LogMessage($"Total entries so far: {count}");
        return base.OnEnter(player);
    }
}
