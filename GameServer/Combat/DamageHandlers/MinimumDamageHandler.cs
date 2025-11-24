// File: GameServer/Combat/DamageHandlers/MinimumDamageHandler.cs
namespace GameServer.Combat.DamageHandlers
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY - Concrete Handler
    /// Ensures damage never goes below minimum threshold (end of chain)
    /// </summary>
    public class MinimumDamageHandler : BaseDamageHandler
    {
        private readonly int _minimumDamage;

        public MinimumDamageHandler(int minimumDamage = 1)
        {
            _minimumDamage = minimumDamage;
        }

        protected override void ProcessDamage(DamageContext context)
        {
            if (context.BaseDamage < _minimumDamage)
            {
                int originalDamage = context.BaseDamage;
                context.BaseDamage = _minimumDamage;

                LogEffect(context, "MINIMUM DAMAGE", 
                    $"Damage capped at minimum: {originalDamage} → {_minimumDamage}");
                
                Console.WriteLine($"⚠️ [MIN DAMAGE] Damage too low, set to minimum: {_minimumDamage}");
            }
        }
    }
}