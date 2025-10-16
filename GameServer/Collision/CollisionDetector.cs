using GameServer;
using GameServer.Events;
using static GameServer.Events.GameEvent;

public class CollisionDetector : Subject
{
    public void CheckCollisions(IEnumerable<object> entities)
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
                    string typeA = GetEntityType(entityA);
                    string typeB = GetEntityType(entityB);
                    

                    var collisionEvent = new CollisionEvent(
                        GetEntityId(entityA), GetEntityId(entityB),
                        typeA, typeB,
                        (GetEntityX(entityA) + GetEntityX(entityB)) / 2,
                        (GetEntityY(entityA) + GetEntityY(entityB)) / 2
                    );
                    //reik switcho ne playeriams
                    NotifyObservers(collisionEvent);
                }
            }
        }
    }

    private bool CheckAABBCollision(object entityA, object entityB)
    {
        const float size = 32f; // bounding box size

        float aX = GetEntityX(entityA);
        float aY = GetEntityY(entityA);
        float bX = GetEntityX(entityB);
        float bY = GetEntityY(entityB);

        return aX < bX + size &&
               aX + size > bX &&
               aY < bY + size &&
               aY + size > bY;
    }

    private float GetEntityX(object entity)
    {
        var prop = entity.GetType().GetProperty("X");
        if (prop == null) return 0f;

        var value = prop.GetValue(entity);
        return value is float f ? f : Convert.ToSingle(value);
    }

    private float GetEntityY(object entity)
    {
        var prop = entity.GetType().GetProperty("Y");
        if (prop == null) return 0f;

        var value = prop.GetValue(entity);
        return value is float f ? f : Convert.ToSingle(value);
    }

    private int GetEntityId(object entity)
    {
        var prop = entity.GetType().GetProperty("Id");
        if (prop == null) return -1;

        var value = prop.GetValue(entity);
        return value is int i ? i : Convert.ToInt32(value);
    }

    private string GetEntityType(object entity)
    {
        var prop = entity.GetType().GetProperty("RoleType");
        return prop != null ? (prop.GetValue(entity)?.ToString() ?? "unknown") : "unknown";
    }
}
