namespace GameShared.Types.Tasks.Interpreter.TerminalExpressions
{
    /// <summary>
    /// Non-terminal Expression: Represents a no-operation/invalid command
    /// Used when a command is not recognized.
    /// </summary>
    public class NoOpExpression : Expression
    {
        private readonly string _invalidInput;

        public NoOpExpression(string invalidInput)
        {
            _invalidInput = invalidInput;
        }

        public override void Interpret(InterpreterContext context)
        {
            Console.WriteLine($"? [INTERPRETER] Unknown command: '{_invalidInput}'");
            Console.WriteLine("?? Type 'help' for available commands\n");
        }

        public override string GetDescription()
        {
            return "No operation - invalid command";
        }
    }
}
