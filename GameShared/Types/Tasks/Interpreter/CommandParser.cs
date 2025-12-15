using GameShared.Types.Tasks.Interpreter.TerminalExpressions;

namespace GameShared.Types.Tasks.Interpreter
{
    /// <summary>
    /// Parser for the Interpreter pattern.
    /// Converts user input strings into Expression objects.
    /// This is the key component that parses commands and constructs the appropriate expressions.
    /// </summary>
    public class CommandParser
    {
        /// <summary>
        /// Parses user input and returns the corresponding Expression.
        /// </summary>
        public Expression Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new NoOpExpression("empty input");
            }

            var tokens = input.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            if (tokens.Length == 0)
            {
                return new NoOpExpression("empty input");
            }

            var command = tokens[0].ToLower();

            return command switch
            {
                "plant" => ParsePlantCommand(tokens),
                "collect" => ParseCollectCommand(tokens),
                "list" => new ListTasksExpression(),
                "help" => new HelpExpression(),
                "exit" => new ExitExpression(),
                _ => new NoOpExpression(input)
            };
        }

        /// <summary>
        /// Parses the "plant" command.
        /// Syntax: plant <count> [plantType]
        /// </summary>
        private Expression ParsePlantCommand(string[] tokens)
        {
            if (tokens.Length < 2)
            {
                Console.WriteLine("? [INTERPRETER] Invalid plant command. Usage: plant <count> [plantType]");
                return new NoOpExpression("plant");
            }

            if (!int.TryParse(tokens[1], out int count))
            {
                Console.WriteLine($"? [INTERPRETER] '{tokens[1]}' is not a valid number");
                return new NoOpExpression($"plant {tokens[1]}");
            }

            string plantType = tokens.Length > 2 ? tokens[2] : "Plant";
            return new PlantTaskExpression(count, plantType);
        }

        /// <summary>
        /// Parses the "collect" command.
        /// Syntax: collect <count> [itemType]
        /// </summary>
        private Expression ParseCollectCommand(string[] tokens)
        {
            if (tokens.Length < 2)
            {
                Console.WriteLine("? [INTERPRETER] Invalid collect command. Usage: collect <count> [itemType]");
                return new NoOpExpression("collect");
            }

            if (!int.TryParse(tokens[1], out int count))
            {
                Console.WriteLine($"? [INTERPRETER] '{tokens[1]}' is not a valid number");
                return new NoOpExpression($"collect {tokens[1]}");
            }

            string itemType = tokens.Length > 2 ? tokens[2] : "Enemy";
            return new CollectTaskExpression(count, itemType);
        }
    }
}
