using GameServer.Combat.DamageHandlers;
using GameShared;
using GameShared.Messages;
using GameShared.Types.Enemies;
using GameShared.Types.Players;

namespace GameServer.Combat
{
    public class MageAttackStrategy : AttackTemplate
    {
        private const int BaseDamage = 18;
        private const float MaxVisualDistancePx = GameConstants.TILE_SIZE * 2;
        private const float AoERadiusPx = GameConstants.TILE_SIZE / 3;
        private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(1200);
        private readonly Dictionary<int, DateTime> _lastAttackTimes = new();

        private readonly IDamageHandler _damageChain;

        public MageAttackStrategy()
        {
            _damageChain = DamageChainBuilder.CreateStandardChain();
            Console.WriteLine("✅ [MAGE] Damage chain initialized with standard configuration");
        }

        protected override bool CanAttack(PlayerRole player)
        {
            if (!_lastAttackTimes.TryGetValue(player.Id, out var last))
                last = DateTime.MinValue;

            if ((DateTime.UtcNow - last) < Cooldown)
            {
                Console.WriteLine($"❌ [COOLDOWN] Mage {player.Id} cannot attack yet");
                return false;
            }

            return true;
        }

        protected override void StartCooldown(PlayerRole player)
        {
            _lastAttackTimes[player.Id] = DateTime.UtcNow;
            Console.WriteLine($"🕑 [COOLDOWN] Mage {player.Id} attack cooldown started");
        }

        protected override IEnumerable<Enemy> SelectTargets(PlayerRole player, AttackMessage msg)
        {
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            float vx = clickX - px;
            float vy = clickY - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

            float animX, animY;
            if (dist > MaxVisualDistancePx && dist > 0f)
            {
                float factor = MaxVisualDistancePx / dist;
                animX = px + vx * factor;
                animY = py + vy * factor;
            }
            else if (dist > 0f)
            {
                animX = clickX;
                animY = clickY;
            }
            else
            {
                animX = px;
                animY = py;
            }

            var enemies = Game.Instance.WorldFacade.GetAllEnemies();
            var targets = enemies.Where(e =>
            {
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;

                float dx = ex - animX;
                float dy = ey - animY;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);

                return d <= AoERadiusPx;
            }).ToList();

            if (!targets.Any())
                Console.WriteLine($"❌ [MISS] Mage {player.Id} cast at ({animX:F1},{animY:F1}) — no hits.");
            else
                Console.WriteLine($"🔥 [MAGE ATTACK] Player {player.Id} casting fireball at ({animX:F1}, {animY:F1}), targets: {targets.Count}");

            return targets;
        }

        // Damage calculation
        protected override int CalculateDamage(PlayerRole player, Enemy enemy)
        {
            var context = new DamageContext(BaseDamage, player, enemy, "fireball");
            var result = _damageChain.HandleDamage(context);

            Console.WriteLine($"💥 [HIT] Mage {player.Id} → Enemy {enemy.Id}: Base={BaseDamage}, Final={result.FinalDamage}, HP={enemy.Health}->{enemy.Health - result.FinalDamage}");
            return result.FinalDamage;
        }

        protected override void OnHit(PlayerRole player, Enemy enemy)
        {
            if (enemy.Health <= 0)
            {
                Console.WriteLine($"☠️ [DEATH] Enemy {enemy.Id} ({enemy.EnemyType}) slain by Mage {player.Id}");
                Game.Instance.WorldFacade.RemoveEnemy(enemy);
            }
        }

        protected override void BroadcastAttackAnimation(PlayerRole player, AttackMessage msg)
        {
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            float vx = clickX - px;
            float vy = clickY - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

            float animX, animY;
            if (dist > MaxVisualDistancePx && dist > 0f)
            {
                float factor = MaxVisualDistancePx / dist;
                animX = px + vx * factor;
                animY = py + vy * factor;
            }
            else if (dist > 0f)
            {
                animX = clickX;
                animY = clickY;
            }
            else
            {
                animX = px;
                animY = py;
            }

            double angleDeg = Math.Atan2(animY - py, animX - px) * 180.0 / Math.PI;

            var animMsg = new AttackAnimationMessage
            {
                AttackType = "fireball",
                PlayerId = player.Id,
                AnimX = animX,
                AnimY = animY,
                Direction = angleDeg.ToString("F1"),
                Radius = AoERadiusPx
            };

            Game.Instance.Server.BroadcastToAll(animMsg);
        }
    }
}
