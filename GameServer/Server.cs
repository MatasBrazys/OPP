using GameServer.Commands;
using GameServer.Events;
using GameShared.Commands;
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
using static GameServer.Events.GameEvent;

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
        private static HashSet<(int x, int y)> eatenCherries = new HashSet<(int x, int y)>();

        static readonly string[] AllRoles = new[] { "hunter", "mage", "defender" };
        private static readonly NormalMovement DefaultMovementStrategy = new();
        // Logging decorator switch
        private const bool EnableTileLogging = true;

        private CollisionDetector _collisionDetector;
        private List<CommandHandler> _commandHandlers;

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
                    // Determine available roles (those not currently used)
                    var used = clientRoles.Values.ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var available = AllRoles.Where(r => !used.Contains(r)).ToList();

                    if (available.Count == 0)
                    {
                        // Max 3 players already connected
                        SendMessage(client, new ErrorMessage { Code = "server_full", Detail = "All roles are taken." });
                        client.Close();
                        continue;
                    }

                    // Pick a random role from the remaining ones
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
                new CollisionCommandHandler(this),
                new PlayerCommandHandler(this, Game.Instance)
            };
            foreach (var handler in _commandHandlers)
            {
                _collisionDetector.RegisterObserver(handler);
            }
            _collisionDetector.RegisterObserver(this);
        }

        private void HandleInput(int id, InputMessage input)
        {
            lock (locker)
            {
                var player = Game.Instance.WorldFacade.GetPlayer(id);
                if (player == null) return;

                int newX = player.X + input.Dx * player.GetSpeed();
                int newY = player.Y + input.Dy * player.GetSpeed();

                var result = Game.Instance.WorldFacade.TryMovePlayer(id, newX, newY);

                if (result != null)
                {

                    ApplyTileEnterResult(player, player.X / 128, player.Y / 128, result);
            }

            var players = Game.Instance.WorldFacade.GetAllPlayers();
            _collisionDetector.CheckCollisions(players);
            }

            BroadcastState();
        }

        public void ApplyTileEnterResult(PlayerRole player, int tileX, int tileY, TileEnterResult result)
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
                eatenCherries.Add((tileX, tileY));

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
            // Server-level handling of game events
            Console.WriteLine($"Server received event: {gameEvent.EventType} at {gameEvent.Timestamp}");

            if (gameEvent is CollisionEvent collision)
            {
                // Server-wide collision handling (logging, analytics, etc.)
                LogCollision(collision);
            }
        }
        private void LogCollision(CollisionEvent collision)
        {
            // Log collision for analytics or debugging
            var logEntry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Collision: {collision.Entity1Id}({collision.Entity1Type}) vs {collision.Entity2Id}({collision.Entity2Type}) at ({collision.X:F1}, {collision.Y:F1})";
            Console.WriteLine(logEntry);
        }

        public void BroadcastToAll<T>(T message)
        {
            foreach (var client in clients.Values)
            {
                SendMessage(client, message);
            }
        }


        private void SendStateTo(TcpClient client)
        {
            var snapshot = new StateMessage
            {
                ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Players = Game.Instance.World.GetPlayers()
                    .Select(p => new PlayerDto { Id = p.Id, X = p.X, Y = p.Y, Health = p.Health, RoleType = p.RoleType, RoleColor = p.RoleColor.Name })
                    .ToList(),
                Enemies = Game.Instance.World.GetEnemies()
                    .Select(e => new EnemyDto { Id = e.Id, EnemyType = e.EnemyType, X = e.X, Y = e.Y, Health = e.Health })
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
                    Console.WriteLine($"Received from client {id}: {line}");
                    var doc = JsonDocument.Parse(line);
                    var type = doc.RootElement.GetProperty("Type").GetString();
                    switch (type)
                    {
                        case "input":
                            var input = JsonSerializer.Deserialize<InputMessage>(line);
                            HandleInput(id, input);
                            break;
                        case "ping":
                            var ping = JsonSerializer.Deserialize<PingMessage>(line);
                            SendMessage(client, new PongMessage { T = ping.T });
                            break;
                        case "command": 
                            var commandMsg = JsonSerializer.Deserialize<CommandMessage>(line);
                            if (commandMsg != null)
                            {
                                HandleCommand(id, commandMsg);
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
                BroadcastState();
            }
        }
        public void BroadcastState()
        {
            StateMessage state;
            List<TcpClient> recipients;

            // 1) Build the snapshot and copy the recipients under the lock
            lock (locker)
            {
                state = new StateMessage
                {
                    ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Players = Game.Instance.World.GetPlayers()
                        .Select(p => new PlayerDto { Id = p.Id, X = p.X, Y = p.Y, Health = p.Health, RoleType = p.RoleType, RoleColor = p.RoleColor.Name })
                        .ToList(),
                    Enemies = Game.Instance.World.GetEnemies()
                        .Select(e => new EnemyDto { Id = e.Id, EnemyType = e.EnemyType, X = e.X, Y = e.Y, Health = e.Health })
                        .ToList()
                };

                // Copy so we can iterate without holding the lock, and without risk of
                // "collection modified" during sends.
                recipients = clients.Values.ToList();
            }

            // 2) Release the lock before doing ANY network I/O
            var json = JsonSerializer.Serialize(state) + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);

            foreach (var client in recipients)
            {
                try
                {
                    client.GetStream().Write(bytes, 0, bytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Broadcast write failed: {ex.Message}");
                    // Optionally mark client for removal, but do NOT take the world lock here.
                }
            }
        }

        private void SendMessage<T>(TcpClient client, T msg)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(msg) + "\n";
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            client.GetStream().Write(bytes, 0, bytes.Length);
        }
        private void HandleCommand(int playerId, CommandMessage commandMsg)
        {
            Console.WriteLine($"=== COMMAND RECEIVED ===");
            Console.WriteLine($"Player: {playerId}");
            Console.WriteLine($"Command Type: {commandMsg.Command?.Type}");

            if (commandMsg?.Command == null)
            {
                Console.WriteLine("ERROR: Command is null!");
                return;
            }

            lock (locker)
            {
                try
                {
                    Game.Instance.Invoker.ExecuteCommand(commandMsg.Command, playerId);

                    if (commandMsg.Command is MoveCommand)
                    {
                        // After movement, check collisions and apply tile effects
                        var player = Game.Instance.World.GetPlayer(playerId);
                        if (player != null)
                        {
                            int tileX = player.X / 128;
                            int tileY = player.Y / 128;
                            var tile = Game.Instance.World.Map.GetTile(tileX, tileY);

                            if (tile != null)
                            {
                                var result = tile.OnEnter(player);
                                if (result != null)
                                {
                                    ApplyTileEnterResult(player, tileX, tileY, result);
                                }
                            }
                            var players = Game.Instance.WorldFacade.GetAllPlayers();
                            _collisionDetector.CheckCollisions(players);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error executing command: {ex.Message}");
                }
            }

            BroadcastState();

            var resultMessage = new CommandResultMessage
            {
                PlayerId = playerId,
                CommandType = commandMsg.Command.Type,
                Success = true
            };

            if (clients.TryGetValue(playerId, out TcpClient? client))
            {
                SendMessage(client, resultMessage);
            }
        }
    }
}
