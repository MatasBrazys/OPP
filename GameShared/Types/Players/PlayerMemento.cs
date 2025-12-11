using GameShared.Strategies;

namespace GameShared.Types.Players
{
    /// <summary>
    /// Marker interface so external callers can store/pass mementos without seeing the internals.
    /// </summary>
    public interface IPlayerMemento { }

    // Concrete memento stays internal; only PlayerRole knows its fields.
    internal sealed class PlayerMemento : IPlayerMemento
    {
        internal int Id { get; }
        internal int X { get; }
        internal int Y { get; }
        internal IMovementStrategy MovementStrategy { get; }

        internal PlayerMemento(int id, int x, int y, IMovementStrategy strategy)
        {
            Id = id;
            X = x;
            Y = y;
            MovementStrategy = strategy;
        }
    }
}
