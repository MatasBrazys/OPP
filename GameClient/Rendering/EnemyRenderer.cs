using System;
using System.Drawing;

namespace GameClient.Rendering
{
    public class EnemyRenderer
    {
        public int Id { get; }
        public string EnemyType { get; }
        private float _prevX, _prevY;
        private float _targetX, _targetY;
        private DateTime _lastUpdateUtc;
        private readonly Image _sprite;
        private const double InterpolationMs = 150.0;
        public float CenterX => _targetX + 20; // assuming sprite size 40
        public float CenterY => _targetY + 20;
        public bool IsDead => CurrentHP <= 0;


        // HP data
        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }

        public EnemyRenderer(int id, string enemyType, int startX, int startY, Image sprite, int currentHP, int maxHP)
        {
            Id = id;
            EnemyType = enemyType;
            _prevX = _targetX = startX;
            _prevY = _targetY = startY;
            _lastUpdateUtc = DateTime.UtcNow;
            _sprite = sprite;
            CurrentHP = currentHP;
            MaxHP = maxHP ;
        }

        public void SetTarget(int x, int y)
        {
            _prevX = _targetX;
            _prevY = _targetY;
            _targetX = x;
            _targetY = y;
            _lastUpdateUtc = DateTime.UtcNow;
        }

        public void Draw(Graphics g)
        {
            var elapsedMs = (DateTime.UtcNow - _lastUpdateUtc).TotalMilliseconds;
            var t = (float)Math.Clamp(elapsedMs / InterpolationMs, 0.0, 1.0);
            float drawX = _prevX + (_targetX - _prevX) * t;
            float drawY = _prevY + (_targetY - _prevY) * t;

            int size = 40;
            g.DrawImage(_sprite, drawX, drawY, size, size);

            // Draw HP bar above enemy
            DrawHPBar(g, drawX, drawY-5, size, 6);

            // Label (enemy name)
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 7, FontStyle.Bold);
            using var brush = new SolidBrush(Color.Black);
            g.DrawString($"{EnemyType} ({Id})", font, brush, drawX, drawY - 25);
        }

        private void DrawHPBar(Graphics g, float x, float y, int width, int height)
        {
            float hpPercent = (float)CurrentHP / MaxHP;
            hpPercent = Math.Clamp(hpPercent, 0f, 1f);

            // Background (gray)
            using (var bgBrush = new SolidBrush(Color.DarkGray))
                g.FillRectangle(bgBrush, x, y, width, height);

            // HP bar (green → red)
            Color hpColor = hpPercent > 0.5f ? Color.LimeGreen :
                            hpPercent > 0.25f ? Color.Orange :
                            Color.Red;

            using (var hpBrush = new SolidBrush(hpColor))
                g.FillRectangle(hpBrush, x, y, width * hpPercent, height);

            // Border
            using (var pen = new Pen(Color.Black, 1))
                g.DrawRectangle(pen, x, y, width, height);

            // 👇 Draw numeric HP text
            string hpText = $"{CurrentHP}/{MaxHP}";
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 6, FontStyle.Bold);
            SizeF textSize = g.MeasureString(hpText, font);

            float textX = x + (width - textSize.Width) / 2;
            float textY = y - 5; // a bit above center

            using var textBrush = new SolidBrush(Color.Black);
            g.DrawString(hpText, font, textBrush, textX, textY);
        }
        public void TakeDamage(int amount)
            {
                CurrentHP = Math.Max(0, CurrentHP - amount);
            }

    }
}
