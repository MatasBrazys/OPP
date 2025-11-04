// File: GameClient/Rendering/Bridge/SpriteEntity.cs
namespace GameClient.Rendering.Bridge
{
    /// <summary>
    /// BRIDGE PATTERN - Refined Abstraction
    /// Entity rendered with a sprite
    /// </summary>
    public class SpriteEntity : VisualEntity
    {
        private readonly Image _sprite;
        private readonly int _width;
        private readonly int _height;

        public SpriteEntity(IRenderer renderer, Image sprite, int width, int height) 
            : base(renderer)
        {
            _sprite = sprite;
            _width = width;
            _height = height;
        }

        public override void Draw(Graphics g)
        {
            _renderer.DrawSprite(g, _sprite, X, Y, _width, _height);
        }
    }
}