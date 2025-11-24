// ./gameclient/commands/AttackCommand.cs
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameClient.Networking;
using GameShared.Messages;

public class AttackCommand : IGameCommand
{
    private readonly ServerConnection _connection;
    private readonly int _playerId;
    private readonly float _clickX;
    private readonly float _clickY;
    private readonly string _attackType;

    public AttackCommand(ServerConnection connection, int playerId, float clickX, float clickY, string attackType = "slash")
    {
        _connection = connection;
        _playerId = playerId;
        _clickX = clickX;
        _clickY = clickY;
        _attackType = attackType;
    }

    public void Execute()
    {
        var msg = new AttackMessage
        {
            Type = "attack",
            PlayerId = _playerId,
            ClickX = _clickX,
            ClickY = _clickY,
            AttackType = _attackType
        };

        var json = JsonSerializer.Serialize(msg);
        _connection.SendRaw(json);
    }

    public void Undo()
    {
        // TODO: Implement attack undo
        // Possible approaches:
        // 1. Restore HP to damaged enemies (requires server-side undo support)
        // 2. Reverse animation (cosmetic only)
        // 3. Send "undo_attack" message to server with original attack data
        Console.WriteLine($"[UNDO] Attack undo not implemented (would reverse attack at {_clickX}, {_clickY})");
    }
}
