// File: GameClient/Rendering/Bridge/LabeledEntity.cs
namespace GameClient.Rendering.Bridge
{
    /// <summary>
    /// BRIDGE PATTERN - Refined Abstraction
    /// Entity with sprite and text label
    /// </summary>
    public class LabeledEntity : VisualEntity
    {
        private readonly Image _sprite;
        private readonly int _width;
        private readonly int _height;
        private readonly string _label;
        private readonly Font _font;
        private readonly Color _labelColor;

        public LabeledEntity(IRenderer renderer, Image sprite, int width, int height, 
                           string label, Font font, Color labelColor) 
            : base(renderer)
        {
            _sprite = sprite;
            _width = width;
            _height = height;
            _label = label;
            _font = font;
            _labelColor = labelColor;
        }

        public override void Draw(Graphics g)
        {
            _renderer.DrawSprite(g, _sprite, X, Y, _width, _height);
            _renderer.DrawText(g, _label, _font, _labelColor, X, Y - 16);
        }
    }
}