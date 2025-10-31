using System.Drawing;
using System;

namespace GameClient.Rendering
{
    public class SlashEffect
    {
        public float X { get; }
        public float Y { get; }
        public float Size { get; }
        public DateTime StartTime { get; }
        public const float DurationMs = 200; // animation lasts 200ms
        public bool IsFinished => (DateTime.UtcNow - StartTime).TotalMilliseconds > DurationMs;

        public SlashEffect(float x, float y, float size)
        {
            X = x;
            Y = y;
            Size = size;
            StartTime = DateTime.UtcNow;
        }

        public void Draw(Graphics g)
        {
            float progress = (float)Math.Clamp((DateTime.UtcNow - StartTime).TotalMilliseconds / DurationMs, 0, 1);
            int alpha = (int)((1 - progress) * 255); // fade out
            using var brush = new SolidBrush(Color.FromArgb(alpha, Color.OrangeRed));
            float drawSize = Size * (1 + 0.5f * progress); // slight scale-up effect
            g.FillEllipse(brush, X - drawSize / 2, Y - drawSize / 2, drawSize, drawSize);
        }
    }
}
