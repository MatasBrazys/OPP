using System.Drawing;

namespace GameShared.Types.Players
{
    public class Mage : PlayerRole
    {
        public Mage()
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