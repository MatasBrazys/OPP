// ./GameClient/Commands/AttackCommand.cs
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameShared.Messages;

public class AttackCommand : IGameCommand
{
    private readonly TcpClient client;
    private readonly int playerId;
    private readonly string attackType;
    private readonly int targetX;
    private readonly int targetY;

    public AttackCommand(TcpClient client, int playerId, string attackType, int targetX, int targetY)
    {
        this.client = client;
        this.playerId = playerId;
        this.attackType = attackType;
        this.targetX = targetX;
        this.targetY = targetY;
    }

    public void Execute()
    {
        var msg = new AttackMessage
        {
            PlayerId = playerId,
            AttackType = attackType,
            TargetX = targetX,
            TargetY = targetY
        };

        var json = JsonSerializer.Serialize(msg) + "\n";
        var data = Encoding.UTF8.GetBytes(json);

        try { client.GetStream().Write(data, 0, data.Length); }
        catch { /* handle errors */ }
    }
}
