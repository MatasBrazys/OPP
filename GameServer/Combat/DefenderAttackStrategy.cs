// GameServer/Combat/DefenderAttackStrategy.cs
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
        // Range measured in tiles (1 = adjacent tiles + current tile)
        private const int RangeInTiles = 1;
        private const int Damage = 10;

        public void ExecuteAttack(PlayerRole player, AttackMessage msg)
        {
            if (player == null)
            {
                Console.WriteLine("[DefenderAttack] No player provided.");
                return;
            }

            // Convert player position into tile coords
            int playerTileX = player.X / GameConstants.TILE_SIZE;
            int playerTileY = player.Y / GameConstants.TILE_SIZE;

            // msg.TargetX/TargetY are expected to already be tile indices from the client
            int targetTileX = msg.TargetX;
            int targetTileY = msg.TargetY;

            // Check whether the clicked tile is within melee reach of the player
            int dxPlayerToTarget = Math.Abs(playerTileX - targetTileX);
            int dyPlayerToTarget = Math.Abs(playerTileY - targetTileY);
            int playerToTargetChebyshev = Math.Max(dxPlayerToTarget, dyPlayerToTarget);

            if (playerToTargetChebyshev > RangeInTiles)
            {
                // Target is too far — slash cannot reach that tile
                Console.WriteLine($"[DEFENDER] Player {player.Id} tried to slash at ({targetTileX},{targetTileY}) but it was out of range (player at {playerTileX},{playerTileY}).");
                return;
            }

            // Now apply damage to enemies that are within RangeInTiles of the player's tile
            var enemies = Game.Instance.WorldFacade.GetAllEnemies();

            int hits = 0;
            // Iterate a copy to allow safe removal inside loop (facade will remove from world)
            var enemiesList = enemies.ToList();

            foreach (var enemy in enemiesList)
            {
                int enemyTileX = enemy.X / GameConstants.TILE_SIZE;
                int enemyTileY = enemy.Y / GameConstants.TILE_SIZE;

                int dx = Math.Abs(enemyTileX - playerTileX);
                int dy = Math.Abs(enemyTileY - playerTileY);
                int cheb = Math.Max(dx, dy);

                if (cheb <= RangeInTiles)
                {
                    // Enemy is within melee range of the player — hit it
                    enemy.Health -= Damage;
                    hits++;

                    Console.WriteLine($"[HIT] Defender {player.Id} hit Enemy {enemy.Id}: -{Damage} HP -> {enemy.Health}/{enemy.MaxHealth}");

                    if (enemy.Health <= 0)
                    {
                        Console.WriteLine($"[DEATH] Enemy {enemy.Id} died (killed by player {player.Id}).");
                        Game.Instance.WorldFacade.RemoveEnemy(enemy);
                    }
                }
            }

            if (hits == 0)
            {
                Console.WriteLine($"[SLASH] Defender {player.Id} slashed at ({targetTileX},{targetTileY}) and hit air.");
            }

            // IMPORTANT: State broadcasting should be done by the caller (server) after this method,
            // so that all clients receive the updated state. If you want immediate broadcast here,
            // you can call the server's BroadcastState method — but prefer the caller to control that.
        }
    }
}
