// File: GameClient/Rendering/CursorRenderer.cs
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace GameClient.Rendering
{
    /// <summary>
    /// Renders a crosshair cursor for gamepad aiming
    /// Shows only when using gamepad input
    /// </summary>
    public class CursorRenderer
    {
        private Point _position;
        private bool _visible;
        private readonly Color _primaryColor = Color.FromArgb(255, 0, 255, 0); // Bright green
        private readonly Color _outlineColor = Color.FromArgb(200, 0, 0, 0);   // Dark outline
        private readonly int _size = 20;
        private readonly int _thickness = 2;

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        public Point Position
        {
            get => _position;
            set => _position = value;
        }

        public void Draw(Graphics g)
        {
            if (!_visible) return;

            var oldMode = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cx = _position.X;
            int cy = _position.Y;
            int halfSize = _size / 2;

            // Draw outline (black)
            using (var outlinePen = new Pen(_outlineColor, _thickness + 2))
            {
                // Horizontal line
                g.DrawLine(outlinePen, cx - halfSize, cy, cx + halfSize, cy);
                // Vertical line
                g.DrawLine(outlinePen, cx, cy - halfSize, cx, cy + halfSize);
                
                // Center circle outline
                g.DrawEllipse(outlinePen, cx - 3, cy - 3, 6, 6);
            }

            // Draw main crosshair (green)
            using (var mainPen = new Pen(_primaryColor, _thickness))
            {
                // Horizontal line
                g.DrawLine(mainPen, cx - halfSize, cy, cx + halfSize, cy);
                // Vertical line
                g.DrawLine(mainPen, cx, cy - halfSize, cx, cy + halfSize);
                
                // Center dot
                using (var centerBrush = new SolidBrush(_primaryColor))
                {
                    g.FillEllipse(centerBrush, cx - 2, cy - 2, 4, 4);
                }
            }

            // Optional: Add corner brackets for a more tactical look
            int bracketSize = 8;
            int bracketOffset = halfSize + 2;
            using (var bracketPen = new Pen(_primaryColor, 1))
            {
                // Top-left
                g.DrawLine(bracketPen, cx - bracketOffset, cy - bracketOffset, 
                          cx - bracketOffset + bracketSize, cy - bracketOffset);
                g.DrawLine(bracketPen, cx - bracketOffset, cy - bracketOffset, 
                          cx - bracketOffset, cy - bracketOffset + bracketSize);
                
                // Top-right
                g.DrawLine(bracketPen, cx + bracketOffset, cy - bracketOffset, 
                          cx + bracketOffset - bracketSize, cy - bracketOffset);
                g.DrawLine(bracketPen, cx + bracketOffset, cy - bracketOffset, 
                          cx + bracketOffset, cy - bracketOffset + bracketSize);
                
                // Bottom-left
                g.DrawLine(bracketPen, cx - bracketOffset, cy + bracketOffset, 
                          cx - bracketOffset + bracketSize, cy + bracketOffset);
                g.DrawLine(bracketPen, cx - bracketOffset, cy + bracketOffset, 
                          cx - bracketOffset, cy + bracketOffset - bracketSize);
                
                // Bottom-right
                g.DrawLine(bracketPen, cx + bracketOffset, cy + bracketOffset, 
                          cx + bracketOffset - bracketSize, cy + bracketOffset);
                g.DrawLine(bracketPen, cx + bracketOffset, cy + bracketOffset, 
                          cx + bracketOffset, cy + bracketOffset - bracketSize);
            }

            g.SmoothingMode = oldMode;
        }
    }
}