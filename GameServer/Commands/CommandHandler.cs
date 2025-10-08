using GameServer.Events;
using GameServer.Observer;
using GameShared.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameServer.Events.GameEvent;

namespace GameServer.Commands
{
    public abstract class CommandHandler : IGameObserver
    {
        public abstract void OnGameEvent(GameEvent gameEvent);
        public abstract string[] HandledEventTypes { get; }
    }

    public class CollisionCommandHandler : CommandHandler
    {
        private readonly Server _server;

        public CollisionCommandHandler(Server server)
        {
            _server = server;
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
            // Simply log and broadcast - no health reduction
            Console.WriteLine($"Collision detected between {collision.Entity1Type} (ID: {collision.Entity1Id}) and {collision.Entity2Type} (ID: {collision.Entity2Id}) at ({collision.X}, {collision.Y})");

            // Broadcast collision to all clients
            var collisionMessage = new CollisionMessage
            {
                Entity1Id = collision.Entity1Id,
                Entity2Id = collision.Entity2Id,
                Entity1Type = collision.Entity1Type,
                Entity2Type = collision.Entity2Type,
                X = collision.X,
                Y = collision.Y
            };

            _server.BroadcastToAll(collisionMessage);
        }
    }
}
