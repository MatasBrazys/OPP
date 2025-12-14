// ./GameShared/Types/Map/World.cs
using GameShared.Types.Enemies;
using GameShared.Types.Players;

namespace GameShared.Types.Map
{
    public class World
    {
        // Toggle these to demo proxy vs direct map load during the lecture.
        private const bool UseLazyMapProxy = false;      // set to false to force eager load
        private const bool LogMapLoadMetrics = true;    // prints load time/memory
        private const string MapPath = "../assets/map.txt";

        public IMap Map { get; private set; }
        public List<Entity> Entities { get; private set; }

        public World()
        {
            GameShared.Types.Map.Map.LogLoadMetrics = LogMapLoadMetrics;

            if (UseLazyMapProxy)
            {
                Map = new LazyMapProxy(MapPath);
                Console.WriteLine("Map proxy enabled (actual map will load on first access)");
            }
            else
            {
                var directMap = new Map();
                directMap.LoadFromText(MapPath);
                Map = directMap;
                Console.WriteLine($"Map loaded eagerly from '{MapPath}'");
            }

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
