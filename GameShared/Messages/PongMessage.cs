

namespace GameShared.Messages
{
    public class PongMessage : GameMessage
    {
        public long T { get; set; }
        public PongMessage() { Type = "pong"; }
    }
}
