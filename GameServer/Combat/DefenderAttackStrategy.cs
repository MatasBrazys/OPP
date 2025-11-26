using GameServer.Combat.DamageHandlers;
using GameShared;
using GameShared.Messages;
using GameShared.Types.Enemies;
using GameShared.Types.Players;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameServer.Combat
{
    public class DefenderAttackStrategy : AttackTemplate
    {
        private const int BaseDamage = 10;
        private static readonly float AttackRangePx = GameConstants.TILE_SIZE;
        private static readonly float SlashAngleDeg = 90f;

        // Chain of Responsibility for damage
        private readonly IDamageHandler _damageChain;

        // Store last attack times per player
        private readonly Dictionary<int, DateTime> _lastAttackTimes = new();

        public DefenderAttackStrategy()
        {
            _damageChain = DamageChainBuilder.CreateStandardChain();
            Console.WriteLine("✅ [DEFENDER] Damage chain initialized with standard configuration");
        }

        protected override bool CanAttack(PlayerRole player)
        {
            if (!_lastAttackTimes.TryGetValue(player.Id, out var last))
                last = DateTime.MinValue;

            if ((DateTime.UtcNow - last) < TimeSpan.FromMilliseconds(600))
            {
                Console.WriteLine($"❌ [COOLDOWN] Defender {player.Id} cannot attack yet");
                return false;
            }

            return true;
        }

        protected override void StartCooldown(PlayerRole player)
        {
            _lastAttackTimes[player.Id] = DateTime.UtcNow;
            Console.WriteLine($"🕑 [COOLDOWN] Defender {player.Id} attack cooldown started");
        }

        protected override IEnumerable<Enemy> SelectTargets(PlayerRole player, AttackMessage msg)
        {
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            double playerAngle = Math.Atan2(clickY - py, clickX - px) * 180.0 / Math.PI;

            var enemies = Game.Instance.WorldFacade.GetAllEnemies();

            var targets = enemies.Where(e =>
            {
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;
                float dx = ex - px;
                float dy = ey - py;
                float distanceToEnemy = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distanceToEnemy > AttackRangePx) return false;

                double angleToEnemy = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                double diff = Math.Abs(NormalizeAngle(angleToEnemy - playerAngle));

                return diff <= SlashAngleDeg / 2f;
            }).ToList();

            if (!targets.Any())
            {
                Console.WriteLine($"❌ [MISS] Defender {player.Id} slashed at ({clickX:F1},{clickY:F1}) — no hits.");
            }
            else
            {
                Console.WriteLine($"🗡️ [DEFENDER ATTACK] Player {player.Id} slashing at angle {playerAngle:F1}°, targets: {targets.Count}");
            }

            return targets;
        }

        // Calculate damage using chain of responsibility
        protected override int CalculateDamage(PlayerRole player, Enemy enemy)
        {
            var context = new DamageContext(BaseDamage, player, enemy, "slash");
            var result = _damageChain.HandleDamage(context);

            Console.WriteLine($"💥 [HIT] Defender {player.Id} → Enemy {enemy.Id}: Base={BaseDamage}, Final={result.FinalDamage}, HP={enemy.Health}->{enemy.Health - result.FinalDamage}");

            return result.FinalDamage;
        }

        protected override void OnHit(PlayerRole player, Enemy enemy)
        {
            if (enemy.Health <= 0)
            {
                Console.WriteLine($"☠️ [DEATH] Enemy {enemy.Id} ({enemy.EnemyType}) slain by Defender {player.Id}");
                Game.Instance.WorldFacade.RemoveEnemy(enemy);
            }
        }

        private static double NormalizeAngle(double angle)
        {
            angle %= 360.0;
            if (angle < 0) angle += 360.0;
            return angle > 180.0 ? 360.0 - angle : angle;
        }

        protected override void BroadcastAttackAnimation(PlayerRole player, AttackMessage msg)
        {
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            float dx = clickX - px;
            float dy = clickY - py;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            // Normalize direction
            if (dist > 0)
            {
                dx /= dist;
                dy /= dist;
            }

            // Clamp animation to real slash range
            float animX = px + dx * AttackRangePx;
            float animY = py + dy * AttackRangePx;

            double playerAngle = Math.Atan2(animY - py, animX - px) * 180.0 / Math.PI;

            var anim = new AttackAnimationMessage
            {
                PlayerId = player.Id,
                AttackType = "slash",
                AnimX = animX,
                AnimY = animY,
                Direction = playerAngle.ToString("F1"),
                Radius = AttackRangePx
            };

            Game.Instance.Server.BroadcastToAll(anim);
        }
    }
}
