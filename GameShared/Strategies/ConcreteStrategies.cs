using GameShared.Types.Map;
using GameShared.Types.Players;

namespace GameShared.Strategies
{
    public class NormalMovement : IMovementStrategy
    {
        public bool CanMove(PlayerRole player, TileData tile) => tile.Passable;

        public int GetSpeed(PlayerRole player) => 10;
        public object Clone()
        {
            return new NormalMovement();
        }
    }

    public class FishSwimMovement : IMovementStrategy
    {
        public bool CanMove(PlayerRole player, TileData tile) => true;

        public int GetSpeed(PlayerRole player) => 10;
        public object Clone()
        {
            return new FishSwimMovement();
        }
    }

    public class AppleBoostMovement : IMovementStrategy
    {
        public bool CanMove(PlayerRole player, TileData tile) => tile.Passable;

        public int GetSpeed(PlayerRole player) => 15;
        public object Clone()
        {
            return new AppleBoostMovement();
        }
    }
    
    public class SandMovement : IMovementStrategy
    {
        public bool CanMove(PlayerRole player, TileData tile) => tile.Passable;

        public int GetSpeed(PlayerRole player) => 5;
        public object Clone()
        {
            return new SandMovement();
        }
    }   

}