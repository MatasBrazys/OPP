namespace GameClient.States
{
    /// <summary>
    /// Interface for player states in the State design pattern.
    /// Each state encapsulates specific behavior and transition logic.
    /// </summary>
    public interface IPlayerState
    {
        /// <summary>
        /// Gets the name of the current state.
        /// </summary>
        string StateName { get; }

        /// <summary>
        /// Called when the player enters this state.
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called when the player exits this state.
        /// </summary>
        void OnExit();

        /// <summary>
        /// Called each frame while in this state.
        /// </summary>
        void Update();

        /// <summary>
        /// Handles movement input in this state.
        /// </summary>
        void HandleMovement(int dx, int dy);

        /// <summary>
        /// Handles attack input in this state.
        /// </summary>
        void HandleAttack(int aimX, int aimY);

        /// <summary>
        /// Handles planting action in this state.
        /// </summary>
        void HandlePlant();

        /// <summary>
        /// Handles harvesting action in this state.
        /// </summary>
        void HandleHarvest();

        /// <summary>
        /// Handles sleep toggle in this state (Z key).
        /// </summary>
        void HandleSleepToggle();

        /// <summary>
        /// Handles task assignment in this state.
        /// </summary>
        void HandleTaskAssignment();

        /// <summary>
        /// Handles task completion in this state.
        /// </summary>
        void HandleTaskCompletion();
    }
}
