// ./gameclient/commands/igamecommand.cs
public interface IGameCommand
{
    void Execute();
    void Undo(); 
}
