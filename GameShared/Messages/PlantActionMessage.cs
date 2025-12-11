//./GameShared/Messages/PlantActionMessage.cs
namespace GameShared.Messages
{
    /// <summary>
    /// Message sent by client when attempting to plant on a tile
    /// </summary>
    public class PlantActionMessage : GameMessage
    {
        public int PlayerId { get; set; }
        public int TileX { get; set; }
        public int TileY { get; set; }
        public string PlantType { get; set; } = "Wheat";

        public PlantActionMessage()
        {
            Type = "plant_action";
        }
    }
}
