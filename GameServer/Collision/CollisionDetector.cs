// ./GameServer/Collision/CollisionDetector.cs
using GameServer;
using GameServer.Events;
using GameShared.Types;
using static GameServer.Events.GameEvent;
using GameServer.Mediator;
using System.Linq;
using System.Collections.Generic;

namespace GameServer.Collision
{
    // Implement IMediatorParticipant so the detector can subscribe itself.
    public class CollisionDetector : Subject, IMediatorParticipant
    {
        private IGameMediator? _mediator;

        // Participant-driven helper: call this from bootstrap to subscribe.
        public void SubscribeToMediator(IGameMediator mediator) => mediator.RegisterParticipant(this);

        // IMediatorParticipant lifecycle callbacks
        public void OnMediatorAttached(IGameMediator mediator) => _mediator = mediator;
        public void OnMediatorDetached() => _mediator = null;

        public void CheckCollisions(IEnumerable<Entity> entities) // ✅ Changed from object to Entity
        {
            var entityList = entities.ToList();

            for (int i = 0; i < entityList.Count; i++)
            {
                for (int j = i + 1; j < entityList.Count; j++)
                {
                    var entityA = entityList[i];
                    var entityB = entityList[j];

                    if (CheckAABBCollision(entityA, entityB))
                    {
                        var collisionEvent = new CollisionEvent(
                            entityA.Id,
                            entityB.Id,
                            entityA.EntityType,
                            entityB.EntityType,
                            (entityA.X + entityB.X) / 2f,
                            (entityA.Y + entityB.Y) / 2f
                        );

                        NotifyObservers(collisionEvent);
                    }
                }
            }
        }

        private bool CheckAABBCollision(Entity entityA, Entity entityB) // ✅ Changed from object to Entity
        {
            const float size = 32f; // bounding box size

            return entityA.X < entityB.X + size &&
                   entityA.X + size > entityB.X &&
                   entityA.Y < entityB.Y + size &&
                   entityA.Y + size > entityB.Y;
        }
    }
}