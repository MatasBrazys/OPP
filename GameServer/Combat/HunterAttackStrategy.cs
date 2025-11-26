using GameServer.Combat.DamageHandlers;
using GameShared;
using GameShared.Messages;
using GameShared.Types.Enemies;
using GameShared.Types.Players;

namespace GameServer.Combat
{
    public sealed class HunterAttackStrategy : AttackTemplate
    {
        private const int BaseDamage = 20;
        private static readonly float AttackRangePx = GameConstants.TILE_SIZE * 3f;
        private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(900);

        private readonly Dictionary<int, DateTime> _lastAttackTimes = new();

        // Chain of Responsibility
        private readonly IDamageHandler _damageChain;

        public HunterAttackStrategy()
        {
            _damageChain = DamageChainBuilder.CreateStandardChain();
            Console.WriteLine("✅ [HUNTER] Damage chain initialized with standard configuration");
        }

        protected override bool CanAttack(PlayerRole player)
        {
            if (!_lastAttackTimes.TryGetValue(player.Id, out var last))
                last = DateTime.MinValue;

            if ((DateTime.UtcNow - last) < Cooldown)
            {
                Console.WriteLine($"❌ [COOLDOWN] Hunter {player.Id} cannot attack yet");
                return false;
            }

            return true;
        }

        protected override void StartCooldown(PlayerRole player)
        {
            _lastAttackTimes[player.Id] = DateTime.UtcNow;
            Console.WriteLine($"🕑 [COOLDOWN] Hunter {player.Id} attack cooldown started");
        }

        protected override IEnumerable<Enemy> SelectTargets(PlayerRole player, AttackMessage msg)
        {
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float tx = msg.ClickX;
            float ty = msg.ClickY;

            float dx = tx - px;
            float dy = ty - py;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist == 0) return Enumerable.Empty<Enemy>();

            float nx = dx / dist;
            float ny = dy / dist;
            float clampedDist = Math.Min(dist, AttackRangePx);

            var enemies = Game.Instance.WorldFacade.GetAllEnemies();

            var targets = enemies.Where(e =>
            {
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;

                float proj = ((ex - px) * nx + (ey - py) * ny) / clampedDist;
                if (proj < 0 || proj > 1) return false;

                float closestX = px + proj * nx * clampedDist;
                float closestY = py + proj * ny * clampedDist;
                float distanceToArrow = (float)Math.Sqrt((ex - closestX) * (ex - closestX) + (ey - closestY) * (ey - closestY));

                return distanceToArrow <= GameConstants.ENEMY_SIZE / 2f;
            }).ToList();

            if (!targets.Any())
                Console.WriteLine($"❌ [MISS] Hunter {player.Id} fired arrow toward ({tx:F1},{ty:F1}) — no hits.");
            else
                Console.WriteLine($"🏹 [HUNTER ATTACK] Player {player.Id} firing arrow, targets: {targets.Count}");

            return targets;
        }

        // Damage calculation
        protected override int CalculateDamage(PlayerRole player, Enemy enemy)
        {
            var context = new DamageContext(BaseDamage, player, enemy, "arrow");
            var result = _damageChain.HandleDamage(context);

            Console.WriteLine($"💥 [HIT] Hunter {player.Id} → Enemy {enemy.Id}: Base={BaseDamage}, Final={result.FinalDamage}, HP={enemy.Health}->{enemy.Health - result.FinalDamage}");
            return result.FinalDamage;
        }

        protected override void OnHit(PlayerRole player, Enemy enemy)
        {
            if (enemy.Health <= 0)
            {
                Console.WriteLine($"☠️ [DEATH] Enemy {enemy.Id} ({enemy.EnemyType}) slain by Hunter {player.Id}");
                Game.Instance.WorldFacade.RemoveEnemy(enemy);
            }
        }

        protected override void BroadcastAttackAnimation(PlayerRole player, AttackMessage msg)
        {
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float tx = msg.ClickX;
            float ty = msg.ClickY;

            float dx = tx - px;
            float dy = ty - py;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            if (dist == 0) return;

            float nx = dx / dist;
            float ny = dy / dist;
            float clampedDist = Math.Min(dist, AttackRangePx);

            float targetX = px + nx * clampedDist;
            float targetY = py + ny * clampedDist;

            double angleDeg = Math.Atan2(targetY - py, targetX - px) * 180.0 / Math.PI;

            var anim = new AttackAnimationMessage
            {
                AttackType = "arrow",
                PlayerId = player.Id,
                AnimX = targetX,
                AnimY = targetY,
                Direction = angleDeg.ToString("F1"),
                Radius = AttackRangePx
            };

            Game.Instance.Server.BroadcastToAll(anim);
        }
    }
}
