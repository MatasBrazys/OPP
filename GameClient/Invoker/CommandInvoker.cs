// ./GameClient/Invoker/CommandInvoker.cs
using System.Collections.Generic;

public class CommandInvoker
{
    private readonly List<IGameCommand> _commands = new();
    private readonly List<IGameCommand> _history = new();
    private const int MaxHistorySize = 50; // Limit history to prevent memory issues

    public bool UndoEnabled { get; set; } = false; // ✅ TOGGLEABLE

    public void AddCommand(IGameCommand command) => _commands.Add(command);

    public void ExecuteCommands()
    {
        foreach (var cmd in _commands)
        {
            cmd.Execute();

            // Only track in history if undo is enabled
            if (UndoEnabled)
            {
                _history.Add(cmd);

                // Trim history if too large
                if (_history.Count > MaxHistorySize)
                {
                    _history.RemoveAt(0);
                }
            }
        }
        _commands.Clear();
    }

    public void UndoLastCommand()
    {
        if (!UndoEnabled)
        {
            Console.WriteLine("[UNDO] Undo is disabled. Press F8 to enable.");
            return;
        }

        if (_history.Count == 0)
        {
            Console.WriteLine("[UNDO] No commands to undo");
            return;
        }

        var lastCommand = _history[_history.Count - 1];
        _history.RemoveAt(_history.Count - 1);

        lastCommand.Undo();
        Console.WriteLine($"[UNDO] Undid command: {lastCommand.GetType().Name}");
    }

    public void ClearHistory()
    {
        _history.Clear();
        Console.WriteLine("[UNDO] Command history cleared");
    }


}