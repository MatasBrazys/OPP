namespace GameShared.Types.Map
{
    public interface IMap
    {
        int Width { get; }
        int Height { get; }

        TileData GetTile(int x, int y);
        void SetTile(int x, int y, TileData newTile);

        void LoadFromText(string filePath);
        void LoadFromDimensions(int width, int height);
    }
}
