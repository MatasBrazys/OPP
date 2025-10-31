// ./GameClient/Invoker/CommandInvoker.cs
using System.Collections.Generic;

public class CommandInvoker
{
    private readonly List<IGameCommand> _commands = new();

    public void AddCommand(IGameCommand command) => _commands.Add(command);

    public void ExecuteCommands()
    {
        foreach (var cmd in _commands) cmd.Execute();
        _commands.Clear();
    }
}
