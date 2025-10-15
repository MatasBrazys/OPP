using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public abstract class ISubject
    {
        protected readonly List<IGameObserver> _observers = new();
        protected readonly object _observerLock = new();

        public virtual void RegisterObserver(IGameObserver observer)
        {
            lock (_observerLock)
            {
                if (!_observers.Contains(observer))
                    _observers.Add(observer);
            }
        }

        public virtual void RemoveObserver(IGameObserver observer)
        {
            lock (_observerLock)
            {
                _observers.Remove(observer);
            }
        }

        public virtual void NotifyObservers(Events.GameEvent gameEvent)
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
}
