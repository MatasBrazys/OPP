using System.Drawing;
using GameShared.Map;

namespace GameClient.Rendering;

public class TileRenderer
{
    int TileSize;
    public TileData Tile { get; private set; }
    public Image Sprite { get; private set; }

    public TileRenderer(TileData tile, Image sprite, int tileSize)
    {
        Tile = tile;
        Sprite = sprite;
        TileSize = tileSize;
    }
    
    public void Draw(Graphics g)
    {
        g.DrawImage(Sprite, Tile.X * TileSize, Tile.Y * TileSize, TileSize, TileSize);
    }
}
