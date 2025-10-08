using GameServer.Events;
using GameServer.Observer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GameServer.Events.GameEvent;

public class CollisionDetector : IGameObservable
{
    private readonly List<IGameObserver> _observers = new();
    private readonly object _observerLock = new();

    public void CheckCollisions(IEnumerable<object> players)
    {
        var playerList = players.ToList();

        // Simple AABB collision detection
        for (int i = 0; i < playerList.Count; i++)
        {
            for (int j = i + 1; j < playerList.Count; j++)
            {
                var player1 = playerList[i];
                var player2 = playerList[j];

                if (CheckAABBCollision(player1, player2))
                {
                    var collisionEvent = new CollisionEvent(
                        GetPlayerId(player1), GetPlayerId(player2),
                        GetPlayerRole(player1), GetPlayerRole(player2),
                        (GetPlayerX(player1) + GetPlayerX(player2)) / 2,
                        (GetPlayerY(player1) + GetPlayerY(player2)) / 2
                    );

                    NotifyObservers(collisionEvent);
                }
            }
        }
    }

    private bool CheckAABBCollision(object playerA, object playerB)
    {
        // Assuming each player has a bounding box of 50x50 pixels
        const float playerSize = 50f;

        float aX = GetPlayerX(playerA);
        float aY = GetPlayerY(playerA);
        float bX = GetPlayerX(playerB);
        float bY = GetPlayerY(playerB);

        return aX < bX + playerSize &&
               aX + playerSize > bX &&
               aY < bY + playerSize &&
               aY + playerSize > bY;
    }

    private float GetPlayerX(object player)
    {
        var prop = player.GetType().GetProperty("X");
        if (prop != null)
        {
            var value = prop.GetValue(player);
            return value is float floatValue ? floatValue : Convert.ToSingle(value);
        }
        return 0f;
    }

    private float GetPlayerY(object player)
    {
        var prop = player.GetType().GetProperty("Y");
        if (prop != null)
        {
            var value = prop.GetValue(player);
            return value is float floatValue ? floatValue : Convert.ToSingle(value);
        }
        return 0f;
    }

    private int GetPlayerId(object player)
    {
        var prop = player.GetType().GetProperty("Id");
        if (prop != null)
        {
            var value = prop.GetValue(player);
            return value is int intValue ? intValue : Convert.ToInt32(value);
        }
        return -1;
    }

    private string GetPlayerRole(object player)
    {
        var prop = player.GetType().GetProperty("RoleType");
        return prop != null ? (prop.GetValue(player)?.ToString() ?? "unknown") : "unknown";
    }

    public void RegisterObserver(IGameObserver observer)
    {
        lock (_observerLock)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }
    }

    public void RemoveObserver(IGameObserver observer)
    {
        lock (_observerLock)
        {
            _observers.Remove(observer);
        }
    }

    public void NotifyObservers(GameEvent gameEvent)
    {
        List<IGameObserver> observersCopy;
        lock (_observerLock)
        {
            observersCopy = new List<IGameObserver>(_observers);
        }

        foreach (var observer in observersCopy)
        {
            try
            {
                observer.OnGameEvent(gameEvent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error notifying observer: {ex.Message}");
            }
        }
    }
}
