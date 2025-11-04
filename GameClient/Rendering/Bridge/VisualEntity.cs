// File: GameClient/Rendering/Bridge/VisualEntity.cs
namespace GameClient.Rendering.Bridge
{
    /// <summary>
    /// BRIDGE PATTERN - Abstraction
    /// Base class for all visual entities that can be rendered
    /// </summary>
    public abstract class VisualEntity
    {
        protected IRenderer _renderer;
        
        public float X { get; set; }
        public float Y { get; set; }

        protected VisualEntity(IRenderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public void SetRenderer(IRenderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public abstract void Draw(Graphics g);
    }
}