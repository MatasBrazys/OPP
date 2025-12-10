//./GameShared/Types/Players/PlayerRole.cs
using System.Drawing;
using GameShared.Interfaces;
using GameShared.Strategies;
using GameShared.Types.Map;

namespace GameShared.Types.Players
{
    public abstract class PlayerRole : Entity, ICloneable
    {
        public int Health { get; set; }
        public string RoleType { get; set; }
        public Color RoleColor { get; set; }
        public IAttackStrategy AttackStrategy { get; set; }

        //cia reik abstract factory sudet, siuo metu as tiesiogiai facade sudedu situs, reiks settint juos i internal veliau. 
        //important!!!!!
        public float AttackRange { get; set; }

        //public float AttackCooldown { get; set; }
        // public float AttackDamage { get; set; }

        public override string EntityType => RoleType;
        private IMovementStrategy _currentStrategy;
        private TileData? _previousTile = null;

        public void SetMovementStrategy(IMovementStrategy strategy)
        {
            _currentStrategy = strategy ?? new NormalMovement();
        }

        public abstract PlayerRole DeepCopy();
        public object Clone()
        {
            return DeepCopy();
        }
        public virtual PlayerRole ShallowCopy()
        {
            var copy = (PlayerRole)this.MemberwiseClone();
            return copy;
        }

        protected PlayerRole(IMovementStrategy initialStrategy)
        {
            _currentStrategy = initialStrategy ?? new NormalMovement();
        }

        public void OnMoveTile(TileData currentTile)
        {
            _previousTile = currentTile;
        }

        public int GetSpeed() => _currentStrategy.GetSpeed(this);
        public bool CanMove(TileData tile) => _currentStrategy.CanMove(this, tile);

        protected void CopyBasePropertiesTo(PlayerRole target)
        {
            target.Id = 0;
            target.X = this.X;
            target.Y = this.Y;
            target.Health = this.Health;
            target.RoleType = this.RoleType;
            target.RoleColor = this.RoleColor;

            // Deep copy the strategy
            if (this._currentStrategy is ICloneable cloneableStrategy)
            {
                target._currentStrategy = (IMovementStrategy)cloneableStrategy.Clone();
            }
            else
            {
                target._currentStrategy = this._currentStrategy;
            }
            target._previousTile = null;
        }

        public void TestDeepCopy()
        {
            Console.WriteLine("======== DEEP COPY TEST =======");

            var original = this;
            Console.WriteLine($"Original: ID={original.Id}, Strategy={original._currentStrategy.GetType().Name}");

            var clone = this.DeepCopy();
            Console.WriteLine($"Clone: ID={clone.Id}, Strategy={clone._currentStrategy.GetType().Name}");

            bool strategiesAreDifferent = !ReferenceEquals(original._currentStrategy, clone._currentStrategy);
            Console.WriteLine($"Strategies differ: {strategiesAreDifferent}");

            Console.WriteLine("Changing original strategy...");
            original._currentStrategy = new AppleBoostMovement();

            Console.WriteLine($"Original strategy after change: {original._currentStrategy.GetType().Name}");
            Console.WriteLine($"Clone strategy after change: {clone._currentStrategy.GetType().Name}");
        }

        public IPlayerMemento CreateMemento()
        {
            // Clone strategy if possible; otherwise reuse
            var strategyCopy = _currentStrategy is ICloneable c
                ? (IMovementStrategy)c.Clone()
                : _currentStrategy;

            return new PlayerMemento(Id, X, Y, strategyCopy);
        }

        public void RestoreMemento(IPlayerMemento memento)
        {
            // Only the originator knows how to unpack the concrete memento
            if (memento is not PlayerMemento snapshot || snapshot.Id != Id) return;

            X = snapshot.X;
            Y = snapshot.Y;
            _currentStrategy = snapshot.MovementStrategy ?? new NormalMovement();
        }
    }
}
