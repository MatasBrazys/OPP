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
        private const float SlashAngleDeg = 90f;                                // 90° cone

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null || msg == null) return;

            // Player center (pixels)
            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;

            // Click position (pixels)
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            // Vector player → click
            float vx = clickX - px;
            float vy = clickY - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

            // Clamp visual animation
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
                animX = px + MaxVisualDistancePx * 0.5f;
                animY = py;
            }

            // Angle of slash (degrees, 0 = right)
            double playerAngle = Math.Atan2(animY - py, animX - px) * 180.0 / Math.PI;

            // Broadcast slash animation
            var anim = new AttackAnimationMessage
            {
                PlayerId = player.Id,
                AnimX = animX,
                AnimY = animY,
                Direction = playerAngle.ToString("F1"),
                Radius = MaxVisualDistancePx
            };
            Game.Instance.Server.BroadcastToAll(anim);

            // Damage enemies inside cone
            var enemies = Game.Instance.WorldFacade.GetAllEnemies().ToList();
            int hits = 0;

            foreach (var e in enemies)
            {
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;

                float dx = ex - px;
                float dy = ey - py;
                float distanceToEnemy = (float)Math.Sqrt(dx * dx + dy * dy);

                if (distanceToEnemy > AttackRangePx)
                    continue;

                double angleToEnemy = Math.Atan2(dy, dx) * 180.0 / Math.PI;
                double diff = Math.Abs(NormalizeAngle(angleToEnemy - playerAngle));

                if (diff <= SlashAngleDeg / 2f)
                {
                    // Hit enemy
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
                Console.WriteLine($"[MISS] Defender {player.Id} slashed at ({animX:F1},{animY:F1}) angle={playerAngle:F1}° — no hits.");
        }

        private static double NormalizeAngle(double angle)
        {
            // Normalize angle to 0..180° for comparison
            angle = angle % 360.0;
            if (angle < 0) angle += 360.0;
            return angle > 180.0 ? 360.0 - angle : angle;
        }
    }
}
