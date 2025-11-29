// ./GameShared/Types/Map/World.cs
using GameShared.Types.Players;
using GameShared.Types.Enemies;

namespace GameShared.Types.Map
{
    public class World : Entity
    {
        public Map Map { get; private set; }

        public World()
        {
            Map = new Map();
            Map.LoadFromText("../assets/map.txt");

            Console.WriteLine($"Map loaded: width={Map.Width}, height={Map.Height}");
            var tile = Map.GetTile(0, 0);
            Console.WriteLine(tile.TileType);
        }

        public override string EntityType => "World";

        // Add child entity (PlayerRole, Enemy, etc.)
        public override void Add(Entity child)
        {
            base.Add(child); // uses Entity's thread-safe _children
        }

        public override void Remove(Entity child)
        {
            base.Remove(child); // thread-safe removal
        }

        public PlayerRole? GetPlayer(int id)
        {
            return GetChildren()
                .OfType<PlayerRole>()
                .FirstOrDefault(p => p.Id == id);
        }

        public List<PlayerRole> GetPlayers()
        {
            return GetChildren()
                .OfType<PlayerRole>()
                .ToList();
        }

        public List<Enemy> GetEnemies()
        {
            return GetChildren()
                .OfType<Enemy>()
                .ToList();
        }

        public override void Update()
        {
            // Update all children safely via Entity composite
            base.Update();

            // Optionally, add world-level logic here (e.g., Map updates)
        }
    }
}
