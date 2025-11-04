// ./GameClient/Theming/ThemeSpriteLoader.cs
using System.Drawing;
using System.IO;
using GameShared;

namespace GameClient.Theming
{
    internal static class ThemeSpriteLoader
    {
        public static Image LoadTileSprite(string path, Color placeholderColor)
        {
            return LoadSprite(path, GameConstants.TILE_SIZE, placeholderColor);
        }

        public static Image LoadPlayerSprite(string path, Color placeholderColor)
        {
            return LoadSprite(path, GameConstants.PLAYER_SIZE, placeholderColor);
        }

        public static Image LoadEnemySprite(string path, Color placeholderColor)
        {
            // Enemies render at player size today
            return LoadSprite(path, GameConstants.PLAYER_SIZE, placeholderColor);
        }

        private static Image LoadSprite(string path, int size, Color placeholderColor)
        {
            if (File.Exists(path))
            {
                return Image.FromFile(path);
            }

            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.Clear(placeholderColor);
            return bmp;
        }
    }
}