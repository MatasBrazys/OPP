// gameserver/combat/damagehandlers/idamagehandler.cs
using GameShared.Types.Players;
using GameShared.Types.Enemies;

namespace GameServer.Combat.DamageHandlers
{

    public interface IDamageHandler
    {
        void SetNext(IDamageHandler handler);
        DamageResult HandleDamage(DamageContext context);
    }

    public class DamageContext
    {
        public int BaseDamage { get; set; }
        public PlayerRole Attacker { get; set; }
        public Enemy Target { get; set; }
        public string AttackType { get; set; } // "slash", "arrow", "fireball"
        public List<string> AppliedEffects { get; set; } = new();

        public DamageContext(int baseDamage, PlayerRole attacker, Enemy target, string attackType)
        {
            BaseDamage = baseDamage;
            Attacker = attacker;
            Target = target;
            AttackType = attackType;
        }
    }

 
    public class DamageResult
    {
        public int FinalDamage { get; set; }
        public List<string> EffectsApplied { get; set; } = new();

        public DamageResult(int finalDamage, List<string> effectsApplied)
        {
            FinalDamage = finalDamage;
            EffectsApplied = effectsApplied;
        }
    }


    public abstract class BaseDamageHandler : IDamageHandler
    {
        private IDamageHandler? _nextHandler;

        public void SetNext(IDamageHandler handler)
        {
            _nextHandler = handler;
        }

        public virtual DamageResult HandleDamage(DamageContext context)
        {

            ProcessDamage(context);
            if (_nextHandler != null)
            {
                return _nextHandler.HandleDamage(context);
            }
            return new DamageResult(context.BaseDamage, context.AppliedEffects);
        }
        protected abstract void ProcessDamage(DamageContext context);

        protected void LogEffect(DamageContext context, string effectName, string description)
        {
            context.AppliedEffects.Add($"[{effectName}] {description}");
        }
    }
}