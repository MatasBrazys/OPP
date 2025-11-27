using GameShared.Strategies;

namespace GameShared.Types.Players
{
    public sealed class PlayerMemento
    {
        public int Id { get; }
        public int X { get; }
        public int Y { get; }
        public int Health { get; }
        public IMovementStrategy MovementStrategy { get; }

        public PlayerMemento(int id, int x, int y, int health, IMovementStrategy strategy)
        {
            Id = id;
            X = x;
            Y = y;
            Health = health;
            MovementStrategy = strategy;
        }
    }
}
