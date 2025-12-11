// ./GameClient/Theming/WinterThemeFactory.cs
using System.Collections.Generic;
using System.Drawing;

namespace GameClient.Theming
{
    public class WinterTileSpriteSet : ITileSpriteSet
    {
        private readonly IReadOnlyDictionary<string, Image> _sprites = new Dictionary<string, Image>
        {
            ["Grass"] = ThemeSpriteLoader.LoadTileSprite("../assets/winter_grass.png", Color.FromArgb(210, 230, 240)),
            ["Tree"] = ThemeSpriteLoader.LoadTileSprite("../assets/winter_tree.png", Color.FromArgb(120, 150, 170)),
            ["House"] = ThemeSpriteLoader.LoadTileSprite("../assets/winter_house.png", Color.FromArgb(180, 190, 210)),
            ["Apple"] = ThemeSpriteLoader.LoadTileSprite("../assets/winter_apple.png", Color.FromArgb(220, 240, 255)),
            ["Fish"] = ThemeSpriteLoader.LoadTileSprite("../assets/winter_fish.png", Color.FromArgb(110, 150, 200)),
            ["Water"] = ThemeSpriteLoader.LoadTileSprite("../assets/winter_water.png", Color.FromArgb(90, 140, 200)),
            ["Sand"] = ThemeSpriteLoader.LoadTileSprite("../assets/winter_sand.png", Color.FromArgb(200, 210, 225)),
            ["Cherry"] = ThemeSpriteLoader.LoadTileSprite("../assets/winter_cherry.png", Color.FromArgb(215, 225, 245)),
            ["Wheat"] = ThemeSpriteLoader.LoadTileSprite("../assets/Wheat.png", Color.FromArgb(215, 225, 245)),
            ["WheatPlant"] = ThemeSpriteLoader.LoadTileSprite("../assets/WheatPlant.png", Color.FromArgb(180, 200, 170))

        };

        public IReadOnlyDictionary<string, Image> Sprites => _sprites;
    }

    public class WinterPlayerSpriteSet : IPlayerSpriteSet
    {
        private readonly IReadOnlyDictionary<string, Image> _sprites = new Dictionary<string, Image>
        {
            ["Mage"] = ThemeSpriteLoader.LoadPlayerSprite("../assets/winter_mage.png", Color.FromArgb(150, 170, 240)),
            ["Hunter"] = ThemeSpriteLoader.LoadPlayerSprite("../assets/winter_hunter.png", Color.FromArgb(140, 190, 220)),
            ["Defender"] = ThemeSpriteLoader.LoadPlayerSprite("../assets/winter_defender.png", Color.FromArgb(120, 150, 210))
        };

        public IReadOnlyDictionary<string, Image> Sprites => _sprites;
    }

    public class WinterEnemySpriteSet : IEnemySpriteSet
    {
        private readonly IReadOnlyDictionary<string, Image> _sprites = new Dictionary<string, Image>
        {
            ["Slime"] = ThemeSpriteLoader.LoadEnemySprite("../assets/winter_slime.png", Color.DarkSeaGreen)
        };

        public IReadOnlyDictionary<string, Image> Sprites => _sprites;
    }

    public class WinterPalette : IUiPalette
    {
        public Color GridLineColor => Color.FromArgb(180, Color.DarkSlateGray);
        public Color PlayerLabelColor => Color.FromArgb(230, Color.DarkSlateGray);
        public Color LocalPlayerRingColor => Color.FromArgb(150, Color.DeepSkyBlue);
    }

    public class WinterGameThemeFactory : IGameThemeFactory
    {
        public ITileSpriteSet CreateTileSpriteSet() => new WinterTileSpriteSet();

        public IPlayerSpriteSet CreatePlayerSpriteSet() => new WinterPlayerSpriteSet();

        public IEnemySpriteSet CreateEnemySpriteSet() => new WinterEnemySpriteSet();

        public IUiPalette CreateUiPalette() => new WinterPalette();
        //private static readonly Lazy<ITileSpriteSet> TileSet = new(() => new WinterTileSpriteSet());
        //private static readonly Lazy<IPlayerSpriteSet> PlayerSet = new(() => new WinterPlayerSpriteSet());
        //private static readonly Lazy<IEnemySpriteSet> EnemySet = new(() => new WinterEnemySpriteSet());
        //private static readonly Lazy<IUiPalette> Palette = new(() => new WinterPalette());

        //public ITileSpriteSet CreateTileSpriteSet() => TileSet.Value;
        //public IPlayerSpriteSet CreatePlayerSpriteSet() => PlayerSet.Value;
        //public IEnemySpriteSet CreateEnemySpriteSet() => EnemySet.Value;
        //public IUiPalette CreateUiPalette() => Palette.Value;
    }
}