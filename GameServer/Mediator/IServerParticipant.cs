
namespace GameServer.Mediator
{
    /// <summary>
    /// Optional marker for server implementations that participate in mediator lifecycle.
    /// Extends IMediatorParticipant so it participates in subscription callbacks.
    /// </summary>
    public interface IServerParticipant : IMediatorParticipant
    {
    }
}