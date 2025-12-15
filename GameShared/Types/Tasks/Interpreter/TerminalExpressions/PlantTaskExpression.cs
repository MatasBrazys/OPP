namespace GameShared.Types.Tasks.Interpreter.TerminalExpressions
{
    /// <summary>
    /// Terminal Expression: Create a new PlantTask
    /// Command syntax: "plant <count>"
    /// Example: "plant 5"
    /// </summary>
    public class PlantTaskExpression : Expression
    {
        private readonly int _count;
        private readonly string _plantType;

        public PlantTaskExpression(int count, string plantType = "Plant")
        {
            _count = count;
            _plantType = plantType;
        }

        public override void Interpret(InterpreterContext context)
        {
            if (_count <= 0)
            {
                Console.WriteLine("? [INTERPRETER] Plant count must be greater than 0");
                return;
            }

            int taskId = context.GetNextTaskId();
            var plantTask = new PlantTask(taskId, _count, _plantType);
            context.TaskManager.AddTask(plantTask);

            Console.WriteLine($"? [INTERPRETER] Created PlantTask: Plant and harvest {_count} {_plantType}");
        }

        public override string GetDescription()
        {
            return $"Create a PlantTask to plant and harvest {_count} {_plantType}";
        }
    }
}
