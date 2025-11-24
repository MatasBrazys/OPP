// File: GameServer/Combat/HunterAttackStrategy.cs
using System;
using System.Collections.Generic;
using System.Linq;
using GameShared;
using GameShared.Interfaces;
using GameShared.Messages;
using GameShared.Types.Players;
using GameServer.Combat.DamageHandlers;

namespace GameServer.Combat
{
    public class HunterAttackStrategy : IAttackStrategy
    {
        private const int BaseDamage = 20;
        private static readonly float AttackRangePx = GameConstants.TILE_SIZE * 3f;
        private const float ArrowSpeedPxPerSec = 800f;
        private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(900);
        private readonly Dictionary<int, DateTime> lastAttackTimes = new();

        // ✅ CHAIN OF RESPONSIBILITY: Damage calculation chain
        private readonly IDamageHandler _damageChain;

        public HunterAttackStrategy()
        {
            _damageChain = DamageChainBuilder.CreateStandardChain();
            Console.WriteLine("✅ [HUNTER] Damage chain initialized with standard configuration");
        }

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null || msg == null)
                return;

            if (lastAttackTimes.TryGetValue(player.Id, out var last) &&
                (DateTime.UtcNow - last) < Cooldown)
                return;

            lastAttackTimes[player.Id] = DateTime.UtcNow;

            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float tx = msg.ClickX;
            float ty = msg.ClickY;

            float vx = tx - px;
            float vy = ty - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

            if (dist == 0) return;

            float nx = vx / dist;
            float ny = vy / dist;
            float clampedDist = Math.Min(dist, AttackRangePx);
            float targetX = px + nx * clampedDist;
            float targetY = py + ny * clampedDist;

            double angleRad = Math.Atan2(targetY - py, targetX - px);
            double angleDeg = (angleRad * 180.0 / Math.PI + 360.0) % 360.0;

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

            var enemies = Game.Instance.WorldFacade.GetAllEnemies().ToList();
            bool hit = false;

            Console.WriteLine($"\n🏹 [HUNTER ATTACK] Player {player.Id} firing arrow at angle {angleDeg:F1}°");

            foreach (var e in enemies)
            {
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;

                float dx = ex - px;
                float dy = ey - py;
                float proj = (dx * nx + dy * ny) / clampedDist;

                if (proj < 0 || proj > 1)
                    continue;

                float closestX = px + proj * nx * clampedDist;
                float closestY = py + proj * ny * clampedDist;
                float offX = ex - closestX;
                float offY = ey - closestY;
                float distanceToArrow = (float)Math.Sqrt(offX * offX + offY * offY);

                if (distanceToArrow <= GameConstants.ENEMY_SIZE / 2f)
                {
                    // ✅ USE CHAIN OF RESPONSIBILITY for damage calculation
                    var context = new DamageContext(BaseDamage, player, e, "arrow");
                    var result = _damageChain.HandleDamage(context);

                    e.Health -= result.FinalDamage;
                    hit = true;

                    Console.WriteLine($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    Console.WriteLine($"🎯 ARROW HIT!");
                    Console.WriteLine($"   Attacker: {player.RoleType} (ID: {player.Id})");
                    Console.WriteLine($"   Target: {e.EnemyType} (ID: {e.Id})");
                    Console.WriteLine($"   Base Damage: {BaseDamage}");
                    Console.WriteLine($"   Final Damage: {result.FinalDamage}");
                    Console.WriteLine($"   HP: {e.Health + result.FinalDamage}/{e.MaxHealth} → {e.Health}/{e.MaxHealth}");
                    
                    if (result.EffectsApplied.Count > 0)
                    {
                        Console.WriteLine($"   Effects Applied:");
                        foreach (var effect in result.EffectsApplied)
                        {
                            Console.WriteLine($"      • {effect}");
                        }
                    }
                    Console.WriteLine($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

                    if (e.Health <= 0)
                    {
                        Console.WriteLine($"☠️ [DEATH] Enemy {e.Id} ({e.EnemyType}) was slain by Hunter {player.Id}!");
                        Game.Instance.WorldFacade.RemoveEnemy(e);
                    }
                }
            }

            if (!hit)
                Console.WriteLine($"❌ [MISS] Hunter {player.Id} fired arrow toward ({targetX:F1},{targetY:F1}) angle={angleDeg:F1}° — no hits.\n");
        }
    }
}