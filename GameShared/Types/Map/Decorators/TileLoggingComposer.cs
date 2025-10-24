namespace GameShared.Types.Map.Decorators;

public static class TileLoggingComposer
{
    public static TileData Wrap(TileData tile)
    {
        if (!TileLogSink.IsEnabled || tile is TileLoggingDecorator)
        {
            return tile;
        }

        TileData result = tile;
        result = new TileEntryLoggerDecorator(result);
        result = new TileStrategyLoggerDecorator(result);
        result = new TileMetricsDecorator(result);
        return result;
    }
}
