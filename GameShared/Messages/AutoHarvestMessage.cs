//./GameShared/Messages/AutoHarvestMessage.cs
namespace GameShared.Messages
{
    /// <summary>
    /// Message sent by client to request server-side auto-harvest of all mature plants
    /// Server will handle all logic and send back tile update messages
    /// </summary>
    public class AutoHarvestMessage : GameMessage
    {
        public override string Type { get; set; } = "auto_harvest";
        public int PlayerId { get; set; }
    }
}
