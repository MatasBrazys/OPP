// File: GameClient/Managers/EntityManager.cs (MODIFIED VERSION)
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GameClient.Rendering;
using GameClient.Rendering.Bridge;
using GameShared.Types.DTOs;

namespace GameClient.Managers
{
    public class EntityManager
    {
        private readonly Dictionary<int, PlayerRenderer> _players = new();
        private readonly Dictionary<int, EnemyRenderer> _enemies = new();
        private Image _defaultEnemySprite;

        // BRIDGE: Store current renderer mode
        private IRenderer _currentRenderer;

        public EntityManager(Image defaultEnemySprite, IRenderer? renderer = null)
        {
            _defaultEnemySprite = defaultEnemySprite;
            _currentRenderer = renderer ?? new StandardRenderer();
        }

        // BRIDGE: Method to switch renderers for ALL entities
        public void SetRenderer(IRenderer renderer)
        {
            _currentRenderer = renderer ?? throw new System.ArgumentNullException(nameof(renderer));

            lock (_players)
            {
                foreach (var pr in _players.Values)
                    pr.SetRenderer(_currentRenderer);
            }

            lock (_enemies)
            {
                foreach (var er in _enemies.Values)
                    er.SetRenderer(_currentRenderer);
            }
        }

        public void SetDefaultEnemySprite(Image sprite)
        {
            _defaultEnemySprite = sprite;
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
                        var isLocal = ps.Id == localPlayerId;

                        // BRIDGE: Pass renderer to constructor
                        var renderer = new PlayerRenderer(ps.Id, ps.RoleType, ps.X, ps.Y, sprite,
                                                         isLocal, Color.Black, Color.Blue, _currentRenderer);
                        _players[ps.Id] = renderer;
                    }
                    else
                    {
                        existing.SetTarget(ps.X, ps.Y);
                    }
                }

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

                        // BRIDGE: Pass renderer to constructor
                        var renderer = new EnemyRenderer(es.Id, es.EnemyType, es.X, es.Y, sprite,
                                                        es.Health, es.MaxHealth, _currentRenderer);
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

        public List<PlayerRenderer> GetAllPlayerRenderers()
        {
            lock (_players)
            {
                return _players.Values.ToList();
            }
        }

    }
}