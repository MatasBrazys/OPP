//./GameShared/Messages/PlantUpdateMessage.cs
namespace GameShared.Messages
{
    /// <summary>
    /// Message sent to client when a plant grows to a new stage
    /// </summary>
    public class PlantUpdateMessage : GameMessage
    {
        public override string Type => "plant_update";
        
        public int PlantId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Stage { get; set; }
        public string TileType { get; set; } = string.Empty;
        public string PlantType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Message sent to client when a new plant is planted
    /// </summary>
    public class PlantPlantedMessage : GameMessage
    {
        public override string Type => "plant_planted";
        
        public int PlantId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string PlantType { get; set; } = string.Empty;
        public string InitialTileType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Message sent to clients when a plant is harvested and should be removed from client-side lists.
    /// </summary>
    public class PlantHarvestedMessage : GameMessage
    {
        public override string Type => "plant_harvested";

        public int PlantId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string PlantType { get; set; } = string.Empty;
    }
}
