//./GameShared/Messages/PingMessage.cs
namespace GameShared.Messages
{
    public class PingMessage : GameMessage
    {
        public long T { get; set; }
        public PingMessage() { Type = "ping"; }
    }
}
