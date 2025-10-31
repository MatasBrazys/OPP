﻿// ./GameServer/Commands/CommandHandler.cs
using GameServer.Events;
using GameShared.Messages;
using static GameServer.Events.GameEvent;

namespace GameServer.Commands
{
    public abstract class CommandHandler : IObserver
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

            _server.BroadcastToAll(collisionMessage);
        }
    }
}
