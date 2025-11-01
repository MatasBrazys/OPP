using System;
using System.Drawing;
using GameShared;

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
        private const double InterpolationMs = 100.0;

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

        public (float X, float Y) Position
        {
            get
            {
                var elapsedMs = (DateTime.UtcNow - _lastUpdateUtc).TotalMilliseconds;
                var t = (float)Math.Clamp(elapsedMs / InterpolationMs, 0, 1);
                return (_prevX + (_targetX - _prevX) * t, _prevY + (_targetY - _prevY) * t);
            }
        }

        public void Draw(Graphics g)
        {
            var (drawX, drawY) = Position;

            // Draw sprite
            int playerSize = GameConstants.PLAYER_SIZE;
            g.DrawImage(_sprite, drawX, drawY, playerSize, playerSize);

            // Draw range circle (debug)
            using var pen = new Pen(Color.Red, 1);
            g.DrawEllipse(pen, drawX + playerSize / 2 - GameConstants.TILE_SIZE, drawY + playerSize / 2 - GameConstants.TILE_SIZE,
                          GameConstants.TILE_SIZE * 2, GameConstants.TILE_SIZE * 2);

            // Draw label
            string label = $"{Role} (ID:{Id})";
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold);
            using var brush = new SolidBrush(Color.Black);
            g.DrawString(label, font, brush, drawX, drawY - 16);
        }
    }
}
