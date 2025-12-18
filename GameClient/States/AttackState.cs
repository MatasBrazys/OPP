using GameClient.Networking;

namespace GameClient.States
{
    /// <summary>
    /// Attack state: Player is performing an attack animation/action.
    /// - Cannot move during attack (queued after attack completes)
    /// - Cannot perform other attacks while attacking
    /// - Cannot plant/harvest
    /// - Cannot sleep
    /// - Automatically transitions back to ActiveState after attack completes
    /// </summary>
    public class AttackState : PlayerStateBase
    {
        private readonly ServerConnection _connection;
        private readonly int _playerId;
        private readonly CommandInvoker _commandInvoker;
        private DateTime _attackStartTime;
        private float _attackDuration; // Duration in seconds until attack completes
        private float _aimX;
        private float _aimY;
        private string _attackType;

        public override string StateName => "Attack";

        // Default attack duration in milliseconds
        private const float DefaultAttackDurationMs = 600f;

        public AttackState(
            PlayerStateManager stateManager,
            ServerConnection connection,
            int playerId,
            CommandInvoker commandInvoker)
            : base(stateManager)
        {
            _connection = connection;
            _playerId = playerId;
            _commandInvoker = commandInvoker;
            _attackDuration = DefaultAttackDurationMs / 1000f;
            _aimX = 0;
            _aimY = 0;
            _attackType = "slash";
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _attackStartTime = DateTime.UtcNow;
            Console.WriteLine($"[STATE-ATTACK] Attack initiated: {_attackType} at ({_aimX}, {_aimY})");
        }

        public override void OnExit()
        {
            base.OnExit();
            var duration = DateTime.UtcNow - _attackStartTime;
            Console.WriteLine($"[STATE-ATTACK] Attack completed after {duration.TotalMilliseconds:F0}ms");
        }

        public override void Update()
        {
            // Check if attack duration has elapsed
            var elapsed = (DateTime.UtcNow - _attackStartTime).TotalSeconds;
            if (elapsed >= _attackDuration)
            {
                Console.WriteLine($"[STATE-ATTACK] Attack animation finished, returning to ActiveState");
                _stateManager.TransitionToActive();
            }
        }

        public override void HandleMovement(int dx, int dy)
        {
            // Silent - movement blocked during attack but don't spam console
        }

        public override void HandleAttack(int aimX, int aimY)
        {
            Console.WriteLine($"[STATE-ATTACK] Cannot chain attacks, wait for current attack to finish");
        }

        public override void HandlePlant()
        {
            Console.WriteLine($"[STATE-ATTACK] Cannot plant during attack");
        }

        public override void HandleHarvest()
        {
            Console.WriteLine($"[STATE-ATTACK] Cannot harvest during attack");
        }

        public override void HandleSleepToggle()
        {
            Console.WriteLine($"[STATE-ATTACK] Cannot sleep during attack");
        }

        public override void HandleTaskAssignment()
        {
            Console.WriteLine($"[STATE-ATTACK] Cannot accept tasks during attack");
        }

        /// <summary>
        /// Set attack parameters when transitioning to this state.
        /// </summary>
        public void SetAttackInfo(float aimX, float aimY, string attackType = "slash", float durationMs = DefaultAttackDurationMs)
        {
            _aimX = aimX;
            _aimY = aimY;
            _attackType = attackType;
            _attackDuration = durationMs / 1000f;
        }

        /// <summary>
        /// Get the current attack progress (0.0 to 1.0).
        /// </summary>
        public float GetAttackProgress()
        {
            var elapsed = (float)(DateTime.UtcNow - _attackStartTime).TotalSeconds;
            return Math.Min(elapsed / _attackDuration, 1.0f);
        }
    }
}
