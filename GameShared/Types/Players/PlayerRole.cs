//./GameShared/Types/Players/PlayerRole.cs
using System.Drawing;
using GameShared.Strategies;
using GameShared.Types.Map;

namespace GameShared.Types.Players
{
    public abstract class PlayerRole : Entity, ICloneable
    {
        public int Health { get; internal set; }
        public string RoleType { get; internal set; }
        public Color RoleColor { get; internal set; }

        public string AttackType { get; internal set; }
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

        public abstract void Attack();
        public abstract void SpecialAbility();

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
    }
}

