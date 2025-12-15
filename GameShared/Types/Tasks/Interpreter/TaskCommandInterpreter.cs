using GameShared.Types.Tasks.Interpreter;

namespace GameShared.Types.Tasks.Interpreter
{
    /// <summary>
    /// Interpreter Client for the task command interpreter.
    /// This is the main interface for using the interpreter pattern to parse and execute task commands.
    /// </summary>
    public class TaskCommandInterpreter
    {
        private readonly CommandParser _parser;
        private readonly InterpreterContext _context;

        public TaskCommandInterpreter(TaskManager taskManager)
        {
            _context = new InterpreterContext(taskManager);
            _parser = new CommandParser();
        }

        /// <summary>
        /// Executes a single command and returns the expression that was interpreted.
        /// </summary>
        public Expression ExecuteCommand(string input)
        {
            var expression = _parser.Parse(input);
            expression.Interpret(_context);
            return expression;
        }

        /// <summary>
        /// Runs the interpreter in interactive console mode.
        /// Continuously reads commands from the console until the user exits.
        /// </summary>
        public void RunInteractiveMode()
        {
            Console.WriteLine("\n??????????????????????????????????????????????????????????");
            Console.WriteLine("?       GAME TASK INTERPRETER - INTERACTIVE MODE        ?");
            Console.WriteLine("?         Type 'help' for available commands            ?");
            Console.WriteLine("??????????????????????????????????????????????????????????\n");

            try
            {
                while (true)
                {
                    Console.Write("task> ");
                    string? input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        continue;
                    }

                    try
                    {
                        ExecuteCommand(input);
                    }
                    catch (OperationCanceledException)
                    {
                        break; // Exit the loop on exit command
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? [INTERPRETER] An error occurred: {ex.Message}");
            }

            Console.WriteLine("\n? [INTERPRETER] Task interpreter closed\n");
        }

        /// <summary>
        /// Executes a batch of commands from a list of strings.
        /// Useful for testing or scripting.
        /// </summary>
        public void ExecuteBatch(IEnumerable<string> commands)
        {
            foreach (var command in commands)
            {
                if (string.IsNullOrWhiteSpace(command))
                    continue;

                Console.WriteLine($"Executing: {command}");
                try
                {
                    ExecuteCommand(command);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Gets the interpreter context (useful for advanced usage).
        /// </summary>
        public InterpreterContext GetContext()
        {
            return _context;
        }
    }
}
