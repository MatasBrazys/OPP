//./GameShared/Messages/CopyMadeMessage.cs
namespace GameShared.Messages
{
    public class CopyMadeMessage : GameMessage
    {
        public override string Type => "copy_made";
        public int OriginalPlayerId { get; set; }
        public int NewPlayerId { get; set; }
        public string OriginalRole { get; set; } = string.Empty;
        public string NewRole { get; set; } = string.Empty;
        public string CopyType { get; set; } = string.Empty; 
    }
}
