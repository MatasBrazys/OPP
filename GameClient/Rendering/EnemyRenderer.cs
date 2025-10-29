using System;
using System.Drawing;

namespace GameClient.Rendering
{
    public class EnemyRenderer
    {
        public int Id { get; }
        public string EnemyType { get; }

        private float _prevX, _prevY, _targetX, _targetY;
        private DateTime _lastUpdateUtc;
        private readonly Image _sprite;
        private const double InterpolationMs = 150.0;

        public EnemyRenderer(int id, string enemyType, int startX, int startY, Image sprite)
        {
            Id = id;
            EnemyType = enemyType;
            _prevX = _targetX = startX;
            _prevY = _targetY = startY;
            _sprite = sprite;
            _lastUpdateUtc = DateTime.UtcNow;
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

            g.DrawImage(_sprite, drawX, drawY, 40, 40); // slightly bigger than player
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold);
            g.DrawString($"{EnemyType} #{Id}", font, Brushes.Red, drawX, drawY - 16);
        }
    }
}
