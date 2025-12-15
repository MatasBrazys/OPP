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
        private readonly CollisionDetector _collision_detector;
        private readonly List<CommandHandler> _commandHandlers;
        private readonly Dictionary<int, Stack<IPlayerMemento>> _playerHistory = new();
        private readonly HashSet<(int x, int y)> _grass_tiles = new();
        private readonly object _history_lock = new();
        private readonly object _grass_lock = new();
        private readonly IMovementStrategy _defaultMovementStrategy = new NormalMovement();

        // participant registry
        private readonly List<IMediatorParticipant> _participants = new();
        private readonly object _participantsLock = new();

        public GameMediator(GameWorldFacade worldFacade, IClientNotifier notifier)
        {
            _worldFacade = worldFacade;
            _notifier = notifier;

            _collision_detector = new CollisionDetector(); // keep collision detector behavior

            var collisionHandler = new CollisionCommandHandler(_notifier);
            _commandHandlers = new List<CommandHandler>
            {
                collisionHandler
            };

            // NOTE: do not register the world facade or collision detector here.
            // Those components will subscribe themselves (participant-driven).
            // Keep registering internal handlers created by the mediator if you want them treated as internal participants.
            RegisterParticipant(collisionHandler);

            // Mediator is the single observer of collisions; it dispatches to handlers.
            _collision_detector.RegisterObserver(this);
        }

        // Public API so external components can subscribe/unsubscribe.
        public void RegisterParticipant(IMediatorParticipant participant)
        {
            if (participant == null) return;

            var callAttach = false;
            lock (_participantsLock)
            {
                if (!_participants.Contains(participant))
                {
                    _participants.Add(participant);
                    callAttach = true;
                }
            }

            if (callAttach)
            {
                // notify outside lock
                participant.OnMediatorAttached(this);
            }
        }

        public void RemoveParticipant(IMediatorParticipant participant)
        {
            if (participant == null) return;

            var removed = false;
            lock (_participantsLock)
            {
                removed = _participants.Remove(participant);
            }

            if (removed)
            {
                participant.OnMediatorDetached();
            }
        }

        // Try to get a single participant of given type/interface (first match).
        public bool TryGetParticipant<T>(out T participant) where T : class
        {
            lock (_participantsLock)
            {
                foreach (var p in _participants)
                {
                    if (p is T t)
                    {
                        participant = t;
                        return true;
                    }
                }
            }

            participant = null;
            return false;
        }

        // Get all registered participants assignable to T.
        public IEnumerable<T> GetParticipants<T>() where T : class
        {
            lock (_participantsLock)
            {
                // materialize to avoid returning a collection that's iterated outside the lock
                return _participants.OfType<T>().ToList();
            }
        }

        // Optional helper: check if some participant of type T is registered
        public bool HasParticipant<T>() where T : class
        {
            lock (_participantsLock)
            {
                return _participants.Any(p => p is T);
            }
        }

        // --- existing game logic methods below (unchanged) ---
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
            _collision_detector.CheckCollisions(players);
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

            // Notify tasks about the planting
            var activeTasks = _worldFacade.GetActiveTasks();
            foreach (var task in activeTasks)
            {
                if (task is GameShared.Types.Tasks.PlantTask plantTask)
                {
                    plantTask.OnPlantSeed();
                    _worldFacade.UpdateTasks();
                }
            }

            var tileUpdate = new TileUpdateMessage
            {
                X = msg.TileX,
                Y = msg.TileY,
                TileType = "WheatPlant"
            };

            _notifier.BroadcastToAll(tileUpdate);
            Console.WriteLine($"[PLANT] Successfully planted {msg.PlantType} at ({msg.TileX}, {msg.TileY})");
        }

        public void HandleHarvestAction(HarvestActionMessage msg)
        {
            Console.WriteLine($"[HARVEST] Received harvest action from player {msg.PlayerId} at ({msg.TileX}, {msg.TileY})");

            var (width, height) = _worldFacade.GetMapSize();

            if (msg.TileX < 0 || msg.TileY < 0 ||
                msg.TileX >= width ||
                msg.TileY >= height)
            {
                Console.WriteLine($"[HARVEST] Invalid tile position: ({msg.TileX}, {msg.TileY})");
                return;
            }

            // Debug: Show all plants currently in the world
            var allPlants = _worldFacade.GetAllPlants();
            Console.WriteLine($"[HARVEST] DEBUG: Total plants in world: {allPlants.Count}");
            foreach (var p in allPlants)
            {
                Console.WriteLine($"[HARVEST] DEBUG: Plant at ({p.X}, {p.Y}) - Type: {p.PlantType}, Stage: {p.CurrentStage}, Matured: {p.IsMatured()}");
            }

            // Get the plant at this location
            var plant = _worldFacade.GetPlantAtTile(msg.TileX, msg.TileY);
            if (plant == null)
            {
                Console.WriteLine($"[HARVEST] No plant found at ({msg.TileX}, {msg.TileY})");
                return;
            }

            Console.WriteLine($"[HARVEST] ? Found plant: {plant.PlantType} at ({plant.X}, {plant.Y}), Stage: {plant.CurrentStage}");

            // Check if plant is mature (ready to harvest)
            if (!plant.IsMatured())
            {
                Console.WriteLine($"[HARVEST] ? Plant at ({msg.TileX}, {msg.TileY}) is not ready to harvest. Stage: {plant.CurrentStage}/{plant.GetStages().Count - 1}");
                return;
            }

            // Harvest the plant
            _worldFacade.HarvestPlant(plant);

            // Notify all clients about the tile change
            var tileUpdate = new TileUpdateMessage
            {
                X = msg.TileX,
                Y = msg.TileY,
                TileType = "Grass"
            };

            _notifier.BroadcastToAll(tileUpdate);
            Console.WriteLine($"[HARVEST] ? Successfully harvested {plant.PlantType} at ({msg.TileX}, {msg.TileY})");
        }

        public void UndoLastMove(int playerId)
        {
            Stack<IPlayerMemento>? stack;
            lock (_history_lock)
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
            lock (_grass_lock)
            {
                return _grass_tiles.Contains((tileX, tileY));
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
            lock (_history_lock)
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
                lock (_grass_lock)
                {
                    _grass_tiles.Add((tileX, tileY));
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
            Console.WriteLine($"Created clone {clone.Id} from player {originalPlayer.Id} with {originalPlayer.GetType().Name} role");
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
