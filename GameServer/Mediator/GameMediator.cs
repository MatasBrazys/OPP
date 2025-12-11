using GameServer.Collision;
using GameServer.Commands;
using GameServer.Events;
using GameShared.Messages;
using GameShared.Strategies;
using GameShared.Types.Map;
using GameShared.Types.Players;
using GameShared;
using System;
using System.Collections.Generic;
using System.Linq;
using GameServer.Facades;

namespace GameServer.Mediator
{
    /// <summary>
    /// Coordinates interactions between Server (network), world facade, and collision handlers.
    /// Acts as a single hub instead of letting the components reference each other directly.
    /// </summary>
    public class GameMediator : IGameMediator
    {
        private readonly GameWorldFacade _worldFacade;
        private readonly IClientNotifier _notifier;
        private readonly CollisionDetector _collisionDetector;
        private readonly List<CommandHandler> _commandHandlers;
        private readonly Dictionary<int, Stack<IPlayerMemento>> _playerHistory = new();
        private readonly HashSet<(int x, int y)> _grassTiles = new();
        private readonly object _historyLock = new();
        private readonly object _grassLock = new();
        private readonly IMovementStrategy _defaultMovementStrategy = new NormalMovement();

        public GameMediator(GameWorldFacade worldFacade, IClientNotifier notifier)
        {
            _worldFacade = worldFacade;
            _notifier = notifier;

            _collisionDetector = new CollisionDetector();
            _commandHandlers = new List<CommandHandler>
            {
                new CollisionCommandHandler(_notifier)
            };

            // Mediator is the single observer of collisions; it dispatches to handlers.
            _collisionDetector.RegisterObserver(this);
        }

        public void HandleInput(int playerId, InputMessage input)
        {
            if (input.Dx == 0 && input.Dy == 0)
                return;

            var player = _worldFacade.GetPlayer(playerId);
            if (player == null) return;

            PushHistory(player);

            int newX = player.X + input.Dx * player.GetSpeed();
            int newY = player.Y + input.Dy * player.GetSpeed();

            var result = _worldFacade.TryMovePlayer(playerId, newX, newY);

            if (result != null)
            {
                ApplyTileEnterResult(player, player.X / GameConstants.TILE_SIZE, player.Y / GameConstants.TILE_SIZE, result);
            }

            var players = _worldFacade.GetAllPlayers();
            _collisionDetector.CheckCollisions(players);
        }

        public void HandleAttack(AttackMessage attack)
        {
            var player = _worldFacade.GetPlayer(attack.PlayerId);
            if (player == null) return;

            player.AttackStrategy?.ExecuteAttack(player, attack);
            _notifier.BroadcastState();
        }

        public void HandlePlantAction(PlantActionMessage msg)
        {
            Console.WriteLine($"[PLANT] Received plant action from player {msg.PlayerId} at ({msg.TileX}, {msg.TileY})");

            var (width, height) = _worldFacade.GetMapSize();

            if (msg.TileX < 0 || msg.TileY < 0 ||
                msg.TileX >= width ||
                msg.TileY >= height)
            {
                Console.WriteLine($"[PLANT] Invalid tile position: ({msg.TileX}, {msg.TileY})");
                return;
            }

            var tile = _worldFacade.GetTileAt(msg.TileX, msg.TileY);
            if (tile == null || !tile.Plantable)
            {
                Console.WriteLine($"[PLANT] Tile at ({msg.TileX}, {msg.TileY}) is not plantable");
                return;
            }

            _worldFacade.PlantSeed(msg.TileX, msg.TileY, msg.PlantType);

            var tileUpdate = new TileUpdateMessage
            {
                X = msg.TileX,
                Y = msg.TileY,
                TileType = "WheatPlant"
            };

            _notifier.BroadcastToAll(tileUpdate);
            Console.WriteLine($"[PLANT] Successfully planted {msg.PlantType} at ({msg.TileX}, {msg.TileY})");
        }

        public void UndoLastMove(int playerId)
        {
            Stack<IPlayerMemento>? stack;
            lock (_historyLock)
            {
                _playerHistory.TryGetValue(playerId, out stack);
            }

            if (stack == null || stack.Count == 0)
            {
                Console.WriteLine($"[UNDO] No memento for player {playerId}");
                return;
            }

            var snap = stack.Pop();
            var player = _worldFacade.GetPlayer(playerId);
            if (player == null) return;

            player.RestoreMemento(snap);
            _notifier.BroadcastState();
        }

        public bool IsTileReplacedWithGrass(int tileX, int tileY)
        {
            lock (_grassLock)
            {
                return _grassTiles.Contains((tileX, tileY));
            }
        }

        public void OnGameEvent(GameEvent gameEvent)
        {
            foreach (var handler in _commandHandlers)
            {
                if (handler.HandledEventTypes.Contains(gameEvent.EventType))
                {
                    handler.OnGameEvent(gameEvent);
                }
            }

            if (gameEvent is CollisionEvent collision)
            {
                LogCollision(collision);
            }
        }

        private void PushHistory(PlayerRole player)
        {
            lock (_historyLock)
            {
                if (!_playerHistory.ContainsKey(player.Id))
                {
                    _playerHistory[player.Id] = new Stack<IPlayerMemento>();
                }

                _playerHistory[player.Id].Push(player.CreateMemento());
            }
        }

        private void ApplyTileEnterResult(PlayerRole player, int tileX, int tileY, TileEnterResult result)
        {
            var strategy = result.StrategyOverride ?? _defaultMovementStrategy;
            player.SetMovementStrategy(strategy);

            if (result.SpawnClone)
            {
                CreatePlayerClone(player);
            }

            if (result.ReplaceWithGrass)
            {
                _worldFacade.ReplaceTile(tileX, tileY, new GrassTile(tileX, tileY));
                lock (_grassLock)
                {
                    _grassTiles.Add((tileX, tileY));
                }

                var tileUpdate = new TileUpdateMessage
                {
                    X = tileX,
                    Y = tileY,
                    TileType = "grass"
                };
                _notifier.BroadcastToAll(tileUpdate);
            }
        }

        private void CreatePlayerClone(PlayerRole originalPlayer)
        {
            PlayerRole clone = originalPlayer.DeepCopy();
            clone.Id = GetNextPlayerId();
            clone.X = originalPlayer.X + 64;
            clone.Y = originalPlayer.Y + 64;

            _worldFacade.AddPlayer(clone);

            var copyMessage = new CopyMadeMessage
            {
                OriginalPlayerId = originalPlayer.Id,
                NewPlayerId = clone.Id,
                OriginalRole = originalPlayer.RoleType,
                NewRole = clone.RoleType,
                CopyType = "deep"
            };

            _notifier.SendToClient(originalPlayer.Id, copyMessage);
            _notifier.BroadcastState();

            originalPlayer.TestDeepCopy();
            Console.WriteLine($"Created clone {clone.Id} from player {originalPlayer.Id} with {clone.GetType().Name} role");
        }

        private int GetNextPlayerId()
        {
            return GameServer.Server.GetNextPlayerIdStatic();
        }

        private void LogCollision(CollisionEvent collision)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Collision: {collision.Entity1Id}({collision.Entity1Type}) vs {collision.Entity2Id}({collision.Entity2Type}) at ({collision.X:F1}, {collision.Y:F1})";
            Console.WriteLine(logEntry);
        }
    }
}
