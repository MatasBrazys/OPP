namespace GameServer.Mediator
{
    /// <summary>
    /// Abstraction for sending messages to clients.
    /// Implemented by Server so mediator/handlers do not depend on concrete server.
    /// </summary>
    public interface IClientNotifier
    {
        void BroadcastState();
        void BroadcastToAll<T>(T message);
        void BroadcastMessage<T>(T message);
        void SendToClient<T>(int playerId, T message);
    }
}
