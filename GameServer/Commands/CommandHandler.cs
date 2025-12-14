// ./GameServer/Commands/CommandHandler.cs
using GameServer.Events;
using GameShared.Messages;
using static GameServer.Events.GameEvent;
using GameServer.Mediator;

namespace GameServer.Commands
{
    // Command handlers are observers of game events and may need the mediator reference.
    public abstract class CommandHandler : IObserver, IMediatorParticipant
    {
        // will be set when mediator attaches
        protected IGameMediator? Mediator { get; private set; }

        public abstract void OnGameEvent(GameEvent gameEvent);
        public abstract string[] HandledEventTypes { get; }

        // IMediatorParticipant implementation
        public virtual void OnMediatorAttached(IGameMediator mediator)
        {
            Mediator = mediator;
        }

        public virtual void OnMediatorDetached()
        {
            Mediator = null;
        }
    }

    public class CollisionCommandHandler : CommandHandler
    {
        private readonly IClientNotifier _notifier;

        public CollisionCommandHandler(IClientNotifier notifier)
        {
            _notifier = notifier;
        }

        public override string[] HandledEventTypes => new[] { "collision" };

        public override void OnGameEvent(GameEvent gameEvent)
        {
            if (gameEvent is CollisionEvent collisionEvent)
            {
                HandleCollision(collisionEvent);
            }
        }

        private void HandleCollision(CollisionEvent collision)
        {
            Console.WriteLine($"Collision detected between {collision.Entity1Type} (ID: {collision.Entity1Id}) and {collision.Entity2Type} (ID: {collision.Entity2Id}) at ({collision.X}, {collision.Y})");

            var collisionMessage = new CollisionMessage
            {
                Entity1Id = collision.Entity1Id,
                Entity2Id = collision.Entity2Id,
                Entity1Type = collision.Entity1Type,
                Entity2Type = collision.Entity2Type,
                X = collision.X,
                Y = collision.Y
            };

            _notifier.BroadcastToAll(collisionMessage);

            // Example usage of mediator (safe because OnMediatorAttached ensures Mediator is set)
            // Mediator?.IsTileReplacedWithGrass(...);
        }
    }
}
