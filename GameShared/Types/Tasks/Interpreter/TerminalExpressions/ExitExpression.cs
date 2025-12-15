namespace GameShared.Types.Tasks.Interpreter.TerminalExpressions
{
    /// <summary>
    /// Terminal Expression: Exit the interpreter
    /// Command syntax: "exit"
    /// </summary>
    public class ExitExpression : Expression
    {
        public override void Interpret(InterpreterContext context)
        {
            Console.WriteLine("?? [INTERPRETER] Exiting task interpreter...");
            throw new OperationCanceledException("User requested exit");
        }

        public override string GetDescription()
        {
            return "Exit the interpreter";
        }
    }
}
