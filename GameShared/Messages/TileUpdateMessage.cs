
//./GameShared/Messages/TileUpdateMessage.cs
namespace GameShared.Messages
{
    public class TileUpdateMessage : GameMessage
    {
        public override string Type => "tile_update";
        public int X { get; set; }
        public int Y { get; set; }
        public string TileType { get; set; } = string.Empty;
    }
}
