
// File: GameClient/Rendering/SpriteRegistry.cs
using GameShared;
namespace GameClient.Rendering

{
    public static class SpriteRegistry
    {
        private static readonly Dictionary<string, Image> _sprites = new();

        public static void Register(string spriteId, Image sprite)
        {
            _sprites[spriteId] = sprite;
        }

        public static Image GetSprite(string spriteId)
        {
            if (_sprites.TryGetValue(spriteId, out var img))
                return img;

            // fallback to "Grass"
            if (_sprites.TryGetValue("Grass", out var fallback))
                return fallback;

            // last-resort blank image
            return new Bitmap( GameConstants.TILE_SIZE,  GameConstants.TILE_SIZE);
        }
    }
}

