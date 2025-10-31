// ./GameServer/IObserver.cs
namespace GameServer
{
    public interface IObserver
    {
        void OnGameEvent(Events.GameEvent gameEvent);
    }
}