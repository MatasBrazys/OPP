// File: GameClient/Rendering/Flyweight/SpriteLoader.cs

namespace GameClient.Rendering.Flyweight
{
    /// <summary>
    /// Utility for loading sprites with flyweight pattern
    /// </summary>
    public static class SpriteLoader
    {
        /// <summary>
        /// Loads a sprite from file using flyweight cache
        /// </summary>
        public static SpriteData LoadSprite(string filePath, Color? fallbackColor = null)
        {
            string key = Path.GetFileName(filePath);
            
            return SpriteCache.Instance.GetSprite(key, () =>
            {
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"📁 [FLYWEIGHT] Loading sprite from disk: {key}");
                    return Image.FromFile(filePath);
                }
                else
                {
                    Console.WriteLine($"⚠️ [FLYWEIGHT] Sprite not found, creating placeholder: {key}");
                    return CreatePlaceholder(fallbackColor ?? Color.Magenta);
                }
            });
        }

        /// <summary>
        /// Gets a sprite if already cached
        /// </summary>
        public static SpriteData? GetCachedSprite(string fileName)
        {
            return SpriteCache.Instance.GetSprite(fileName);
        }

        private static Image CreatePlaceholder(Color color)
        {
            var bmp = new Bitmap(64, 64);
            using var g = Graphics.FromImage(bmp);
            g.Clear(color);
            return bmp;
        }
    }
}