namespace GameShared.Types.Tasks.Interpreter.TerminalExpressions
{
    /// <summary>
    /// Terminal Expression: List all active tasks
    /// Command syntax: "list"
    /// </summary>
    public class ListTasksExpression : Expression
    {
        public override void Interpret(InterpreterContext context)
        {
            var activeTasks = context.TaskManager.GetActiveTasks();

            if (activeTasks.Count == 0)
            {
                Console.WriteLine("\n[INTERPRETER] No active tasks.");
                return;
            }

            Console.WriteLine("\n=== ACTIVE TASKS ===");
            foreach (var task in activeTasks)
            {
                Console.WriteLine($" - Task {task.Id}: {task.Description}");

                if (task is PlantTask plantTask)
                {
                    Console.WriteLine($"    Progress: {plantTask.GetProgress()}/{plantTask.GetRequired()}");
                }
                else if (task is CollectTask collectTask)
                {
                    Console.WriteLine($"    Progress: {collectTask.GetProgress()}/{collectTask.GetRequired()}");
                }
            }
            Console.WriteLine("====================\n");
        }

        public override string GetDescription()
        {
            return "List all active tasks";
        }
    }
}
