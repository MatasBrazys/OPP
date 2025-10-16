using GameShared.Types.Map;
using GameShared.Types.Players;

namespace GameShared.Strategies
{
    public interface IMovementStrategy : ICloneable
    {
        bool CanMove(PlayerRole player, TileData tile);

        int GetSpeed(PlayerRole player);
    }
}
