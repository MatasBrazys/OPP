using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace GameClient.Rendering
{
    public class SlashEffect
    {
        public float X { get; }
        public float Y { get; }
        public float Radius { get; }
        public float RotationDeg { get; }
        public DateTime Start { get; }
        public const float DurationMs = 220f;

        private static readonly Image SlashSprite = Image.FromFile("../assets/slash.png");

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
            float progress = Math.Clamp((float)(DateTime.UtcNow - Start).TotalMilliseconds / DurationMs, 0f, 1f);
            int alpha = (int)((1f - progress) * 220);
            float scale = 2f + 0.25f * progress;

            int drawWidth = (int)(120f * scale);   // length along slash
            int drawHeight = (int)(54f * scale);   // thickness

            using var attr = new ImageAttributes();
            attr.SetColorMatrix(new ColorMatrix { Matrix33 = alpha / 255f });

            var state = g.Save();
            g.TranslateTransform(X, Y);
            g.RotateTransform(RotationDeg);

            // Draw sprite centered on X/Y
            g.DrawImage(
                SlashSprite,
                new Rectangle(-drawWidth / 2, -drawHeight / 2, drawWidth, drawHeight),
                0, 0, SlashSprite.Width, SlashSprite.Height,
                GraphicsUnit.Pixel,
                attr
            );

            g.Restore(state);
        }
    }
}
