// ./GameClient/Commands/WalkCommand.cs
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameShared.Messages;

public class WalkCommand : IGameCommand
{
    private readonly TcpClient client;
    private readonly int playerId;
    private readonly float dx;
    private readonly float dy;

    public WalkCommand(TcpClient client, int playerId, float dx, float dy)
    {
        this.client = client;
        this.playerId = playerId;
        this.dx = dx;
        this.dy = dy;
    }

    public void Execute()
    {
        var msg = new InputMessage
        {
            Type = "input",
            Dx = (int)Math.Round(dx),
            Dy = (int)Math.Round(dy)
        };

        var json = JsonSerializer.Serialize(msg) + "\n";
        var data = Encoding.UTF8.GetBytes(json);

        try { client.GetStream().Write(data, 0, data.Length); }
        catch { }
    }
}
