using GameShared.Types.Map;
using GameShared.Types.Players;

namespace GameShared.Strategies
{
    public class NormalMovement : IMovementStrategy
    {
        public bool CanMove(PlayerRole player, TileData tile) => tile.Passable;

        public int GetSpeed(PlayerRole player) => 10;
    }

    public class FishSwimMovement : IMovementStrategy
    {
        public bool CanMove(PlayerRole player, TileData tile) => true;

        public int GetSpeed(PlayerRole player) => 10;
    }

    public class AppleBoostMovement : IMovementStrategy
    {
        public bool CanMove(PlayerRole player, TileData tile) => tile.Passable;

        public int GetSpeed(PlayerRole player) => 15;
    }
    
    public class SandMovement : IMovementStrategy
    {
        public bool CanMove(PlayerRole player, TileData tile) => tile.Passable;

        public int GetSpeed(PlayerRole player) => 5;
    }   

}