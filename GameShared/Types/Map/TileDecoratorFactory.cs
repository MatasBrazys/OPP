using GameShared.Types.Map.Decorators;

namespace GameShared.Types.Map;

public static class TileDecoratorFactory
{
    public static TileData Apply(TileData tile)
    {
        TileData result = tile;

        if (tile is AppleTile)
        {
            result = new SpeedBoostTileDecorator(result);
        }

        if (tile is SandTile)
        {
            result = new SandDragTileDecorator(result);
        }

        if (tile is FishTile or WaterTile)
        {
            result = new FishMovementTileDecorator(result);
        }

        if (tile is CherryTile)
        {
            result = new CherryCloneTileDecorator(result);
        }

        result = new DefaultMovementTileDecorator(result);

        return result;
    }
}
