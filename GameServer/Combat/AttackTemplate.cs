using GameShared.Interfaces;
using GameShared.Messages;
using GameShared.Types.Enemies;
using GameShared.Types.Players;

namespace GameServer.Combat
{
    public abstract class AttackTemplate : IAttackStrategy
    {
        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null || msg == null) return;

            if (!CanAttack(player)) return;

            StartCooldown(player);
            SetAttackState(player);
            BroadcastAttackAnimation(player, msg);

            var enemies = SelectTargets(player, msg);

            foreach (var enemy in enemies)
            {
                int damage = CalculateDamage(player, enemy);
                ApplyDamage(enemy, damage);
                OnHit(player, enemy);
            }

            FinalizeAttack(player);
        }

        protected virtual bool CanAttack(PlayerRole player) => true;

        protected virtual void StartCooldown(PlayerRole player)
        {
        }

        protected virtual void SetAttackState(PlayerRole player)
        {
        }

        protected virtual void BroadcastAttackAnimation(PlayerRole player, AttackMessage msg)
        {
            var anim = new AttackAnimationMessage
            {
                PlayerId = player.Id,
                AttackType = "template",
                AnimX = msg.ClickX,
                AnimY = msg.ClickY
            };
            Game.Instance.Server.BroadcastToAll(anim);
        }

        protected virtual void ApplyDamage(Enemy enemy, int amount)
        {
            enemy.Health -= amount;
        }

        protected virtual void FinalizeAttack(PlayerRole player)
        {
            Game.Instance.Server.BroadcastState();
        }

        protected abstract IEnumerable<Enemy> SelectTargets(PlayerRole player, AttackMessage msg);
        protected abstract int CalculateDamage(PlayerRole player, Enemy enemy);
        protected virtual void OnHit(PlayerRole player, Enemy enemy) { }
    }

}
