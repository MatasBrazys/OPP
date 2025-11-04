// ./GameClient/Theming/IGameThemeFactory.cs
using System.Collections.Generic;
using System.Drawing;

namespace GameClient.Theming
{
    public interface ITileSpriteSet
    {
        IReadOnlyDictionary<string, Image> Sprites { get; }
    }

    public interface IPlayerSpriteSet
    {
        IReadOnlyDictionary<string, Image> Sprites { get; }
    }

    public interface IEnemySpriteSet
    {
        IReadOnlyDictionary<string, Image> Sprites { get; }
    }

    public interface IUiPalette
    {
        Color GridLineColor { get; }
        Color PlayerLabelColor { get; }
        Color LocalPlayerRingColor { get; }
    }

    public interface IGameThemeFactory
    {
        ITileSpriteSet CreateTileSpriteSet();
        IPlayerSpriteSet CreatePlayerSpriteSet();
        IEnemySpriteSet CreateEnemySpriteSet();
        IUiPalette CreateUiPalette();
    }
}