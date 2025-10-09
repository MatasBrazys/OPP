// ./GameShared/Types/Map.cs
using GameShared.Map;
using GameShared.Types.DTOs;
using System.Text.Json;

namespace GameShared.Types
{
    public class Map
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private TileData[,] tiles;

        public Map()
        {
        }
        public void LoadFromJson(string json)
        {
            var mapJson = JsonSerializer.Deserialize<MapJson>(json);
            if (mapJson == null) return;

            Width = mapJson.Width;
            Height = mapJson.Height;
            tiles = new TileData[Width, Height];

            foreach (var t in mapJson.Tiles)
            {
                Console.WriteLine($"X={t.X}, Y={t.Y}, Id={t.Id}");
                TileData tile = t.Id switch
                {
                    0 => new GrassTile(t.X, t.Y),
                    1 => new TreeTile(t.X, t.Y),
                    2 => new HouseTile(t.X, t.Y),
                    3 => new AppleTile(t.X, t.Y),
                    4 => new FishTile(t.X, t.Y),
                    5 => new WaterTile(t.X, t.Y),
                    6 => new SandTile(t.X, t.Y),
                    _ => new GrassTile(t.X, t.Y) // default
                };
                tiles[t.X, t.Y] = tile;
            }
        }
        public TileData GetTile(int x, int y)
        {
            return tiles[x, y];
        
        }

        // Add map logic here as needed
    }
}