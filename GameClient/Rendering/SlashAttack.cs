using System;
using System.Drawing;

namespace GameClient.Rendering
{
    public class SlashEffect
    {
        public float X { get; }
        public float Y { get; }
        public float Size { get; }
        private readonly float Rotation;
        private readonly DateTime StartTime;
        private const float DurationMs = 200f;

        public bool IsFinished => (DateTime.UtcNow - StartTime).TotalMilliseconds > DurationMs;

        public SlashEffect(float x, float y, float size, float rotationDeg)
        {
            X = x;
            Y = y;
            Size = size;
            Rotation = rotationDeg;
            StartTime = DateTime.UtcNow;
        }

        public void Draw(Graphics g)
        {
            float progress = (float)Math.Clamp((DateTime.UtcNow - StartTime).TotalMilliseconds / DurationMs, 0, 1);
            int alpha = (int)((1 - progress) * 255);
            float drawSize = Size * (1 + 0.5f * progress);

            using var brush = new SolidBrush(Color.FromArgb(alpha, Color.OrangeRed));

            g.TranslateTransform(X, Y);
            g.RotateTransform(Rotation);
            g.FillEllipse(brush, -drawSize / 2, -drawSize / 4, drawSize, drawSize / 2); // elongated slash
            g.ResetTransform();
        }
    }
}
