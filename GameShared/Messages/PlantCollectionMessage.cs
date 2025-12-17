namespace GameShared.Messages
{
    /// <summary>
    /// Message sent to client when a player collects a plant.
    /// Contains information about how much was collected and the plant type.
    /// </summary>
    public class PlantCollectionMessage : GameMessage
    {
        public override string Type => "plant_collection";

        public int PlayerId { get; set; }
        public int Amount { get; set; }  // Amount in kg
        public string PlantType { get; set; } = string.Empty;  // "Wheat", "Carrot", or "Potato"
    }
}
