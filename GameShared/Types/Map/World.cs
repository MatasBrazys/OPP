// ./GameShared/Types/Map/World.cs
using GameShared.Types.Players;
using GameShared.Types.Enemies;

namespace GameShared.Types.Map
{
    public class World
    {
        public Map Map { get; private set; }
        public List<Entity> Entities { get; private set; }

        public World()
        {
            Map = new Map();
            Map.LoadFromText("../assets/map.txt");

            Console.WriteLine($"Map loaded: width={Map.Width}, height={Map.Height}");
            var tile = Map.GetTile(0, 0);
            Console.WriteLine(tile.TileType);
            Entities = new List<Entity>();
        }

        public void AddEntity(Entity entity)
        {
            Console.WriteLine($"World.AddEntity: world={GetHashCode()}, type={entity.GetType().Name}");
            Entities.Add(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            Entities.Remove(entity);
        }

        public PlayerRole? GetPlayer(int id)
        {
            return Entities?.OfType<PlayerRole>()?.FirstOrDefault(p => p.Id == id);
        }

        public List<PlayerRole> GetPlayers()
        {
            //Console.WriteLine($"World.GetPlayers: world={GetHashCode()}, count={Entities.OfType<PlayerRole>().Count()}"); 
            return Entities.OfType<PlayerRole>().ToList();
        }

        public List<Enemy> GetEnemies()
        {
            return Entities.OfType<Enemy>().ToList();
        }

        public void Update()
        {
            foreach (var entity in Entities)
            {
                entity.Update();
            }
        }
    }
}
