// File: GameClient/Rendering/Bridge/HealthBarEntity.cs
namespace GameClient.Rendering.Bridge
{
    /// <summary>
    /// BRIDGE PATTERN - Refined Abstraction
    /// Entity with sprite and health bar
    /// </summary>
    public class HealthBarEntity : VisualEntity
    {
        private readonly Image _sprite;
        private readonly int _width;
        private readonly int _height;
        private float _currentHealth;
        private float _maxHealth;

        public float CurrentHealth 
        { 
            get => _currentHealth; 
            set => _currentHealth = Math.Clamp(value, 0, _maxHealth); 
        }

        public float MaxHealth 
        { 
            get => _maxHealth; 
            set => _maxHealth = Math.Max(1, value); 
        }

        public HealthBarEntity(IRenderer renderer, Image sprite, int width, int height, 
                              float currentHealth, float maxHealth) 
            : base(renderer)
        {
            _sprite = sprite;
            _width = width;
            _height = height;
            _maxHealth = Math.Max(1, maxHealth);
            _currentHealth = Math.Clamp(currentHealth, 0, _maxHealth);
        }

        public override void Draw(Graphics g)
        {
            _renderer.DrawSprite(g, _sprite, X, Y, _width, _height);
            
            float percentage = _currentHealth / _maxHealth;
            Color barColor = percentage > 0.5f ? Color.LimeGreen :
                           percentage > 0.25f ? Color.Orange :
                           Color.Red;

            _renderer.DrawHealthBar(g, X, Y - 5, _width, 6, percentage, barColor);
        }
    }
}