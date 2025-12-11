using GameServer.Events;
using GameShared.Messages;
using GameServer;

namespace GameServer.Mediator
{
    public interface IGameMediator : IObserver
    {
        void HandleInput(int playerId, InputMessage input);
        void HandleAttack(AttackMessage attack);
        void HandlePlantAction(PlantActionMessage action);
        void UndoLastMove(int playerId);
        bool IsTileReplacedWithGrass(int tileX, int tileY);
    }
}
