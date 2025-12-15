//./GameShared/Messages/HarvestActionMessage.cs
namespace GameShared.Messages
{
    /// <summary>
    /// Message sent by client when attempting to harvest a plant on a tile
    /// </summary>
    public class HarvestActionMessage : GameMessage
    {
        public int PlayerId { get; set; }
        public int TileX { get; set; }
        public int TileY { get; set; }

        public HarvestActionMessage()
        {
            Type = "harvest_action";
        }
    }
}
