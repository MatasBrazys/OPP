// File: GameServer/Combat/DefenderAttackStrategy.cs
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
    public class DefenderAttackStrategy : IAttackStrategy
    {
        private const int BaseDamage = 10;
        private static readonly float AttackRangePx = GameConstants.TILE_SIZE; 
        private const float MaxVisualDistancePx = 128f;                        
        private const float SlashAngleDeg = 90f;                                
        private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(600);

        private readonly Dictionary<int, DateTime> lastAttackTimes = new();
        
        // ✅ CHAIN OF RESPONSIBILITY: Damage calculation chain
        private readonly IDamageHandler _damageChain;

        public DefenderAttackStrategy()
        {
            // Build the damage chain using the builder
            _damageChain = DamageChainBuilder.CreateStandardChain();
            
            Console.WriteLine("✅ [DEFENDER] Damage chain initialized with standard configuration");
        }

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null || msg == null) return;

            if (lastAttackTimes.TryGetValue(player.Id, out var last) && 
                (DateTime.UtcNow - last) < Cooldown)
            {
                return;
            }

            lastAttackTimes[player.Id] = DateTime.UtcNow;

            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            float vx = clickX - px;
            float vy = clickY - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

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

            double playerAngle = Math.Atan2(animY - py, animX - px) * 180.0 / Math.PI;

            var anim = new AttackAnimationMessage
            {
                AttackType = "slash",
                PlayerId = player.Id,
                AnimX = animX,
                AnimY = animY,
                Direction = playerAngle.ToString("F1"),
                Radius = MaxVisualDistancePx
            };
            Game.Instance.Server.BroadcastToAll(anim);

            var enemies = Game.Instance.WorldFacade.GetAllEnemies().ToList();
            int hits = 0;

            Console.WriteLine($"\n🗡️ [DEFENDER ATTACK] Player {player.Id} slashing at angle {playerAngle:F1}°");

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
                    // ✅ USE CHAIN OF RESPONSIBILITY for damage calculation
                    var context = new DamageContext(BaseDamage, player, e, "slash");
                    var result = _damageChain.HandleDamage(context);

                    e.Health -= result.FinalDamage;
                    hits++;

                    Console.WriteLine($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    Console.WriteLine($"💥 HIT #{hits}");
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
                        Console.WriteLine($"☠️ [DEATH] Enemy {e.Id} ({e.EnemyType}) was slain by Defender {player.Id}!");
                        Game.Instance.WorldFacade.RemoveEnemy(e);
                    }
                }
            }

            if (hits == 0)
                Console.WriteLine($"❌ [MISS] Defender {player.Id} slashed at ({animX:F1},{animY:F1}) angle={playerAngle:F1}° — no hits.\n");
        }

        private static double NormalizeAngle(double angle)
        {
            angle %= 360.0;
            if (angle < 0) angle += 360.0;
            return angle > 180.0 ? 360.0 - angle : angle;
        }
    }
}