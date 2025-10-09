using System.Drawing;
using GameShared.Strategies;

namespace GameShared.Types.Players
{
    public class Mage : PlayerRole
    {
        public Mage() : base(new NormalMovement())
        {
            Health = 5;
            RoleType = "Mage";
            RoleColor = Color.Blue;
        }

        public override void Attack()
        {
            // Magic attack implementation
        }

        public override void SpecialAbility()
        {
            // Mage special ability
        }
    }
}