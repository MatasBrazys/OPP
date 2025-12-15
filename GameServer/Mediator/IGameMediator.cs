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
        void HandleHarvestAction(HarvestActionMessage action);
        void HandleAutoHarvest(AutoHarvestMessage action);
        void UndoLastMove(int playerId);
        bool IsTileReplacedWithGrass(int tileX, int tileY);

        // subscription API — participants call these to register/unregister themselves
        void RegisterParticipant(IMediatorParticipant participant);
        void RemoveParticipant(IMediatorParticipant participant);

        // participant lookup API
        bool TryGetParticipant<T>(out T participant) where T : class;
        IEnumerable<T> GetParticipants<T>() where T : class;
        bool HasParticipant<T>() where T : class;
    }
}
