// ./GameShared/Types/Map.cs
using System.Diagnostics;
using GameShared.Types.DTOs;
using GameShared.Types.Map.Decorators;
using static GameShared.Types.Map.CherryTile;

namespace GameShared.Types.Map
{
    public class Map : IMap
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        private TileData[,] tiles;

        // Optional instrumentation for demos; enable via Map.LogLoadMetrics = true.
        public static bool LogLoadMetrics = false;

        public Map()
        {
        }

        public void LoadFromText(string filePath)
        {
            Stopwatch? sw = null;
            long before = 0;
            if (LogLoadMetrics)
            {
                sw = Stopwatch.StartNew();
                before = GC.GetTotalMemory(false);
            }

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
                    TileData tile = tileId switch
                    {
                        0 => new GrassTile(x, y),
                        1 => new TreeTile(x, y),
                        2 => new HouseTile(x, y),
                        3 => new AppleTile(x, y),
                        4 => new FishTile(x, y),
                        5 => new WaterTile(x, y),
                        6 => new SandTile(x, y),
                        7 => new CherryTile(x, y),
                        8 => new WheatTile(x, y),
                        _ => new GrassTile(x, y)
                    };
                    tiles[x, y] = TileLoggingComposer.Wrap(tile);
                }
            }

            if (LogLoadMetrics && sw != null)
            {
                sw.Stop();
                var after = GC.GetTotalMemory(false);
                Console.WriteLine($"[Map] Loaded from '{filePath}' in {sw.ElapsedMilliseconds} ms, Δmem={after - before} bytes");
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
                tiles[x, y] = TileLoggingComposer.Wrap(newTile);
            }
        }

        public void LoadFromDimensions(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new TileData[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tiles[x, y] = TileLoggingComposer.Wrap(new GrassTile(x, y));
                }
            }
        }
    }
}
