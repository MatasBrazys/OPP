// File: GameServer/Combat/MageAttackStrategy.cs
using System;
using System.Linq;
using GameShared;
using GameShared.Interfaces;
using GameShared.Messages;
using GameShared.Types.Players;
using GameServer.Combat.DamageHandlers;

namespace GameServer.Combat
{
    public class MageAttackStrategy : IAttackStrategy
    {
        private const int BaseDamage = 18;
        private const float MaxVisualDistancePx = GameConstants.TILE_SIZE * 2;                 
        private const float AoERadiusPx = GameConstants.TILE_SIZE / 3;
        private static DateTime _lastAttack = DateTime.MinValue;
        private static readonly TimeSpan Cooldown = TimeSpan.FromMilliseconds(1200);

        // ✅ CHAIN OF RESPONSIBILITY: Damage calculation chain
        private readonly IDamageHandler _damageChain;

        public MageAttackStrategy()
        {
            _damageChain = DamageChainBuilder.CreateStandardChain();
            Console.WriteLine("✅ [MAGE] Damage chain initialized with standard configuration");
        }

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null || msg == null) return;

            if (DateTime.UtcNow - _lastAttack < Cooldown) return;
            _lastAttack = DateTime.UtcNow;

            float px = player.X + GameConstants.PLAYER_SIZE / 2f;
            float py = player.Y + GameConstants.PLAYER_SIZE / 2f;
            float clickX = msg.ClickX;
            float clickY = msg.ClickY;

            float vx = clickX - px;
            float vy = clickY - py;
            float dist = (float)Math.Sqrt(vx * vx + vy * vy);

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

            double angleRad = Math.Atan2(animY - py, animX - px);
            double angleDeg = (angleRad * 180.0 / Math.PI + 360.0) % 360.0;

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

            var enemies = Game.Instance.WorldFacade.GetAllEnemies().ToList();
            int hits = 0;

            Console.WriteLine($"\n🔥 [MAGE ATTACK] Player {player.Id} casting fireball at ({animX:F1}, {animY:F1})");

            foreach (var e in enemies)
            {
                float ex = e.X + GameConstants.ENEMY_SIZE / 2f;
                float ey = e.Y + GameConstants.ENEMY_SIZE / 2f;
                float dx = ex - animX;
                float dy = ey - animY;
                float d = (float)Math.Sqrt(dx * dx + dy * dy);

                if (d <= AoERadiusPx)
                {
                    // ✅ USE CHAIN OF RESPONSIBILITY for damage calculation
                    var context = new DamageContext(BaseDamage, player, e, "fireball");
                    var result = _damageChain.HandleDamage(context);

                    e.Health -= result.FinalDamage;
                    hits++;

                    Console.WriteLine($"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
                    Console.WriteLine($"🔥 FIREBALL HIT #{hits}");
                    Console.WriteLine($"   Attacker: {player.RoleType} (ID: {player.Id})");
                    Console.WriteLine($"   Target: {e.EnemyType} (ID: {e.Id})");
                    Console.WriteLine($"   Distance from blast: {d:F1}px");
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
                        Console.WriteLine($"☠️ [DEATH] Enemy {e.Id} ({e.EnemyType}) was slain by Mage {player.Id}!");
                        Game.Instance.WorldFacade.RemoveEnemy(e);
                    }
                }
            }

            if (hits == 0)
                Console.WriteLine($"❌ [MISS] Mage {player.Id} cast at ({animX:F1},{animY:F1}) angle={angleDeg:F1}° — no hits.\n");
            else
                Console.WriteLine($"💥 [BLAST SUMMARY] Fireball hit {hits} enemy/enemies in AoE!\n");
        }
    }
}