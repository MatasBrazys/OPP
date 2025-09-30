using System.Drawing;

namespace GameShared.Types.Players
{
    public class Defender : PlayerRole
    {
        public Defender()
        {
            Health = 5;
            RoleType = "Defender";
            RoleColor = Color.Green;
        }

        public override void Attack()
        {
            // Melee attack implementation
        }

        public override void SpecialAbility()
        {
            // Defender special ability
        }
    }
}