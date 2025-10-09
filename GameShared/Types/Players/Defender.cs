using System.Drawing;
using GameShared.Strategies;

namespace GameShared.Types.Players
{
    public class Defender : PlayerRole
    {
        public Defender() : base(new NormalMovement())
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