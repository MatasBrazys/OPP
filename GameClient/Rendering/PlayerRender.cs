using System;
using System.Drawing;

namespace GameClient.Rendering
{
    public class PlayerRenderer
    {
        public int Id { get; }
        public string Role { get; }
        private float _prevX, _prevY;
        private float _targetX, _targetY;
        private DateTime _lastUpdateUtc;
        private readonly Image _sprite;
        private readonly bool _isLocalPlayer;
        private const double InterpolationMs = 150.0;

        public PlayerRenderer(int id, string role, int startX, int startY, Image sprite, bool isLocalPlayer)
        {
            Id = id;
            Role = role;
            _prevX = _targetX = startX;
            _prevY = _targetY = startY;
            _lastUpdateUtc = DateTime.UtcNow;
            _sprite = sprite;
            _isLocalPlayer = isLocalPlayer;
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

            
            // Draw sprite
            int playerSize = 40;
            g.DrawImage(_sprite, drawX, drawY, playerSize, playerSize);

            // Draw ID and role above player
            string label = $"{Role} (ID:{Id})";
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold);
            using var brush = new SolidBrush(Color.Black);
            g.DrawString(label, font, brush, drawX, drawY - 16);
        }
    }
}
