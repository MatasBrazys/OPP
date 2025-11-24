// File: GameClient/Rendering/EnemyRenderer.cs (MODIFIED VERSION)
using System;
using System.Drawing;
using GameShared;
using GameClient.Rendering.Bridge;

namespace GameClient.Rendering
{
    public class EnemyRenderer
    {
        public int Id { get; }
        public string EnemyType { get; }
        private float _prevX, _prevY;
        private float _targetX, _targetY;
        private DateTime _lastUpdateUtc;
        private  Image _sprite;
        private const double InterpolationMs = 150.0;
        public float CenterX => _targetX + 20;
        public float CenterY => _targetY + 20;
        public bool IsDead => CurrentHP <= 0;

        public int CurrentHP { get; set; }
        public int MaxHP { get; set; }

        // BRIDGE PATTERN: Use IRenderer
        private IRenderer _renderer;

        public EnemyRenderer(int id, string enemyType, int startX, int startY, Image sprite,
                           int currentHP, int maxHP, IRenderer? renderer = null)
        {
            Id = id;
            EnemyType = enemyType;
            _prevX = _targetX = startX;
            _prevY = _targetY = startY;
            _lastUpdateUtc = DateTime.UtcNow;
            _sprite = sprite;
            CurrentHP = currentHP;
            MaxHP = maxHP;

            // BRIDGE: Default to StandardRenderer
            _renderer = renderer ?? new StandardRenderer();
        }

        // BRIDGE: Allow changing renderer at runtime
        public void SetRenderer(IRenderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
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

            int size = GameConstants.ENEMY_SIZE;

            // BRIDGE: Use renderer for sprite
            _renderer.DrawSprite(g, _sprite, drawX, drawY, size, size);

            // Draw HP bar above enemy
            DrawHPBar(g, drawX, drawY - 5, size, 6);

            // Label (enemy name)
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 7, FontStyle.Bold);

            // BRIDGE: Use renderer for text
            _renderer.DrawText(g, $"{EnemyType} ({Id})", font, Color.Black, drawX, drawY - 25);
        }

        private void DrawHPBar(Graphics g, float x, float y, int width, int height)
        {
            float hpPercent = (float)CurrentHP / MaxHP;
            hpPercent = Math.Clamp(hpPercent, 0f, 1f);

            Color hpColor = hpPercent > 0.5f ? Color.LimeGreen :
                            hpPercent > 0.25f ? Color.Orange :
                            Color.Red;

            // BRIDGE: Use renderer for health bar
            _renderer.DrawHealthBar(g, x, y, width, height, hpPercent, hpColor);

            // Draw numeric HP text
            string hpText = $"{CurrentHP}/{MaxHP}";
            using var font = new Font(SystemFonts.DefaultFont.FontFamily, 6, FontStyle.Bold);
            SizeF textSize = g.MeasureString(hpText, font);

            float textX = x + (width - textSize.Width) / 2;
            float textY = y - 5;

            // BRIDGE: Use renderer for text
            _renderer.DrawText(g, hpText, font, Color.Black, textX, textY);
        }



    }
}