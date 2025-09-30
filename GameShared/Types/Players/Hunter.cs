using System.Drawing;

namespace GameShared.Types.Players
{
    public class Hunter : PlayerRole
    {
        public Hunter()
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