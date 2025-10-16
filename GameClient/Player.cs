using System;
using System.Drawing;

namespace GameClient
{

    public class Player
    {
        public int Id { get; }
        public Color Color { get; }
        private float PrevX;
        private float PrevY;
        private float TargetX;
        private float TargetY;

        private DateTime LastUpdateUtc;

        // How long (ms) we interpolate between Prev -> Target
        // You can tweak this (100..200 ms is common)
        private const double InterpolationMs = 150.0;

        public Player(int id, int x, int y, Color color)
        {
            Id = id;
            Color = color;

            PrevX = TargetX = x;
            PrevY = TargetY = y;
            LastUpdateUtc = DateTime.UtcNow;
        }

       
        public void SetTarget(int x, int y)
        {

            PrevX = TargetX;
            PrevY = TargetY;

            TargetX = x;
            TargetY = y;
            LastUpdateUtc = DateTime.UtcNow;
        }


        public void SnapTo(int x, int y)
        {
            PrevX = TargetX = x;
            PrevY = TargetY = y;
            LastUpdateUtc = DateTime.UtcNow;
        }


        public void Draw(Graphics g)
        {

            var elapsedMs = (DateTime.UtcNow - LastUpdateUtc).TotalMilliseconds;
            var t = InterpolationMs <= 0 ? 1.0f : (float)Math.Clamp(elapsedMs / InterpolationMs, 0.0, 1.0);

            float drawX = PrevX + (TargetX - PrevX) * t;
            float drawY = PrevY + (TargetY - PrevY) * t;

            const int size = 30;
            using var brush = new SolidBrush(Color);
            g.FillEllipse(brush, drawX, drawY, size, size);
            g.DrawString($"ID:{Id}", SystemFonts.DefaultFont, Brushes.Black, drawX, drawY - 16);
        }
        public (float x, float y) GetInterpolatedPosition()
        {
            var elapsedMs = (DateTime.UtcNow - LastUpdateUtc).TotalMilliseconds;
            var t = InterpolationMs <= 0 ? 1.0f : (float)Math.Clamp(elapsedMs / InterpolationMs, 0.0, 1.0);
            float drawX = PrevX + (TargetX - PrevX) * t;
            float drawY = PrevY + (TargetY - PrevY) * t;
            return (drawX, drawY);
        }
    }
}
