using System;
using System.Drawing;

namespace GameClient.Rendering
{
    public class SlashEffect
    {
        public float X { get; }
        public float Y { get; }
        public float Radius { get; }
        public float RotationDeg { get; } // degrees
        public DateTime Start { get; }
        public const float DurationMs = 220f;

        public bool IsFinished => (DateTime.UtcNow - Start).TotalMilliseconds > DurationMs;

        public SlashEffect(float x, float y, float radius, float rotationDeg)
        {
            X = x;
            Y = y;
            Radius = radius;
            RotationDeg = rotationDeg;
            Start = DateTime.UtcNow;
        }

        public void Draw(Graphics g)
        {
            float elapsed = (float)(DateTime.UtcNow - Start).TotalMilliseconds;
            float t = Math.Clamp(elapsed / DurationMs, 0f, 1f);
            int alpha = (int)((1f - t) * 220);
            float length = Math.Min(Radius, 128f) * (1f + 0.25f * t);
            float width = GameShared.GameConstants.PLAYER_SIZE * (1f - 0.4f * t) + 8f;

            using var brush = new SolidBrush(Color.FromArgb(alpha, Color.OrangeRed));
            var state = g.Save();
            g.TranslateTransform(X, Y);
            g.RotateTransform(RotationDeg);

            // Draw elliptical slash oriented along X axis (rightwards)
            var rect = new RectangleF(-length / 2f, -width / 2f, length, width);
            g.FillEllipse(brush, rect);

            g.Restore(state);
        }
    }
}
