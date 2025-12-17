// File: GameClient/Rendering/PlayerRenderer.cs (MODIFIED - with animation)
using System;
using System.Drawing;
using GameShared;
using GameClient.Rendering.Bridge;
using GameClient.Animation;

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

        // ✨ NEW: Animation controller
        private readonly WalkAnimationController _animController;
        private float _lastDx = 0;
        private float _lastDy = 0;

        private IRenderer _renderer;

        // Collection text display with accumulation
        private Dictionary<string, int> _collectedAmounts = new Dictionary<string, int>();
        private string _currentDisplayPlantType = string.Empty;
        private DateTime _collectionTextShowUntil = DateTime.MinValue;

        public PlayerRenderer(int id, string role, int startX, int startY, Image sprite,
                            bool isLocalPlayer, Color labelColor, Color localPlayerRingColor,
                            IRenderer? renderer = null)
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
            _renderer = renderer ?? new StandardRenderer();

            // ✨ Initialize animation controller
            _animController = new WalkAnimationController();
        }

        /// <summary>
        /// Show collection text above the player for a specified duration.
        /// Accumulates amounts for the same plant type.
        /// </summary>
        public void ShowCollectionText(int amount, string plantType, double durationSeconds = 2.5)
        {
            // Add to accumulated amount for this plant type
            if (_collectedAmounts.ContainsKey(plantType))
            {
                _collectedAmounts[plantType] += amount;
            }
            else
            {
                _collectedAmounts[plantType] = amount;
            }

            _currentDisplayPlantType = plantType;
            _collectionTextShowUntil = DateTime.UtcNow.AddSeconds(durationSeconds);
        }

        public void SetRenderer(IRenderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public void UpdateTheme(Image sprite, Color labelColor, Color localPlayerRingColor)
        {
            // Keep static sprite as fallback but don't use it for theme changes
            // Animation sprites are loaded separately and should not be affected by theme
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

            // ✨ Calculate movement direction for animation
            float dx = _targetX - _prevX;
            float dy = _targetY - _prevY;
            _lastDx = dx;
            _lastDy = dy;
        }

        public (float X, float Y) Position
        {
            get
            {
                var elapsedMs = (DateTime.UtcNow - _lastUpdateUtc).TotalMilliseconds;
                var t = (float)Math.Clamp(elapsedMs / InterpolationMs, 0.0, 1.0);
                float drawX = _prevX + (_targetX - _prevX) * t;
                float drawY = _prevY + (_targetY - _prevY) * t;
                return (drawX + GameConstants.PLAYER_SIZE / 2f, drawY + GameConstants.PLAYER_SIZE / 2f);
            }
        }

        public void Draw(Graphics g)
        {
            var elapsedMs = (DateTime.UtcNow - _lastUpdateUtc).TotalMilliseconds;
            var t = (float)Math.Clamp(elapsedMs / InterpolationMs, 0.0, 1.0);
            float drawX = _prevX + (_targetX - _prevX) * t;
            float drawY = _prevY + (_targetY - _prevY) * t;

            // ✨ Update animation based on movement
            _animController.Update(_lastDx, _lastDy);

            // ✨ Get animated sprite
            string animatedSpriteName = _animController.GetCurrentSpriteName(Role);
            Image currentSprite = SpriteRegistry.GetSprite(animatedSpriteName);

            // Fallback to static sprite if animation not found
            if (currentSprite == null || currentSprite.Width == GameConstants.TILE_SIZE)
            {
                currentSprite = _sprite;
            }

            int playerSize = GameConstants.PLAYER_SIZE;

            // Draw animated sprite
            _renderer.DrawSprite(g, currentSprite, drawX, drawY, playerSize, playerSize);

            // Draw ID and role
            string label = $"{Role} (ID:{Id})";
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 8, FontStyle.Bold);
            _renderer.DrawText(g, label, font, _labelColor, drawX, drawY - 16);

            // Draw collection text if active (showing accumulated amount)
            if (DateTime.UtcNow < _collectionTextShowUntil && !string.IsNullOrEmpty(_currentDisplayPlantType))
            {
                if (_collectedAmounts.TryGetValue(_currentDisplayPlantType, out int totalAmount))
                {
                    string collectionText = $"{totalAmount} kg {_currentDisplayPlantType}";
                    using var collectionFont = new Font(SystemFonts.DefaultFont.FontFamily, 9, FontStyle.Bold);
                    _renderer.DrawText(g, collectionText, collectionFont, Color.Black, drawX, drawY - 32);
                }
            }

            // Draw range indicator for local player
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
                g.DrawEllipse(pen, centerX - radius, centerY - radius, radius * 2f, radius * 2f);
            }
        }
    }
}