namespace GameShared.Types.Tasks.Interpreter.TerminalExpressions
{
    /// <summary>
    /// Terminal Expression: Create a new CollectTask
    /// Command syntax: "collect <count>" or "collect <count> <itemType>"
    /// Example: "collect 10" or "collect 10 Enemy"
    /// </summary>
    public class CollectTaskExpression : Expression
    {
        private readonly int _count;
        private readonly string _itemType;

        public CollectTaskExpression(int count, string itemType = "Enemy")
        {
            _count = count;
            _itemType = itemType;
        }

        public override void Interpret(InterpreterContext context)
        {
            if (_count <= 0)
            {
                Console.WriteLine("? [INTERPRETER] Collect count must be greater than 0");
                return;
            }

            int taskId = context.GetNextTaskId();
            var collectTask = new CollectTask(taskId, _count, _itemType);
            context.TaskManager.AddTask(collectTask);

            Console.WriteLine($"? [INTERPRETER] Created CollectTask: Defeat {_count} {_itemType}");
        }

        public override string GetDescription()
        {
            return $"Create a CollectTask to collect {_count} {_itemType}";
        }
    }
}
