// File: GameServer/Combat/DamageHandlers/RoleBonusHandler.cs
namespace GameServer.Combat.DamageHandlers
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY - Concrete Handler
    /// Applies role-specific bonuses based on attack type
    /// </summary>
    public class RoleBonusHandler : BaseDamageHandler
    {
        protected override void ProcessDamage(DamageContext context)
        {
            // Mages deal extra damage with fireball
            if (context.Attacker.RoleType == "Mage" && context.AttackType == "fireball")
            {
                int originalDamage = context.BaseDamage;
                context.BaseDamage = (int)(context.BaseDamage * 1.1);
                int bonus = context.BaseDamage - originalDamage;

                LogEffect(context, "MAGE MASTERY", 
                    $"Mage fireball expertise: +{bonus} damage");
                
                Console.WriteLine($"🔥 [MAGE MASTERY] Fireball expertise: +{bonus} damage");
            }
            // Hunters deal extra damage at long range (arrows)
            else if (context.Attacker.RoleType == "Hunter" && context.AttackType == "arrow")
            {
                int originalDamage = context.BaseDamage;
                context.BaseDamage = (int)(context.BaseDamage * 1.15);
                int bonus = context.BaseDamage - originalDamage;

                LogEffect(context, "MARKSMAN", 
                    $"Hunter precision: +{bonus} damage");
                
                Console.WriteLine($"🎯 [MARKSMAN] Precision shot: +{bonus} damage");
            }
            // Defenders deal consistent damage with slashes
            else if (context.Attacker.RoleType == "Defender" && context.AttackType == "slash")
            {
                int originalDamage = context.BaseDamage;
                context.BaseDamage = (int)(context.BaseDamage * 1.05);
                int bonus = context.BaseDamage - originalDamage;

                LogEffect(context, "WARRIOR SPIRIT", 
                    $"Defender fortitude: +{bonus} damage");
                
                Console.WriteLine($"⚔️ [WARRIOR SPIRIT] Fortitude: +{bonus} damage");
            }
        }
    }
}