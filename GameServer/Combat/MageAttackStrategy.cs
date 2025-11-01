using System;
using System.Linq;
using GameShared;
using GameShared.Interfaces;
using GameShared.Messages;
using GameShared.Types.Players;

namespace GameServer.Combat
{
    public class MageAttackStrategy : IAttackStrategy
    {
        private const int Damage = 8;
        private const float AttackRangePx = GameConstants.TILE_SIZE * 3; // ranged: 3 tiles
        private const float MaxVisualDistancePx = 200f;                   // visual clamp for animation
        private const float AoERadiusPx = GameConstants.TILE_SIZE;        // splash around target
        private static DateTime _lastAttack = DateTime.MinValue;
        private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(500);

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null || msg == null) return;

            // Enforce cooldown
            if (DateTime.UtcNow - _lastAttack < Cooldown) return;
            _lastAttack = DateTime.UtcNow;

            // Player center
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;

            // Click coords (from client)
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            // Vector to click
            float vx = clickX - px;
            float vy = clickY - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

            // Clamp to max visual distance
            float animX = px, animY = py;
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

            // Angle for animation
            double angleRad = Math.Atan2(animY - py, animX - px);
            double angleDeg = (angleRad * 180.0 / Math.PI + 360.0) % 360.0;

            // Broadcast animation
            var animMsg = new AttackAnimationMessage
            {
                AttackType = "mage",
                PlayerId = player.Id,
                AnimX = animX,
                AnimY = animY,
                Direction = angleDeg.ToString("F1"),
                Radius = AoERadiusPx
            };
            Game.Instance.Server.BroadcastToAll(animMsg);

            // Damage enemies in AoE
            var enemies = Game.Instance.WorldFacade.GetAllEnemies().ToList();
            int hits = 0;
            foreach (var e in enemies)
            {
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;
                float dx = ex - animX;
                float dy = ey - animY;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);

                if (d <= AoERadiusPx)
                {
                    e.Health -= Damage;
                    hits++;
                    Console.WriteLine($"[HIT] Mage {player.Id} hit Enemy {e.Id}: -{Damage} HP ({e.Health}/{e.MaxHealth})");
                    if (e.Health <= 0)
                    {
                        Console.WriteLine($"[DEATH] Enemy {e.Id} was slain by Mage {player.Id}");
                        Game.Instance.WorldFacade.RemoveEnemy(e);
                    }
                }
            }

            if (hits == 0)
                Console.WriteLine($"[MISS] Mage {player.Id} cast at ({animX:F1},{animY:F1}) angle={angleDeg:F1}° — no hits.");
        }
    }
}
