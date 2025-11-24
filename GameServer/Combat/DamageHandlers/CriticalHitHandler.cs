namespace GameServer.Combat.DamageHandlers
{
    /// <summary>
    /// CHAIN OF RESPONSIBILITY - Concrete Handler
    /// Applies critical hit chance to damage
    /// </summary>
    public class CriticalHitHandler : BaseDamageHandler
    {
        private readonly double _critChance;
        private readonly double _critMultiplier;
        private readonly Random _random = new();

        public CriticalHitHandler(double critChance = 0.15, double critMultiplier = 2.0)
        {
            _critChance = critChance;
            _critMultiplier = critMultiplier;
        }

        protected override void ProcessDamage(DamageContext context)
        {
            if (_random.NextDouble() < _critChance)
            {
                int originalDamage = context.BaseDamage;
                context.BaseDamage = (int)(context.BaseDamage * _critMultiplier);
                int bonusDamage = context.BaseDamage - originalDamage;

                LogEffect(context, "CRITICAL HIT", 
                    $"Critical strike! +{bonusDamage} damage ({_critMultiplier}x multiplier)");
                
                Console.WriteLine($"💥 [CRIT] {context.Attacker.RoleType} landed a critical hit! " +
                    $"Damage: {originalDamage} → {context.BaseDamage}");
            }
        }
    }
}