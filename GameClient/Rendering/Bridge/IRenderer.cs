// File: GameClient/Rendering/Bridge/IRenderer.cs
namespace GameClient.Rendering.Bridge
{
    /// <summary>
    /// BRIDGE PATTERN - Implementor Interface
    /// Defines the interface for rendering implementations
    /// </summary>
    public interface IRenderer
    {
        void DrawSprite(Graphics g, Image sprite, float x, float y, int width, int height);
        void DrawText(Graphics g, string text, Font font, Color color, float x, float y);
        void DrawShape(Graphics g, Color color, float x, float y, float width, float height, ShapeType shape);
        void DrawHealthBar(Graphics g, float x, float y, float width, float height, float percentage, Color color);
    }

    public enum ShapeType
    {
        Rectangle,
        Circle,
        Ellipse
    }
}