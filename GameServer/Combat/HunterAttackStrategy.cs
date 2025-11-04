using System;
using System.Collections.Generic;
using System.Linq;
using GameShared;
using GameShared.Interfaces;
using GameShared.Messages;
using GameShared.Types.Players;

namespace GameServer.Combat
{
    public class HunterAttackStrategy : IAttackStrategy
    {
        private const int Damage = 20;
        private static readonly float AttackRangePx = GameConstants.TILE_SIZE * 3f; // 3 tiles
        private const float ArrowSpeedPxPerSec = 800f; // visual simulation speed
        private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(900);
        private readonly Dictionary<int, DateTime> lastAttackTimes = new();

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null || msg == null)
                return;

            // Cooldown check
            if (lastAttackTimes.TryGetValue(player.Id, out var last) &&
                (DateTime.UtcNow - last) < Cooldown)
                return;

            lastAttackTimes[player.Id] = DateTime.UtcNow;

            // Player center
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;

            // Target click
            float tx = msg.ClickX;
            float ty = msg.ClickY;

            // Direction vector
            float vx = tx - px;
            float vy = ty - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

            if (dist == 0) return;

            // Normalize + clamp to 3 tiles range
            float nx = vx / dist;
            float ny = vy / dist;
            float clampedDist = Math.Min(dist, AttackRangePx);
            float targetX = px + nx * clampedDist;
            float targetY = py + ny * clampedDist;

            // Direction angle (for animation)
            double angleRad = Math.Atan2(targetY - py, targetX - px);
            double angleDeg = (angleRad * 180.0 / Math.PI + 360.0) % 360.0;

            // --- Broadcast "arrow shot" animation ---
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

            // --- Hit logic ---
            var enemies = Game.Instance.WorldFacade.GetAllEnemies().ToList();
            bool hit = false;

            foreach (var e in enemies)
            {
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;

                // Vector player → enemy
                float dx = ex - px;
                float dy = ey - py;
                float proj = (dx * nx + dy * ny) / clampedDist;

                // enemy outside shot segment → skip
                if (proj < 0 || proj > 1)
                    continue;

                // perpendicular distance from enemy to arrow line
                float closestX = px + proj * nx * clampedDist;
                float closestY = py + proj * ny * clampedDist;
                float offX = ex - closestX;
                float offY = ey - closestY;
                float distanceToArrow = (float)Math.Sqrt(offX * offX + offY * offY);

                if (distanceToArrow <= GameConstants.ENEMY_SIZE / 2f)
                {
                    e.Health -= Damage;
                    hit = true;
                    Console.WriteLine($"[HIT] Hunter {player.Id} shot Enemy {e.Id}: -{Damage} HP ({e.Health}/{e.MaxHealth})");

                    if (e.Health <= 0)
                    {
                        Console.WriteLine($"[DEATH] Enemy {e.Id} was slain by Hunter {player.Id}");
                        Game.Instance.WorldFacade.RemoveEnemy(e);
                    }
                }
            }

            if (!hit)
                Console.WriteLine($"[MISS] Hunter {player.Id} fired arrow toward ({targetX:F1},{targetY:F1}) angle={angleDeg:F1}° — no hits.");
        }
    }
}
