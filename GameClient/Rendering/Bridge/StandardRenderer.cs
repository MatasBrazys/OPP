// File: GameClient/Rendering/Bridge/StandardRenderer.cs
namespace GameClient.Rendering.Bridge
{
    /// <summary>
    /// BRIDGE PATTERN - Concrete Implementor
    /// Standard GDI+ rendering implementation
    /// </summary>
    public class StandardRenderer : IRenderer
    {
        public void DrawSprite(Graphics g, Image sprite, float x, float y, int width, int height)
        {
            g.DrawImage(sprite, x, y, width, height);
        }

        public void DrawText(Graphics g, string text, Font font, Color color, float x, float y)
        {
            using var brush = new SolidBrush(color);
            g.DrawString(text, font, brush, x, y);
        }

        public void DrawShape(Graphics g, Color color, float x, float y, float width, float height, ShapeType shape)
        {
            using var brush = new SolidBrush(color);
            
            switch (shape)
            {
                case ShapeType.Rectangle:
                    g.FillRectangle(brush, x, y, width, height);
                    break;
                case ShapeType.Circle:
                case ShapeType.Ellipse:
                    g.FillEllipse(brush, x, y, width, height);
                    break;
            }
        }

        public void DrawHealthBar(Graphics g, float x, float y, float width, float height, float percentage, Color color)
        {
            // Background
            using (var bgBrush = new SolidBrush(Color.DarkGray))
                g.FillRectangle(bgBrush, x, y, width, height);

            // Health bar
            using (var hpBrush = new SolidBrush(color))
                g.FillRectangle(hpBrush, x, y, width * percentage, height);

            // Border
            using (var pen = new Pen(Color.Black, 1))
                g.DrawRectangle(pen, x, y, width, height);
        }
    }
}