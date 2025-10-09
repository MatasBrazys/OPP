using System.Drawing;
using GameShared.Strategies;

namespace GameShared.Types.Players
{
    public class Hunter : PlayerRole
    {
        public Hunter() : base(new NormalMovement())
        {
            Health = 5;
            RoleType = "Hunter";
            RoleColor = Color.Brown;
        }

        public override void Attack()
        {
            // Bow attack implementation
        }

        public override void SpecialAbility()
        {
            // Hunter special ability
        }
    }
}