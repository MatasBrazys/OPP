namespace GameShared.Types.Tasks.Interpreter.TerminalExpressions
{
    /// <summary>
    /// Terminal Expression: Display help information
    /// Command syntax: "help"
    /// </summary>
    public class HelpExpression : Expression
    {
        public override void Interpret(InterpreterContext context)
        {
            Console.WriteLine("\n??????????????????????????????????????????????????????????");
            Console.WriteLine("?         TASK INTERPRETER - AVAILABLE COMMANDS          ?");
            Console.WriteLine("??????????????????????????????????????????????????????????");
            Console.WriteLine("?                                                        ?");
            Console.WriteLine("?  plant <count> [type]                                  ?");
            Console.WriteLine("?    Create a plant task                                 ?");
            Console.WriteLine("?    Example: plant 5                                    ?");
            Console.WriteLine("?             plant 3 Wheat                              ?");
            Console.WriteLine("?                                                        ?");
            Console.WriteLine("?  collect <count> [itemType]                            ?");
            Console.WriteLine("?    Create a collect task                               ?");
            Console.WriteLine("?    Example: collect 10                                 ?");
            Console.WriteLine("?             collect 5 Goblin                           ?");
            Console.WriteLine("?                                                        ?");
            Console.WriteLine("?  list                                                  ?");
            Console.WriteLine("?    Show all active tasks                               ?");
            Console.WriteLine("?                                                        ?");
            Console.WriteLine("?  help                                                  ?");
            Console.WriteLine("?    Display this help message                           ?");
            Console.WriteLine("?                                                        ?");
            Console.WriteLine("?  exit                                                  ?");
            Console.WriteLine("?    Exit the interpreter                                ?");
            Console.WriteLine("?                                                        ?");
            Console.WriteLine("??????????????????????????????????????????????????????????\n");
        }

        public override string GetDescription()
        {
            return "Display help information about available commands";
        }
    }
}
