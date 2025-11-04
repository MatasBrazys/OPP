// File: GameClient/Rendering/Bridge/AntiAliasedRenderer.cs
namespace GameClient.Rendering.Bridge
{
    /// <summary>
    /// BRIDGE PATTERN - Concrete Implementor
    /// High-quality anti-aliased rendering implementation
    /// </summary>
    public class AntiAliasedRenderer : IRenderer
    {
        public void DrawSprite(Graphics g, Image sprite, float x, float y, int width, int height)
        {
            var oldMode = g.InterpolationMode;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(sprite, x, y, width, height);
            g.InterpolationMode = oldMode;
        }

        public void DrawText(Graphics g, string text, Font font, Color color, float x, float y)
        {
            var oldMode = g.TextRenderingHint;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            
            using var brush = new SolidBrush(color);
            g.DrawString(text, font, brush, x, y);
            
            g.TextRenderingHint = oldMode;
        }

        public void DrawShape(Graphics g, Color color, float x, float y, float width, float height, ShapeType shape)
        {
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            
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
            
            g.SmoothingMode = oldMode;
        }

        public void DrawHealthBar(Graphics g, float x, float y, float width, float height, float percentage, Color color)
        {
            var oldMode = g.SmoothingMode;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Background with rounded corners
            using (var bgBrush = new SolidBrush(Color.DarkGray))
            {
                var bgPath = CreateRoundedRectangle(x, y, width, height, 2);
                g.FillPath(bgBrush, bgPath);
            }

            // Health bar with rounded corners
            if (percentage > 0)
            {
                using (var hpBrush = new SolidBrush(color))
                {
                    var hpPath = CreateRoundedRectangle(x, y, width * percentage, height, 2);
                    g.FillPath(hpBrush, hpPath);
                }
            }

            // Border
            using (var pen = new Pen(Color.Black, 1))
            {
                var borderPath = CreateRoundedRectangle(x, y, width, height, 2);
                g.DrawPath(pen, borderPath);
            }

            g.SmoothingMode = oldMode;
        }

        private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectangle(float x, float y, float width, float height, float radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, radius * 2, radius * 2, 180, 90);
            path.AddArc(x + width - radius * 2, y, radius * 2, radius * 2, 270, 90);
            path.AddArc(x + width - radius * 2, y + height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(x, y + height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}