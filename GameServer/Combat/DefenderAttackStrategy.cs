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
        private static readonly float AttackRangePx = GameConstants.TILE_SIZE; // melee reach in pixels
        private const float MaxVisualDistancePx = 128f;                        // visual clamp

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null || msg == null) return;

            // Player pixel center (player.X/Y in your code are already pixel coords)
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;

            // Click pixel coords (client sent)
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            // Vector from player -> click
            float vx = clickX - px;
            float vy = clickY - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

            // If click is inside visual distance, use exact click; otherwise clamp to max visual distance
            float animX, animY;
            if (dist <= MaxVisualDistancePx && dist > 0f)
            {
                animX = clickX;
                animY = clickY;
            }
            else if (dist > 0f)
            {
                float factor = MaxVisualDistancePx / dist;
                animX = px + vx * factor;
                animY = py + vy * factor;
            }
            else
            {
                // click exactly on player center -> place animation a bit ahead (choose right direction 0°)
                animX = px + MaxVisualDistancePx * 0.5f;
                animY = py;
            }

            // Angle for animation (degrees, 0 = right, clockwise)
            double angleRad = Math.Atan2(animY - py, animX - px); // -pi..pi
            double angleDeg = (angleRad * 180.0 / Math.PI + 360.0) % 360.0;

            // Broadcast animation (use pixel coords)
            var anim = new AttackAnimationMessage
            {
                PlayerId = player.Id,
                AnimX = animX,
                AnimY = animY,
                Direction = angleDeg.ToString("F1"),
                Radius = MaxVisualDistancePx
            };
            Game.Instance.Server.BroadcastToAll(anim);

            // Damage: check enemies by pixel distance to player's centre (not to anim target).
            var enemies = Game.Instance.WorldFacade.GetAllEnemies().ToList();
            int hits = 0;
            foreach (var e in enemies)
            {
                // enemy.X/Y are top-left; get center for accurate distance
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;
                float dx = ex - px;
                float dy = ey - py;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);

                if (d <= AttackRangePx)
                {
                    e.Health -= Damage;
                    hits++;
                    Console.WriteLine($"[HIT] Defender {player.Id} hit Enemy {e.Id}: -{Damage} HP ({e.Health}/{e.MaxHealth})");
                    if (e.Health <= 0)
                    {
                        Console.WriteLine($"[DEATH] Enemy {e.Id} was slain by Defender {player.Id}");
                        Game.Instance.WorldFacade.RemoveEnemy(e);
                    }
                }
            }

            if (hits == 0)
                Console.WriteLine($"[MISS] Defender {player.Id} slashed at ({animX:F1},{animY:F1}) angle={angleDeg:F1}° — no hits.");
        }
    }
}
