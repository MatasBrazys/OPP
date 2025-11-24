// File: GameServer/Combat/DamageHandlers/DefenseHandler.cs
namespace GameServer.Combat.DamageHandlers
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY - Concrete Handler
    /// Applies enemy defense reduction
    /// </summary>
    public class DefenseHandler : BaseDamageHandler
    {
        private readonly double _defenseReduction;

        public DefenseHandler(double defenseReduction = 0.1)
        {
            _defenseReduction = defenseReduction;
        }

        protected override void ProcessDamage(DamageContext context)
        {
            int originalDamage = context.BaseDamage;
            int reduction = (int)(originalDamage * _defenseReduction);
            context.BaseDamage = Math.Max(1, originalDamage - reduction); // Minimum 1 damage

            LogEffect(context, "DEFENSE", 
                $"{context.Target.EnemyType} defense reduced damage by {reduction}");
            
            Console.WriteLine($"🛡️ [DEFENSE] Enemy defense: {originalDamage} → {context.BaseDamage} (-{reduction})");
        }
    }
}