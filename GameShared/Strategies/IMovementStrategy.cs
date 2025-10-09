using GameShared.Map;
using GameShared.Types.Players;

namespace GameShared.Strategies
{
    public interface IMovementStrategy
    {
        bool CanMove(PlayerRole player, TileData tile);

        int GetSpeed(PlayerRole player);
    }
}
