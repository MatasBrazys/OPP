// ./gameshare/interfaces/IAttackStrategy.cs
using GameShared.Messages;
using GameShared.Types.Players;

namespace GameShared.Interfaces
{
    public interface IAttackStrategy
    {
        void ExecuteAttack(PlayerRole player, AttackMessage msg);
    }
}
