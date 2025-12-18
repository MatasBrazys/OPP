namespace GameClient.States
{
    /// <summary>
    /// Sleep state: Player is asleep and cannot perform any actions.
    /// - Cannot move
    /// - Cannot attack
    /// - Cannot plant/harvest
    /// - Can only wake up by pressing Z again (transitions back to ActiveState)
    /// </summary>
    public class SleepState : PlayerStateBase
    {
        private DateTime _sleepStartTime;
        private bool _isRenderingSleep;

        public override string StateName => "Sleep";

        public SleepState(PlayerStateManager stateManager) : base(stateManager)
        {
            _sleepStartTime = DateTime.UtcNow;
            _isRenderingSleep = false;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _sleepStartTime = DateTime.UtcNow;
            _isRenderingSleep = true;
            Console.WriteLine($"[STATE-SLEEP] Player is now sleeping");
        }

        public override void OnExit()
        {
            base.OnExit();
            _isRenderingSleep = false;
            var duration = DateTime.UtcNow - _sleepStartTime;
            Console.WriteLine($"[STATE-SLEEP] Player woke up after {duration.TotalSeconds:F2}s");
        }

        public override void Update()
        {
            // Optionally render sleeping animation/effects
            if (_isRenderingSleep)
            {
                // This could trigger sleep particle effects, different sprite, etc.
            }
        }

        public override void HandleMovement(int dx, int dy)
        {
            // Silent - movement blocked during sleep but don't spam console
        }

        public override void HandleAttack(int aimX, int aimY)
        {
            Console.WriteLine($"[STATE-SLEEP] Cannot attack while sleeping");
        }

        public override void HandlePlant()
        {
            Console.WriteLine($"[STATE-SLEEP] Cannot plant while sleeping");
        }

        public override void HandleHarvest()
        {
            Console.WriteLine($"[STATE-SLEEP] Cannot harvest while sleeping");
        }

        public override void HandleSleepToggle()
        {
            Console.WriteLine($"[STATE-SLEEP] Waking up - transitioning to ActiveState");
            _stateManager.TransitionToActive();
        }

        public override void HandleTaskAssignment()
        {
            Console.WriteLine($"[STATE-SLEEP] Cannot accept tasks while sleeping");
        }

        /// <summary>
        /// Get the current sleep duration for rendering or logic purposes.
        /// </summary>
        public TimeSpan GetSleepDuration()
        {
            return DateTime.UtcNow - _sleepStartTime;
        }
    }
}
