//gameclient/adapters/tiledatadapter.cs

using GameShared.Types.Map;
using GameClient.Rendering;

namespace GameClient.Adapters
{
    public class TileDataAdapter : IRenderable
    {
        private readonly TileData _tile;

        public TileDataAdapter(TileData tile)
        {
            _tile = tile;
        }

        public int X => _tile.X;
        public int Y => _tile.Y;

        // return the string id used in SpriteRegistry
        public string TextureName => _tile.TileType; 
    }
}

