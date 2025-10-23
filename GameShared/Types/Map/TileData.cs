// .GameShared/Map/TileData.cs
using GameShared.Types;
using GameShared.Types.Players;

namespace GameShared.Types.Map;

public abstract class TileData:Entity
{
    public virtual bool Passable => true;
    public abstract string TileType { get; }
    protected TileData(int x, int y)
    {
        X = x;
        Y = y;
    }

    public virtual TileEnterResult OnEnter(PlayerRole player) => TileEnterResult.None;
}
