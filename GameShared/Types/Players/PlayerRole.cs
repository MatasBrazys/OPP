using System.Drawing;

namespace GameShared.Types.Players
{
    // Description for abstract factory pattern
    public abstract class PlayerRole : PlayerState
    {
        public int Health { get; protected set; } = 5;
        public string RoleType { get; protected set; }
        public Color RoleColor { get; protected set; }
        
        public abstract void Attack();
        public abstract void SpecialAbility();
    }
}