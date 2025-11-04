// ./GameClient/Theming/SummerThemeFactory.cs
using System.Collections.Generic;
using System.Drawing;

namespace GameClient.Theming
{
    public class SummerTileSpriteSet : ITileSpriteSet
    {
        private readonly IReadOnlyDictionary<string, Image> _sprites = new Dictionary<string, Image>
        {
            ["Grass"] = ThemeSpriteLoader.LoadTileSprite("../assets/grass.png", Color.ForestGreen),
            ["Tree"] = ThemeSpriteLoader.LoadTileSprite("../assets/tree.png", Color.DarkGreen),
            ["House"] = ThemeSpriteLoader.LoadTileSprite("../assets/house.png", Color.SaddleBrown),
            ["Apple"] = ThemeSpriteLoader.LoadTileSprite("../assets/apple.png", Color.Red),
            ["Fish"] = ThemeSpriteLoader.LoadTileSprite("../assets/fish.png", Color.SteelBlue),
            ["Water"] = ThemeSpriteLoader.LoadTileSprite("../assets/water.png", Color.RoyalBlue),
            ["Sand"] = ThemeSpriteLoader.LoadTileSprite("../assets/sand.png", Color.Khaki),
            ["Cherry"] = ThemeSpriteLoader.LoadTileSprite("../assets/cherry.jpg", Color.IndianRed)
        };

        public IReadOnlyDictionary<string, Image> Sprites => _sprites;
    }

    public class SummerPlayerSpriteSet : IPlayerSpriteSet
    {
        private readonly IReadOnlyDictionary<string, Image> _sprites = new Dictionary<string, Image>
        {
            ["Mage"] = ThemeSpriteLoader.LoadPlayerSprite("../assets/mage.png", Color.MediumPurple),
            ["Hunter"] = ThemeSpriteLoader.LoadPlayerSprite("../assets/hunter.png", Color.DarkOliveGreen),
            ["Defender"] = ThemeSpriteLoader.LoadPlayerSprite("../assets/defender.png", Color.DarkSlateBlue)
        };

        public IReadOnlyDictionary<string, Image> Sprites => _sprites;
    }

    public class SummerEnemySpriteSet : IEnemySpriteSet
    {
        private readonly IReadOnlyDictionary<string, Image> _sprites = new Dictionary<string, Image>
        {
            ["Slime"] = ThemeSpriteLoader.LoadEnemySprite("../assets/slime.png", Color.DarkSeaGreen)
        };

        public IReadOnlyDictionary<string, Image> Sprites => _sprites;
    }

    public class SummerPalette : IUiPalette
    {
        public Color GridLineColor => Color.FromArgb(180, Color.Black);
        public Color PlayerLabelColor => Color.FromArgb(230, Color.Black);
        public Color LocalPlayerRingColor => Color.FromArgb(120, Color.Blue);
    }

    public class SummerGameThemeFactory : IGameThemeFactory
    {
        public ITileSpriteSet CreateTileSpriteSet() => new SummerTileSpriteSet();

        public IPlayerSpriteSet CreatePlayerSpriteSet() => new SummerPlayerSpriteSet();

        public IEnemySpriteSet CreateEnemySpriteSet() => new SummerEnemySpriteSet();

        public IUiPalette CreateUiPalette() => new SummerPalette();
    }
}