using System;
using GameShared.Strategies;

namespace GameShared.Types.Players
{
    /// <summary>
    /// Concrete memento storing player position and movement strategy.
    /// Data is internal so only the originator can restore it.
    /// </summary>
    public sealed class PlayerMemento : IPlayerMemento
    {
        private readonly int _id;
        private readonly int _x;
        private readonly int _y;
        private readonly IMovementStrategy _movementStrategy;
        private readonly DateTime _snapshotDateUtc;
        private readonly string _password;

        internal int Id => _id;
        internal int X => _x;
        internal int Y => _y;
        internal IMovementStrategy MovementStrategy => _movementStrategy;

        public PlayerMemento(int id, int x, int y, IMovementStrategy strategy, string password)
        {
            _id = id;
            _x = x;
            _y = y;
            _movementStrategy = strategy;
            _snapshotDateUtc = DateTime.UtcNow;
            _password = password;
        }

        string IPlayerMemento.GetName() => $"Player#{_id} @ ({_x},{_y})";

        DateTime IPlayerMemento.GetSnapshotDate() => _snapshotDateUtc;

        public IPlayerMemento GetSnapshot(string password)
        {
            if (password != _password)
                throw new UnauthorizedAccessException("Invalid password for accessing memento data.");
            return this;
        }
    }
}
