//./GameServer/Server.cs
using GameServer.Commands;
using GameServer.Events;
using GameShared.Messages;
using GameShared.Strategies;
using GameShared.Types.DTOs;
using GameShared.Types.Map;
using GameShared.Types.Map.Decorators;
using GameShared.Types.Players;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameServer.Collision;
using GameShared;

namespace GameServer
{
    public class Server : IObserver
    {
        static TcpListener listener;
        static Dictionary<int, TcpClient> clients = new();
        static Dictionary<int, string> clientRoles = new();
        static int nextId = 1;
        static object locker = new object();
        private static int nextPlayerId = 1000;
        private readonly object idLock = new object();
        private static readonly object _cherriesLock = new object();
        private static HashSet<(int x, int y)> eatenCherries = new HashSet<(int x, int y)>();

        static readonly string[] AllRoles = new[] { "mage", "defender", "hunter" };
        private static readonly NormalMovement DefaultMovementStrategy = new();
        private const bool EnableTileLogging = false;

        private CollisionDetector _collisionDetector;
        private List<CommandHandler> _commandHandlers;

        private readonly Dictionary<int, Stack<IPlayerMemento>> _playerHistory = new();


        public void Start(int port)
        {
            TileLogSink.IsEnabled = EnableTileLogging;
            TileLogSink.Logger = EnableTileLogging ? message => Console.WriteLine(message) : null;

            _collisionDetector = new CollisionDetector();
            InitializeCommandHandlers();

            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Server started on port {port}");

            while (true)
            {
                var client = listener.AcceptTcpClient();
                int id;
                string role;
                lock (locker)
                {
                    var used = clientRoles.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var available = AllRoles.Where(r => !used.Contains(r)).ToList();

                    if (available.Count == 0)
                    {
                        SendMessage(client, new ErrorMessage { Code = "server_full", Detail = "All roles are taken." });
                        client.Close();
                        continue;
                    }

                    role = available[Random.Shared.Next(available.Count)];
                    
                    id = nextId++;
                    clients[id] = client;
                    clientRoles[id] = role;

                    var player = Game.Instance.WorldFacade.CreatePlayer(role, id);
                }
                SendMessage(client, new WelcomeMessage { Id = id, Type = "welcome" });
                SendStateTo(client);

                var thread = new Thread(() => HandleClient(id, client));
                thread.Start();
            }
        }

        private void InitializeCommandHandlers()
        {
            _commandHandlers = new List<CommandHandler>
            {
                new CollisionCommandHandler(this)
            };
            foreach (var handler in _commandHandlers)
            {
                _collisionDetector.RegisterObserver(handler);
            }
            _collisionDetector.RegisterObserver(this);
        }

        private void HandleInput(int id, InputMessage input)
        {
            // ✅ FIX 1: Skip if no movement
            if (input.Dx == 0 && input.Dy == 0)
                return;

            lock (locker)
            {
                var player = Game.Instance.WorldFacade.GetPlayer(id);
                if (player == null) return;
                if (!_playerHistory.ContainsKey(player.Id))
                    _playerHistory[player.Id] = new Stack<IPlayerMemento>();

                _playerHistory[player.Id].Push(player.CreateMemento());


                int oldX = player.X;
                int oldY = player.Y;

                int newX = player.X + input.Dx * player.GetSpeed();
                int newY = player.Y + input.Dy * player.GetSpeed();

                var result = Game.Instance.WorldFacade.TryMovePlayer(id, newX, newY);

                if (result != null)
                {
                    ApplyTileEnterResult(player, player.X / GameConstants.TILE_SIZE, player.Y / GameConstants.TILE_SIZE, result);
                }

                var players = Game.Instance.WorldFacade.GetAllPlayers();
                _collisionDetector.CheckCollisions(players);

                // ✅ FIX 2: REMOVED BroadcastState() - only Game.Tick() broadcasts now
                // Movement updates happen every 50ms via BroadcastState() in Game.Tick()
            }
        }

        private void ApplyTileEnterResult(PlayerRole player, int tileX, int tileY, TileEnterResult result)
        {
            var strategy = result.StrategyOverride ?? DefaultMovementStrategy;
            player.SetMovementStrategy(strategy);

            if (result.SpawnClone)
            {
                CreatePlayerClone(player);
            }

            if (result.ReplaceWithGrass)
            {
                Game.Instance.WorldFacade.ReplaceTile(tileX, tileY, new GrassTile(tileX, tileY));
                lock (_cherriesLock)
                {
                    eatenCherries.Add((tileX, tileY));
                }

                var tileUpdate = new TileUpdateMessage
                {
                    X = tileX,
                    Y = tileY,
                    TileType = "grass"
                };
                BroadcastToAll(tileUpdate);
            }
        }

        private void CreatePlayerClone(PlayerRole originalPlayer)
        {
            PlayerRole clone = originalPlayer.DeepCopy();
            clone.Id = GetNextPlayerId();
            clone.X = originalPlayer.X + 64;
            clone.Y = originalPlayer.Y + 64;

            Game.Instance.WorldFacade.AddPlayer(clone);

            var copyMessage = new CopyMadeMessage
            {
                OriginalPlayerId = originalPlayer.Id,
                NewPlayerId = clone.Id,
                OriginalRole = originalPlayer.RoleType,
                NewRole = clone.RoleType,
                CopyType = "deep"
            };

            if (clients.TryGetValue(originalPlayer.Id, out TcpClient? originalClient))
            {
                SendMessage(originalClient, copyMessage);
                Console.WriteLine($"Sent copy_made message to client {originalPlayer.Id}");
            }
            else
            {
                Console.WriteLine($"Could not find client for player {originalPlayer.Id}");
            }
            
            // ✅ Keep: Immediate broadcast needed for new entity
            BroadcastState();
            
            originalPlayer.TestDeepCopy();
            Console.WriteLine($"Created clone {clone.Id} from player {originalPlayer.Id} with {clone.GetType().Name} role");
        }

        private int GetNextPlayerId()
        {
            lock (idLock)
            {
                return nextPlayerId++;
            }
        }

        public void OnGameEvent(GameEvent gameEvent)
        {
            Console.WriteLine($"Server received event: {gameEvent.EventType} at {gameEvent.Timestamp}");

            if (gameEvent is CollisionEvent collision)
            {
                LogCollision(collision);
            }
        }

        private void LogCollision(CollisionEvent collision)
        {
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Collision: {collision.Entity1Id}({collision.Entity1Type}) vs {collision.Entity2Id}({collision.Entity2Type}) at ({collision.X:F1}, {collision.Y:F1})";
            Console.WriteLine(logEntry);
        }

        public void BroadcastToAll<T>(T message)
        {
            lock (locker)
            {
                foreach (var client in clients.Values)
                {
                    SendMessage(client, message);
                }
            }
        }

        private void SendStateTo(TcpClient client)
        {
            var snapshot = new StateMessage
            {
                ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Players = Game.Instance.WorldFacade.GetAllPlayers()
                    .Select(p => new PlayerDto { Id = p.Id, X = p.X, Y = p.Y, Health = p.Health, RoleType = p.RoleType, RoleColor = p.RoleColor.Name })
                    .ToList(),
                Enemies = Game.Instance.WorldFacade.GetAllEnemies()
                    .Select(e => new EnemyDto { Id = e.Id, EnemyType = e.EnemyType, X = e.X, Y = e.Y, Health = e.Health, MaxHealth = e.MaxHealth })
                    .ToList()
            };
            SendMessage(client, snapshot);
            SendCurrentMapStateTo(client);
        }

        private void SendCurrentMapStateTo(TcpClient client)
        {
            var (width, height) = Game.Instance.WorldFacade.GetMapSize();
            var mapState = new MapStateMessage
            {
                Width = width,
                Height = height,
                Tiles = new List<MapTileDto>()
            };

            for (int x = 0; x < Game.Instance.World.Map.Width; x++)
            {
                for (int y = 0; y < Game.Instance.World.Map.Height; y++)
                {
                    var tile = Game.Instance.WorldFacade.GetTileAt(x, y);

                    if (eatenCherries.Contains((x, y)))
                    {
                        mapState.Tiles.Add(new MapTileDto
                        {
                            X = x,
                            Y = y,
                            TileType = "grass"
                        });
                    }
                    else
                    {
                        mapState.Tiles.Add(new MapTileDto
                        {
                            X = x,
                            Y = y,
                            TileType = tile.TileType
                        });
                    }
                }
            }
            SendMessage(client, mapState);
            Console.WriteLine($"Sent current map state to new client (with {eatenCherries.Count} eaten cherries marked as grass)");
        }

        private void HandleClient(int id, TcpClient client)
        {
            Console.WriteLine($"Started handling client {id}");
            var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            string? line;
            try
            {
                while (client.Connected && (line = reader.ReadLine()) != null)
                {
                    var doc = JsonDocument.Parse(line);
                    var type = doc.RootElement.GetProperty("Type").GetString();
                    switch (type)
                    {
                        case "input":
                            var input = JsonSerializer.Deserialize<InputMessage>(line);
                            HandleInput(id, input);
                            break;
                        case "attack":
                            var attack = JsonSerializer.Deserialize<AttackMessage>(line);
                            if (attack != null)
                            {
                                if (attack.PlayerId == 0) attack.PlayerId = id;
                                OnReceiveAttack(attack);
                            }
                            break;
                        case "ping":
                            var ping = JsonSerializer.Deserialize<PingMessage>(line);
                            SendMessage(client, new PongMessage { T = ping.T });
                            break;
                        case "position_restore":
                            if (_playerHistory.TryGetValue(id, out var stack) && stack.Count > 0)
                            {
                                var snap = stack.Pop();
                                var p = Game.Instance.WorldFacade.GetPlayer(id);
                                if (p != null)
                                {
                                    p.RestoreMemento(snap);
                                    BroadcastState(); // notify all clients
                                }
                            }
                            else
                            {
                                Console.WriteLine($"[UNDO] No memento for player {id}");
                            }
                            break;

                        default:
                            SendMessage(client, new ErrorMessage { Code = "bad_message", Detail = $"unknown type: {type}" });
                            break;
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Client {id} disconnected (IOException): {ex.Message}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Client {id} sent invalid JSON: {ex.Message}");
                SendMessage(client, new ErrorMessage { Code = "invalid_json", Detail = "Malformed JSON message" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client {id}: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Client {id} disconnected");
                lock (locker)
                {
                    clients.Remove(id);
                    var player = Game.Instance.WorldFacade.GetPlayer(id);
                    if (player != null)
                        Game.Instance.WorldFacade.RemovePlayer(player);

                    clientRoles.Remove(id);
                }
                
                // ✅ Keep: Immediate broadcast needed when player disconnects
                BroadcastState();
            }
        }

        // Called by Game.Tick() every 50ms
        public void BroadcastState()
        {
            StateMessage state;
            lock (locker)
            {
                var worldPlayers = Game.Instance.WorldFacade.GetAllPlayers();

                state = new StateMessage
                {
                    ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Players = worldPlayers
                        .Select(p => new PlayerDto
                        {
                            Id = p.Id,
                            X = p.X,
                            Y = p.Y,
                            Health = p.Health,
                            RoleType = p.RoleType,
                            RoleColor = p.RoleColor.Name
                        })
                        .ToList(),
                    Enemies = Game.Instance.WorldFacade.GetAllEnemies()
                        .Select(e => new EnemyDto
                        {
                            Id = e.Id,
                            EnemyType = e.EnemyType,
                            X = e.X,
                            Y = e.Y,
                            Health = e.Health,
                            MaxHealth = e.MaxHealth
                        })
                        .ToList()
                };
            }

            lock (locker)
            {
                foreach (var client in clients.Values)
                {
                    SendMessage(client, state);
                }
            }
        }

        private void SendMessage<T>(TcpClient client, T msg)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(msg) + "\n";
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        }

        public void OnReceiveAttack(AttackMessage msg)
        {
            var player = Game.Instance.WorldFacade.GetPlayer(msg.PlayerId);
            if (player == null) return;

            player.AttackStrategy?.ExecuteAttack(player, msg);

            // ✅ Keep: Immediate broadcast for attack feedback
            BroadcastState();
        }
    }
}
