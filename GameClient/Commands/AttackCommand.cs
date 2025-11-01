using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameShared.Messages;

public class AttackCommand : IGameCommand
{
    private readonly TcpClient _client;
    private readonly int _playerId;
    private readonly float _clickX;
    private readonly float _clickY;
    private readonly string _attackType;

    public AttackCommand(TcpClient client, int playerId, float clickX, float clickY, string attackType = "slash")
    {
        _client = client;
        _playerId = playerId;
        _clickX = clickX;
        _clickY = clickY;
        _attackType = attackType;
    }

    public void Execute()
    {
        var msg = new AttackMessage
        {
            PlayerId = _playerId,
            ClickX = _clickX,
            ClickY = _clickY,
            AttackType = _attackType
        };

        var json = JsonSerializer.Serialize(msg) + "\n";
        var data = Encoding.UTF8.GetBytes(json);
        try { _client.GetStream().Write(data, 0, data.Length); }
        catch { /* ignore socket errors here */ }
    }
}
