
// File: GameClient/Rendering/SpriteRegistry.cs
using GameShared;
namespace GameClient.Rendering

{
    public static class SpriteRegistry
    {
        private static readonly Dictionary<string, Image> _sprites = new();
        private static readonly object SyncRoot= new();

        public static void Register(string spriteId, Image sprite)
        {
             lock (SyncRoot)
            {
                _sprites[spriteId] = sprite;
            }
        }

        public static void Clear()
        {
            lock (SyncRoot)
            {
                _sprites.Clear();
            }
        }

        public static Image GetSprite(string spriteId)
        {
            lock (SyncRoot)
            {
                if (_sprites.TryGetValue(spriteId, out var img))
                    return img;
            }

            // fallback to "Grass"
            if (_sprites.TryGetValue("Grass", out var fallback))
                return fallback;

            // last-resort blank image
            return new Bitmap( GameConstants.TILE_SIZE,  GameConstants.TILE_SIZE);
        }
    }
}

