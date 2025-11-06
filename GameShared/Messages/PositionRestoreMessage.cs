// ./GameShared/Messages/PositionRestoreMessage.cs
namespace GameShared.Messages
{
    public class PositionRestoreMessage : GameMessage
    {
        public int PlayerId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public PositionRestoreMessage() { Type = "position_restore"; }
    }
}