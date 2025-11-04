// ./GameClient/Managers/TileManager.cs
using System.Collections.Generic;
using GameShared.Types.Map;
using GameClient.Adapters;
using GameClient.Rendering;

namespace GameClient.Managers
{
    public class TileManager
    {
        private readonly Dictionary<(int x, int y), TileRenderer> _tileRenderers = new();
        public int TileSize { get; }

        public TileManager(int tileSize)
        {
            TileSize = tileSize;
        }

        public void Clear() => _tileRenderers.Clear();

        public void SetTile(TileData tile)
        {
            _tileRenderers[(tile.X, tile.Y)] = new TileRenderer(new TileDataAdapter(tile), TileSize);
        }

        public void DrawAll(System.Drawing.Graphics g)
        {
            foreach (var tr in _tileRenderers.Values) tr.Draw(g);
        }
    }
}
