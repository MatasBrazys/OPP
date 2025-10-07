// File: GameClient/Rendering/TileRenderer.cs
using System.Drawing;
using GameShared.Map;

namespace GameClient.Rendering
{
    public class TileRenderer
    {
        private readonly int _tileSize;
        public TileData Tile { get; }
        public Image Sprite { get; }

        public TileRenderer(TileData tile, Image sprite, int tileSize)
        {
            Tile = tile;
            Sprite = sprite;
            _tileSize = tileSize;
        }

        public void Draw(Graphics g)
        {
            g.DrawImage(Sprite, Tile.X * _tileSize, Tile.Y * _tileSize, _tileSize, _tileSize);
        }
    }
}
