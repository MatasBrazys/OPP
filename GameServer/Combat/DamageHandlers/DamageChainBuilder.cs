// File: GameServer/Combat/DamageHandlers/DamageChainBuilder.cs
namespace GameServer.Combat.DamageHandlers
{
    /// <summary>
    /// Builder for constructing the damage calculation chain
    /// Provides fluent API for chain configuration
    /// </summary>
    public class DamageChainBuilder
    {
        private readonly List<IDamageHandler> _handlers = new();

        public DamageChainBuilder WithCriticalHits(double chance = 0.15, double multiplier = 2.0)
        {
            _handlers.Add(new CriticalHitHandler(chance, multiplier));
            return this;
        }

        public DamageChainBuilder WithWeaknesses()
        {
            _handlers.Add(new WeaknessHandler());
            return this;
        }

        public DamageChainBuilder WithDefense(double reduction = 0.1)
        {
            _handlers.Add(new DefenseHandler(reduction));
            return this;
        }

        public DamageChainBuilder WithRoleBonuses()
        {
            _handlers.Add(new RoleBonusHandler());
            return this;
        }

        public DamageChainBuilder WithMinimumDamage(int minimum = 1)
        {
            _handlers.Add(new MinimumDamageHandler(minimum));
            return this;
        }

        public DamageChainBuilder WithCustomHandler(IDamageHandler handler)
        {
            _handlers.Add(handler);
            return this;
        }

        public IDamageHandler Build()
        {
            if (_handlers.Count == 0)
            {
                throw new InvalidOperationException("Chain must have at least one handler");
            }

            // Link handlers together
            for (int i = 0; i < _handlers.Count - 1; i++)
            {
                _handlers[i].SetNext(_handlers[i + 1]);
            }

            // Return first handler (head of chain)
            return _handlers[0];
        }

        /// <summary>
        /// Creates a standard damage chain with all common handlers
        /// </summary>
        public static IDamageHandler CreateStandardChain()
        {
            return new DamageChainBuilder()
                .WithCriticalHits(chance: 0.15, multiplier: 2.0)
                .WithRoleBonuses()
                .WithWeaknesses()
                .WithDefense(reduction: 0.1)
                .WithMinimumDamage(minimum: 1)
                .Build();
        }

        /// <summary>
        /// Creates an easy mode chain (higher damage, more crits)
        /// </summary>
        public static IDamageHandler CreateEasyModeChain()
        {
            return new DamageChainBuilder()
                .WithCriticalHits(chance: 0.25, multiplier: 2.5)
                .WithRoleBonuses()
                .WithWeaknesses()
                .WithDefense(reduction: 0.05) // Less defense reduction
                .WithMinimumDamage(minimum: 2) // Higher minimum
                .Build();
        }

        /// <summary>
        /// Creates a hard mode chain (less damage, fewer crits)
        /// </summary>
        public static IDamageHandler CreateHardModeChain()
        {
            return new DamageChainBuilder()
                .WithCriticalHits(chance: 0.10, multiplier: 1.5)
                .WithRoleBonuses()
                .WithWeaknesses()
                .WithDefense(reduction: 0.20) // More defense reduction
                .WithMinimumDamage(minimum: 1)
                .Build();
        }
    }
}