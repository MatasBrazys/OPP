namespace GameShared.Types.DTOs;
public class TileJson
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Id { get; set; }
}

public class MapJson
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<TileJson> Tiles { get; set; } = new();
}
