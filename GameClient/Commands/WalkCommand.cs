// ./GameClient/Commands/WalkCommand.cs
using System.Data;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameClient.Networking;
using GameShared.Messages;

public class WalkCommand : IGameCommand
{
    private readonly ServerConnection connection;
    private readonly int playerId;
    private readonly float dx;
    private readonly float dy;

    public WalkCommand(ServerConnection connection, int playerId, float dx, float dy)
    {
        this.connection = connection ;
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

        var json = JsonSerializer.Serialize(msg);
        connection.SendRaw(json);
       
    }
}
