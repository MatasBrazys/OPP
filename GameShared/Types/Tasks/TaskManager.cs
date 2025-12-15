namespace GameShared.Types.Tasks
{
    /// <summary>
    /// Manages tasks for a player
    /// </summary>
    public class TaskManager
    {
        private List<ITask> _tasks = new();
        private Queue<ITask> _completedTasks = new();

        public event Action<ITask>? OnTaskCompleted;

        public void AddTask(ITask task)
        {
            if (task != null)
            {
                _tasks.Add(task);
                DisplayTaskAdded(task);
            }
        }

        public void RemoveTask(ITask task)
        {
            _tasks.Remove(task);
        }

        public List<ITask> GetActiveTasks()
        {
            return _tasks.Where(t => !t.IsCompleted).ToList();
        }

        public List<ITask> GetAllTasks()
        {
            return new List<ITask>(_tasks);
        }

        public ITask? GetTaskById(int taskId)
        {
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        public void Update()
        {
            var completedTasks = new List<ITask>();

            foreach (var task in _tasks)
            {
                if (!task.IsCompleted)
                {
                    task.OnUpdate();

                    if (task.IsCompleted && !_completedTasks.Contains(task))
                    {
                        completedTasks.Add(task);
                        _completedTasks.Enqueue(task);
                        OnTaskCompleted?.Invoke(task);
                    }
                }
            }
        }

        public void ClearCompletedTasks()
        {
            _completedTasks.Clear();
        }

        /// <summary>
        /// Display active tasks in console
        /// </summary>
        public void DisplayActiveTasks()
        {
            var activeTasks = GetActiveTasks();
            if (activeTasks.Count == 0)
            {
                Console.WriteLine("\n[TASKS] No active tasks.\n");
                return;
            }

            Console.WriteLine("ACTIVE TASKS");

            
            foreach (var task in activeTasks)
            {
                var taskInfo = task as PlantTask;
                if (taskInfo != null)
                {
                    var progress = taskInfo.GetProgress();
                    var required = taskInfo.GetRequired();
                    var progressBar = GenerateProgressBar(progress, required);
                    Console.WriteLine($"? Task {task.Id}: {task.Description}     ?");
                    Console.WriteLine($"? Progress: {progressBar} {progress}/{required}      ?");
                }
                else
                {
                    Console.WriteLine($"? Task {task.Id}: {task.Description}  ?");
                }
            }
            
        }

        private static void DisplayTaskAdded(ITask task)
        {
            Console.WriteLine($"NEW TASK: {task.Description.PadRight(27)}?");
        }

        private static string GenerateProgressBar(int current, int total)
        {
            const int barLength = 20;
            int filledLength = (int)((double)current / total * barLength);
            string bar = new string('?', filledLength) + new string('?', barLength - filledLength);
            return $"[{bar}]";
        }
    }
}
