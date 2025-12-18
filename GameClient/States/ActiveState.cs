using GameClient.Networking;

namespace GameClient.States
{
    /// <summary>
    /// Active state: Player is idle/active and can respond to all inputs.
    /// - Can move
    /// - Can attack
    /// - Can plant/harvest
    /// - Can sleep (transition to SleepState)
    /// - Can accept tasks (transition to WorkingState)
    /// </summary>
    public class ActiveState : PlayerStateBase
    {
        private readonly ServerConnection _connection;
        private readonly int _playerId;
        private readonly CommandInvoker _commandInvoker;
        private readonly Action _handlePlantAction;
        private readonly Action _handleHarvestAction;

        public override string StateName => "Active";

        public ActiveState(
            PlayerStateManager stateManager,
            ServerConnection connection,
            int playerId,
            CommandInvoker commandInvoker,
            Action handlePlantAction,
            Action handleHarvestAction)
            : base(stateManager)
        {
            _connection = connection;
            _playerId = playerId;
            _commandInvoker = commandInvoker;
            _handlePlantAction = handlePlantAction;
            _handleHarvestAction = handleHarvestAction;
        }

        public override void HandleMovement(int dx, int dy)
        {

        }

        public override void HandleAttack(int aimX, int aimY)
        {
            Console.WriteLine($"[STATE-ACTIVE] Attacking at ({aimX}, {aimY}) - transitioning to AttackState");
            _stateManager.TransitionToAttack();
        }

        public override void HandlePlant()
        {
            Console.WriteLine($"[STATE-ACTIVE] Planting");
            _handlePlantAction?.Invoke();
        }

        public override void HandleHarvest()
        {
            Console.WriteLine($"[STATE-ACTIVE] Harvesting");
            _handleHarvestAction?.Invoke();
        }

        public override void HandleSleepToggle()
        {
            Console.WriteLine($"[STATE-ACTIVE] Falling asleep - transitioning to SleepState");
            _stateManager.TransitionToSleep();
        }

        public override void HandleTaskAssignment()
        {
            Console.WriteLine($"[STATE-ACTIVE] Task assigned - transitioning to WorkingState");
            _stateManager.TransitionToWorking();
        }
    }
}
