// .GameShared/Map/TileData.cs
namespace GameShared.Map;

public abstract record TileData
{
    public int X { get; init; }
    public int Y { get; init; }
    public virtual bool Passable => true;
    public abstract string TileType { get; }
    protected TileData(int x, int y)
    {
        X = x;
        Y = y;
    }
}
