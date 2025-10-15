namespace GameServer
{
    public interface IGameObserver
    {
        void OnGameEvent(Events.GameEvent gameEvent);
    }
}