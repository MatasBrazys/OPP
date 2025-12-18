using GameShared.Types.Tasks;

namespace GameClient.States
{
    /// <summary>
    /// Working state: Player is performing a task (e.g., plant 3 wheat).
    /// - CAN move while working (needed for tasks like planting)
    /// - CAN plant/harvest while working (to complete task requirements)
    /// - Cannot attack
    /// - Cannot sleep
    /// - Automatically transitions back to ActiveState when task requirement is fulfilled
    /// - Can be cancelled by pressing Q key
    /// </summary>
    public class WorkingState : PlayerStateBase
    {
        private DateTime _workStartTime;
        private PlantTask? _currentTask;
        private bool _isWorking;

        public override string StateName => "Working";

        /// <summary>
        /// Gets whether the player is currently working (for rendering purposes).
        /// </summary>
        public bool IsWorking => _isWorking;

        /// <summary>
        /// Gets the name of the current task being performed.
        /// </summary>
        public string CurrentTaskName => _currentTask?.Description ?? "No Task";

        /// <summary>
        /// Gets the current task (for external progress tracking).
        /// </summary>
        public PlantTask? CurrentTask => _currentTask;

        /// <summary>
        /// Gets the progress of the current work (0.0 to 1.0).
        /// </summary>
        public float WorkProgress
        {
            get
            {
                if (!_isWorking || _currentTask == null) return 0f;
                int required = _currentTask.GetRequired();
                if (required <= 0) return 0f;
                return Math.Min((float)_currentTask.GetProgress() / required, 1f);
            }
        }

        public WorkingState(PlayerStateManager stateManager) : base(stateManager)
        {
            _currentTask = null;
            _isWorking = false;
        }

        /// <summary>
        /// Assigns a task to this working state. Call before transitioning to WorkingState.
        /// </summary>
        /// <param name="task">The task to perform.</param>
        public void AssignTask(PlantTask task)
        {
            _currentTask = task;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            _workStartTime = DateTime.UtcNow;
            _isWorking = true;
            Console.WriteLine($"[STATE-WORKING] Started task: {CurrentTaskName}");
        }

        public override void OnExit()
        {
            base.OnExit();
            _isWorking = false;
            var duration = DateTime.UtcNow - _workStartTime;
            Console.WriteLine($"[STATE-WORKING] Exited after {duration.TotalSeconds:F2}s");
            _currentTask = null;
        }

        public override void Update()
        {
            // Check if task is completed (requirement fulfilled)
            if (_currentTask != null && _currentTask.IsCompleted)
            {
                Console.WriteLine($"[STATE-WORKING] Task completed! Returning to ActiveState");
                _stateManager.TransitionToActive();
            }
        }

        public override void HandleMovement(int dx, int dy)
        {
            // Allow movement while working (needed for tasks like planting/harvesting)
        }

        public override void HandleAttack(int aimX, int aimY)
        {
            // Silent - don't spam console
        }

        public override void HandlePlant()
        {
            // Allow planting while working - this helps complete the task!
            // The actual planting logic is handled by GameClientForm
        }

        public override void HandleHarvest()
        {
            // Allow harvesting while working - this completes the task requirement!
            // The actual harvest logic is handled by GameClientForm
        }

        /// <summary>
        /// Called when a harvest action is performed. Updates task progress.
        /// </summary>
        public void OnHarvestPerformed()
        {
            if (_currentTask != null && !_currentTask.IsCompleted)
            {
                _currentTask.OnHarvestPlant();
                Console.WriteLine($"[STATE-WORKING] Progress: {_currentTask.GetProgress()}/{_currentTask.GetRequired()}");
            }
        }

        public override void HandleSleepToggle()
        {
            // Silent - don't spam console
        }

        public override void HandleTaskAssignment()
        {
            Console.WriteLine($"[STATE-WORKING] Already working on: {CurrentTaskName}");
        }

        public override void HandleTaskCompletion()
        {
            Console.WriteLine($"[STATE-WORKING] Task cancelled, returning to ActiveState");
            _stateManager.TransitionToActive();
        }

        /// <summary>
        /// Gets the remaining progress for the current task.
        /// </summary>
        public (int current, int required) GetTaskProgress()
        {
            if (_currentTask == null) return (0, 0);
            return (_currentTask.GetProgress(), _currentTask.GetRequired());
        }
    }
}
