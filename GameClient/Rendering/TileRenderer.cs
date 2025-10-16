// File: GameClient/Rendering/TileRenderer.cs
namespace GameClient.Rendering
{
    public class TileRenderer
    {
        private readonly IRenderable _tile;
        private readonly int _tileSize;

        public int X => _tile.X;
        public int Y => _tile.Y;
        public IRenderable Tile => _tile;

        public TileRenderer(IRenderable tile, int tileSize)
        {
            _tile = tile;
            _tileSize = tileSize;
        }

        public void Draw(Graphics g)
        {
            Image sprite = SpriteRegistry.GetSprite(_tile.TextureName);
            g.DrawImage(sprite, _tile.X * _tileSize, _tile.Y * _tileSize, _tileSize, _tileSize);
        }
    }
}