// File: GameServer/Combat/DamageHandlers/WeaknessHandler.cs
namespace GameServer.Combat.DamageHandlers
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY - Concrete Handler
    /// Applies type effectiveness bonuses
    /// </summary>
    public class WeaknessHandler : BaseDamageHandler
    {
        private readonly Dictionary<(string role, string enemy), double> _weaknessTable = new()
        {
            { ("Mage", "Slime"), 1.5 },      // Mage is strong vs Slime
            { ("Hunter", "Slime"), 1.2 },    // Hunter is moderately strong vs Slime
            { ("Defender", "Slime"), 0.9 }   // Defender is slightly weak vs Slime
        };

        protected override void ProcessDamage(DamageContext context)
        {
            var key = (context.Attacker.RoleType, context.Target.EnemyType);

            if (_weaknessTable.TryGetValue(key, out double multiplier) && multiplier != 1.0)
            {
                int originalDamage = context.BaseDamage;
                context.BaseDamage = (int)(context.BaseDamage * multiplier);
                int difference = context.BaseDamage - originalDamage;

                string effectType = multiplier > 1.0 ? "SUPER EFFECTIVE" : "NOT VERY EFFECTIVE";
                string sign = difference > 0 ? "+" : "";

                LogEffect(context, effectType, 
                    $"{context.Attacker.RoleType} vs {context.Target.EnemyType}: {sign}{difference} damage ({multiplier}x)");
                
                Console.WriteLine($"⚡ [{effectType}] {context.Attacker.RoleType} vs {context.Target.EnemyType}: " +
                    $"{originalDamage} → {context.BaseDamage} ({multiplier}x)");
            }
        }
    }
}