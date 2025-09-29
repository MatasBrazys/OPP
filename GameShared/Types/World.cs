using System.Collections.Generic;
using System.Linq;

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
            Entities.Add(entity);
        }

        public void RemoveEntity(Entity entity)
        {
            Entities.Remove(entity);
        }

        public PlayerState GetPlayer(int id)
        {
            return Entities.OfType<PlayerState>().FirstOrDefault(p => p.Id == id);
        }

        public List<PlayerState> GetPlayers()
        {
            return Entities.OfType<PlayerState>().ToList();
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