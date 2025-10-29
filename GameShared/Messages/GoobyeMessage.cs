

namespace GameShared.Messages
{
    public class GoodbyeMessage : GameMessage
    {
        public string Reason { get; set; } = "";
        public GoodbyeMessage() { Type = "goodbye"; }
    }
}
