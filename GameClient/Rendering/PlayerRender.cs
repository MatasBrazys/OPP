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
        private Image _sprite;
        private readonly bool _isLocalPlayer;
        private Color _labelColor;
        private Color _localPlayerRingColor;
        private const double InterpolationMs = 100.0;

        public PlayerRenderer(int id, string role, int startX, int startY, Image sprite, bool isLocalPlayer, Color labelColor, Color localPlayerRingColor)
        {
            Id = id;
            Role = role;
            _prevX = _targetX = startX;
            _prevY = _targetY = startY;
            _lastUpdateUtc = DateTime.UtcNow;
            _sprite = sprite;
            _isLocalPlayer = isLocalPlayer;
            _labelColor = labelColor;
            _localPlayerRingColor = localPlayerRingColor;
        }

        public void UpdateTheme(Image sprite, Color labelColor, Color localPlayerRingColor)
        {
            _sprite = sprite;
            _labelColor = labelColor;
            _localPlayerRingColor = localPlayerRingColor;
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
                var t = (float)Math.Clamp(elapsedMs / InterpolationMs, 0.0, 1.0);
                float drawX = _prevX + (_targetX - _prevX) * t;
                float drawY = _prevY + (_targetY - _prevY) * t;
                return (drawX + GameConstants.PLAYER_SIZE / 2f, drawY + GameConstants.PLAYER_SIZE / 2f); // return center
            }
        }

        public void Draw(Graphics g)
        {
            var elapsedMs = (DateTime.UtcNow - _lastUpdateUtc).TotalMilliseconds;
            var t = (float)Math.Clamp(elapsedMs / InterpolationMs, 0.0, 1.0);
            float drawX = _prevX + (_targetX - _prevX) * t;
            float drawY = _prevY + (_targetY - _prevY) * t;

            int playerSize = GameConstants.PLAYER_SIZE;
            g.DrawImage(_sprite, drawX, drawY, playerSize, playerSize);

            // Draw ID and role
            string label = $"{Role} (ID:{Id})";
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold);
            using var brush = new SolidBrush(_labelColor);
            g.DrawString(label, font, brush, drawX, drawY - 16);

            // Inside Draw()
            if (_isLocalPlayer)
            {
                float centerX = drawX + playerSize / 2f;
                float centerY = drawY + playerSize / 2f;
                float radius = GameConstants.TILE_SIZE;
                Color color = Color.Blue;

                switch (Role.ToLower())
                {
                    case "mage":
                        radius = GameConstants.TILE_SIZE * 2f;
                        color = Color.Purple;
                        break;
                    case "hunter":
                        radius = GameConstants.TILE_SIZE * 3f;
                        color = Color.Yellow;
                        break;
                    case "defender":
                        radius = GameConstants.TILE_SIZE;
                        color = Color.Blue;
                        break;
                }

                using var pen = new Pen(_localPlayerRingColor, 1.5f);
                float centerX = drawX + playerSize / 2f;
                float centerY = drawY + playerSize / 2f;
                g.DrawEllipse(pen, centerX - radius, centerY - radius, radius * 2f, radius * 2f);
            }

        }
    }
}
