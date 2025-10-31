//./GameShared/Messages/MapStateMessage.cs
namespace GameShared.Messages
{
    public class MapStateMessage : GameMessage
    {
        public override string Type => "map_state";
        public int Width { get; set; }
        public int Height { get; set; }
        public List<MapTileDto> Tiles { get; set; } = new();
    }

    public class MapTileDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string TileType { get; set; } = string.Empty;
    }
}
