// File: GameClient/Rendering/Bridge/DebugRenderer.cs
namespace GameClient.Rendering.Bridge
{
    /// <summary>
    /// BRIDGE PATTERN - Concrete Implementor
    /// Debug rendering with wireframes and collision boxes
    /// </summary>
    public class DebugRenderer : IRenderer
    {
        public void DrawSprite(Graphics g, Image sprite, float x, float y, int width, int height)
        {
            // Draw wireframe box instead of sprite
            using var pen = new Pen(Color.Cyan, 2);
            g.DrawRectangle(pen, x, y, width, height);
            
            // Draw diagonal cross
            g.DrawLine(pen, x, y, x + width, y + height);
            g.DrawLine(pen, x + width, y, x, y + height);
            
            // Draw center point
            float cx = x + width / 2f;
            float cy = y + height / 2f;
            g.FillEllipse(Brushes.Red, cx - 2, cy - 2, 4, 4);
        }

        public void DrawText(Graphics g, string text, Font font, Color color, float x, float y)
        {
            // Draw text with debug background
            using var brush = new SolidBrush(color);
            using var bgBrush = new SolidBrush(Color.FromArgb(128, Color.Black));
            
            var size = g.MeasureString(text, font);
            g.FillRectangle(bgBrush, x, y, size.Width, size.Height);
            g.DrawString(text, font, brush, x, y);
        }

        public void DrawShape(Graphics g, Color color, float x, float y, float width, float height, ShapeType shape)
        {
            // Draw wireframe only
            using var pen = new Pen(color, 2);
            
            switch (shape)
            {
                case ShapeType.Rectangle:
                    g.DrawRectangle(pen, x, y, width, height);
                    break;
                case ShapeType.Circle:
                case ShapeType.Ellipse:
                    g.DrawEllipse(pen, x, y, width, height);
                    break;
            }
        }

        public void DrawHealthBar(Graphics g, float x, float y, float width, float height, float percentage, Color color)
        {
            // Simple debug health bar
            using (var pen = new Pen(Color.White, 1))
                g.DrawRectangle(pen, x, y, width, height);
            
            using (var brush = new SolidBrush(Color.FromArgb(128, color)))
                g.FillRectangle(brush, x, y, width * percentage, height);
            
            // Draw percentage text
            string pct = $"{percentage * 100:F0}%";
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 6);
            DrawText(g, pct, font, Color.White, x + 2, y - 10);
        }
    }
}