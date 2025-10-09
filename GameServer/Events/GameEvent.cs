
namespace GameServer.Events
{
    public abstract class GameEvent
    {
        public string EventType { get; protected set; }
        public DateTime Timestamp { get; private set; }
        protected GameEvent(string eventType)
        {
            EventType = eventType;
            Timestamp = DateTime.UtcNow;
        }
        public class CollisionEvent : GameEvent
        {
            public int Entity1Id { get; }
            public int Entity2Id { get; }
            public string Entity1Type { get; }
            public string Entity2Type { get; }
            public float X { get; }
            public float Y { get; }
            public DateTime Timestamp { get; }

            public CollisionEvent(int entity1Id, int entity2Id, string entity1Type, string entity2Type, float x, float y)
                : base("collision")
            {
                Entity1Id = entity1Id;
                Entity2Id = entity2Id;
                Entity1Type = entity1Type;
                Entity2Type = entity2Type;
                X = x;
                Y = y;
            }
        }
    }
}