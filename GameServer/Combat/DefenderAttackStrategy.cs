using System;
using System.Linq;
using GameShared;
using GameShared.Interfaces;
using GameShared.Messages;
using GameShared.Types.Players;

namespace GameServer.Combat
{
    public class DefenderAttackStrategy : IAttackStrategy
    {
        private const int Damage = 10;
        private const float AttackRangePx = GameConstants.TILE_SIZE; // melee 1 tile
        private const float MaxVisualDistancePx = 128f;              // max slash distance

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null) return;

            float playerX = player.X;
            float playerY = player.Y;

            // Clicked pixel coordinates
            float targetX = msg.TargetX * GameConstants.TILE_SIZE + GameConstants.TILE_SIZE / 2f;
            float targetY = msg.TargetY * GameConstants.TILE_SIZE + GameConstants.TILE_SIZE / 2f;

            float dx = targetX - playerX;
            float dy = targetY - playerY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            float animX, animY;

            if (distance <= MaxVisualDistancePx)
            {
                // If click is inside circle, just use exact click coords
                animX = targetX;
                animY = targetY;
            }
            else
            {
                // Outside circle → clamp vector to max visual distance
                float factor = MaxVisualDistancePx / distance;
                animX = playerX + dx * factor;
                animY = playerY + dy * factor;
            }

            // Compute angle for animation
            double angleDeg = (Math.Atan2(animY - playerY, animX - playerX) * 180 / Math.PI + 360) % 360;

            // Broadcast animation
            Game.Instance.Server.BroadcastToAll(new AttackAnimationMessage
            {
                PlayerId = player.Id,
                AnimX = animX,
                AnimY = animY,
                Direction = angleDeg.ToString("F1")
            });

            // --- Damage enemies ---
            var enemies = Game.Instance.WorldFacade.GetAllEnemies().ToList();
            int hits = 0;

            foreach (var enemy in enemies)
            {
                float distToEnemy = (float)Math.Sqrt((enemy.X - playerX) * (enemy.X - playerX) +
                                                     (enemy.Y - playerY) * (enemy.Y - playerY));
                if (distToEnemy <= AttackRangePx)
                {
                    enemy.Health -= Damage;
                    hits++;
                    Console.WriteLine($"[HIT] Defender {player.Id} hit Enemy {enemy.Id}: -{Damage} HP ({enemy.Health}/{enemy.MaxHealth})");
                    if (enemy.Health <= 0)
                        Game.Instance.WorldFacade.RemoveEnemy(enemy);
                }
            }

            if (hits == 0)
                Console.WriteLine($"[MISS] Defender {player.Id} slashed at ({animX:F1},{animY:F1}) — no hits.");
        }
    }
}
