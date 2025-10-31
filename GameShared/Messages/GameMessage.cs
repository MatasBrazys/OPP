//./GameShared/Messages/GameMessage.cs

namespace GameShared.Messages
{
    public abstract class GameMessage
    {
        public virtual string Type { get; set; } = "";
        public int V { get; set; } = 1;
    }

}
