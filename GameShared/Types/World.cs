using System.Collections.Generic;
using System.Linq;
using GameShared.Types.Players;

namespace GameShared.Types
{
    public class World
    {
        public Map Map { get; private set; }
        public List<Entity> Entities { get; private set; }

        public World()
        {
            Map = new Map();
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
            Console.WriteLine($"World.GetPlayers: world={GetHashCode()}, countIndex={Entities.OfType<PlayerState>().ToList().Count}");
            return Entities.OfType<PlayerRole>().ToList();
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