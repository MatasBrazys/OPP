using System.Drawing;
using GameShared.Strategies;
using GameShared.Types.Map;

namespace GameShared.Types.Players
{
    public abstract class PlayerRole : PlayerState, ICloneable
    {
        public int Health { get; protected set; } = 5;
        public string RoleType { get; protected set; }
        public Color RoleColor { get; protected set; }

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

        private IMovementStrategy _currentStrategy;
        private TileData? _previousTile = null;

        protected PlayerRole(IMovementStrategy initialStrategy)
        {
            _currentStrategy = initialStrategy ?? new NormalMovement();
        }

        /// <summary>
        /// Call this after moving onto a tile to update strategy
        /// </summary>
        public void OnMoveTile(TileData currentTile)
        {
            if (_previousTile is WaterTile && _currentStrategy is FishSwimMovement && !(currentTile is WaterTile || currentTile is FishTile))
            {
                _currentStrategy = new NormalMovement();
            }
            else if (currentTile is AppleTile)
            {
                _currentStrategy = new AppleBoostMovement();
            }
            else if (currentTile is SandTile)
            {
                _currentStrategy = new SandMovement();
            }
            else if ((currentTile is WaterTile || currentTile is FishTile) && _currentStrategy is not FishSwimMovement)
            {
                _currentStrategy = new FishSwimMovement();
            }
            else if (_currentStrategy is not FishSwimMovement)
            {
                _currentStrategy = new NormalMovement();
            }

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
            

    }
}
