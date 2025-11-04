// ./GameClient/Managers/EntityManager.cs
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GameClient.Rendering;
using GameShared.Types.DTOs;

namespace GameClient.Managers
{
    public class EntityManager
    {
        private readonly Dictionary<int, PlayerRenderer> _players = new();
        private readonly Dictionary<int, EnemyRenderer> _enemies = new();
        private readonly Image _defaultEnemySprite;

        public EntityManager(Image defaultEnemySprite)
        {
            _defaultEnemySprite = defaultEnemySprite;
        }

        public void UpdatePlayers(List<PlayerDto> players, int localPlayerId)
        {
            lock (_players)
            {
                foreach (var ps in players)
                {
                    if (!_players.TryGetValue(ps.Id, out var existing))
                    {
                        var sprite = SpriteRegistry.GetSprite(ps.RoleType);
                        var isLocal = ps.Id == localPlayerId; // mark local player
                        var renderer = new PlayerRenderer(ps.Id, ps.RoleType, ps.X, ps.Y, sprite, isLocal);
                        _players[ps.Id] = renderer;
                    }
                    else
                    {
                        existing.SetTarget(ps.X, ps.Y);
                    }
                }

                // remove missing players
                var serverIds = players.Select(p => p.Id).ToHashSet();
                var toRemove = _players.Keys.Where(k => !serverIds.Contains(k)).ToList();
                foreach (var id in toRemove) _players.Remove(id);
            }
        }

        
        public void UpdateEnemies(List<EnemyDto> enemies)
        {
            lock (_enemies)
            {
                foreach (var es in enemies)
                {
                    if (!_enemies.TryGetValue(es.Id, out var existing))
                    {
                        var sprite = SpriteRegistry.GetSprite(es.EnemyType) ?? _defaultEnemySprite;
                        var renderer = new EnemyRenderer(es.Id, es.EnemyType, es.X, es.Y, sprite, es.Health, es.MaxHealth);
                        _enemies[es.Id] = renderer;
                    }
                    else
                    {
                        existing.SetTarget(es.X, es.Y);
                        existing.CurrentHP = es.Health;
                        existing.MaxHP = es.MaxHealth;
                    }
                }

                var serverIds = enemies.Select(e => e.Id).ToHashSet();
                var toRemove = _enemies.Keys.Where(k => !serverIds.Contains(k)).ToList();
                foreach (var id in toRemove) _enemies.Remove(id);
            }
        }

        public void DrawAll(Graphics g)
        {
            lock (_players)
            {
                foreach (var pr in _players.Values) pr.Draw(g);
            }

            lock (_enemies)
            {
                foreach (var er in _enemies.Values) er.Draw(g);
            }
        }

        public PlayerRenderer? GetPlayerRenderer(int id)
        {
            lock (_players)
            {
                _players.TryGetValue(id, out var r);
                return r;
            }
        }
    }
}
