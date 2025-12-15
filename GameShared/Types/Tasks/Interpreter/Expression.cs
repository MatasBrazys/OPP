namespace GameShared.Types.Tasks.Interpreter
{
    /// <summary>
    /// Abstract base class for the Interpreter pattern.
    /// Defines the interface for interpreting and executing task commands.
    /// </summary>
    public abstract class Expression
    {
        /// <summary>
        /// Interprets and executes the expression in the given context.
        /// </summary>
        public abstract void Interpret(InterpreterContext context);

        /// <summary>
        /// Gets a description of what this expression does.
        /// </summary>
        public abstract string GetDescription();
    }
}
