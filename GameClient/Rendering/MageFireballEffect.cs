using System;
using System.Drawing;

namespace GameClient.Rendering
{
    public class MageFireballEffect
    {
        public float TargetX { get; }
        public float TargetY { get; }
        public DateTime Start { get; }
        public const float DurationMs = 600f; // total drop animation
        private static readonly Image FireballSprite = Image.FromFile("../assets/fireball.png");

        public bool IsFinished => (DateTime.UtcNow - Start).TotalMilliseconds > DurationMs;

        public MageFireballEffect(float x, float y)
        {
            TargetX = x;
            TargetY = y;
            Start = DateTime.UtcNow;
        }

        public void Draw(Graphics g)
        {
            float elapsed = (float)(DateTime.UtcNow - Start).TotalMilliseconds;
            float t = Math.Clamp(elapsed / DurationMs, 0f, 1f);

            // Falling effect: start from above
            float startY = TargetY - 300; // 300px above
            float drawY = startY + (TargetY - startY) * t;
            float drawX = TargetX;

            // Optional: fade-in as it falls
            int alpha = (int)(255 * t);

            float scale = 1f + 0.2f * t; // slight scale increase

            var state = g.Save();
            g.TranslateTransform(drawX, drawY);

            using var attr = new System.Drawing.Imaging.ImageAttributes();
            var cm = new System.Drawing.Imaging.ColorMatrix
            {
                Matrix33 = alpha / 255f
            };
            attr.SetColorMatrix(cm);

            float width = FireballSprite.Width * scale;
            float height = FireballSprite.Height * scale;

            g.DrawImage(FireballSprite,
                new Rectangle((int)(-width / 2f), (int)(-height / 2f), (int)width, (int)height),
                0, 0, FireballSprite.Width, FireballSprite.Height,
                GraphicsUnit.Pixel,
                attr
            );

            g.Restore(state);
        }
    }
}
