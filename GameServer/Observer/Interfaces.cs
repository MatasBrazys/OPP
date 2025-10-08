using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Observer
{
    public interface IGameObserver
    {
        void OnGameEvent(Events.GameEvent gameEvent);
    }

    public interface IGameObservable
    {
        void RegisterObserver(IGameObserver observer);
        void RemoveObserver(IGameObserver observer);
        void NotifyObservers(Events.GameEvent gameEvent);
    }
}
