//./GameServer/Events/GameEvent.cs
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
        
    }
}