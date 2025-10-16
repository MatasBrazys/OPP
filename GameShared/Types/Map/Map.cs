// ./GameShared/Types/Map.cs
using GameShared.Types.DTOs;
using System.Text.Json;

namespace GameShared.Types.Map
{
    public class Map
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private TileData[,] tiles;

        public Map()
        {
        }
 public void LoadFromText(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            if (lines.Length < 2)
                throw new Exception("Invalid map file: missing width/height.");

            // parse dimensions
            Width = int.Parse(lines[0]);
            Height = int.Parse(lines[1]);

            if (lines.Length < 2 + Height)
                throw new Exception("Invalid map file: not enough rows for tiles.");

            tiles = new TileData[Width, Height];

            for (int y = 0; y < Height; y++)
            {
                string row = lines[2 + y];
                if (row.Length != Width)
                    throw new Exception($"Invalid row length at line {2 + y}: expected {Width}, got {row.Length}");

                for (int x = 0; x < Width; x++)
                {
                    int tileId = row[x] - '0'; // convert char '0'..'9' to int 0..9
                    tiles[x, y] = tileId switch
                    {
                        0 => new GrassTile(x, y),
                        1 => new TreeTile(x, y),
                        2 => new HouseTile(x, y),
                        3 => new AppleTile(x, y),
                        4 => new FishTile(x, y),
                        5 => new WaterTile(x, y),
                        6 => new SandTile(x, y),
                        7 => new CherryTile(x, y),
                        _ => new GrassTile(x, y)
                    };
                }
            }
        }
        public TileData GetTile(int x, int y)
        {
            return tiles[x, y];
        
        }
        public void SetTile(int x, int y, TileData newTile)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                tiles[x, y] = newTile;
            }
        }

    }
}