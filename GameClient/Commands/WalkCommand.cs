// ./GameClient/Commands/WalkCommand.cs
using System.Text.Json;
using GameClient.Networking;
using GameShared.Messages;

public class WalkCommand : IGameCommand
{
    private readonly ServerConnection _connection;
    private readonly int _playerId;
    private readonly int _dx;
    private readonly int _dy;
    private readonly int _previousX;
    private readonly int _previousY;

    public WalkCommand(ServerConnection connection, int playerId, float dx, float dy, int currentX, int currentY)
    {
        _connection = connection;
        _playerId = playerId;
        _dx = (int)Math.Round(dx);
        _dy = (int)Math.Round(dy);

        // Store position BEFORE movement (the position before sending input)
        _previousX = currentX;
        _previousY = currentY;
    }

    public void Execute()
    {
        var msg = new InputMessage
        {
            Type = "input",
            Dx = _dx,
            Dy = _dy
        };
        _connection.SendRaw(JsonSerializer.Serialize(msg));
    }

    public void Undo()
    {
        var msg = new PositionRestoreMessage
        {
            Type = "position_restore",
            PlayerId = _playerId,
            X = _previousX,
            Y = _previousY
        };
        _connection.SendRaw(JsonSerializer.Serialize(msg));
        Console.WriteLine($"[UNDO] Requested undo for player {_playerId} → {_previousX},{_previousY}");
    }


}
