namespace GameShared.Types.Tasks.Interpreter
{
    /// <summary>
    /// Context for the Interpreter pattern.
    /// Maintains the state needed for interpreting and executing task commands.
    /// </summary>
    public class InterpreterContext
    {
        private readonly TaskManager _taskManager;
        private int _nextTaskId = 1;

        public InterpreterContext(TaskManager taskManager)
        {
            _taskManager = taskManager ?? throw new ArgumentNullException(nameof(taskManager));
        }

        public TaskManager TaskManager => _taskManager;

        /// <summary>
        /// Gets the next available task ID.
        /// </summary>
        public int GetNextTaskId()
        {
            return _nextTaskId++;
        }

        /// <summary>
        /// Resets the task ID counter.
        /// </summary>
        public void ResetTaskIdCounter()
        {
            _nextTaskId = 1;
        }
    }
}
