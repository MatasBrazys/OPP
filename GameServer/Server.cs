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
using System.Threading;
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

                    var player = Game.Instance.CreatePlayer(role, id);
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
            lock (locker)
            {
                var player = Game.Instance.World.GetPlayer(id);
                if (player == null) return;

                int currentTileX = player.X / 128;
                int currentTileY = player.Y / 128;
                int speed = player.GetSpeed();
                int newX = player.X + input.Dx * speed;
                int newY = player.Y + input.Dy * speed;

                int targetTileX = newX / 128;
                int targetTileY = newY / 128;

                if (targetTileX < 0 || targetTileX >= Game.Instance.World.Map.Width ||
                    targetTileY < 0 || targetTileY >= Game.Instance.World.Map.Height)
                    return;

                var targetTile = Game.Instance.World.Map.GetTile(targetTileX, targetTileY);

                if (player.CanMove(targetTile))
                {
                    bool enteredNewTile = (currentTileX != targetTileX || currentTileY != targetTileY);

                    player.X = newX;
                    player.Y = newY;

                    if (enteredNewTile)
                    {
                        var enterResult = targetTile.OnEnter(player);
                        ApplyTileEnterResult(player, targetTileX, targetTileY, enterResult);
                    }

                    player.OnMoveTile(targetTile);
                }

                var players = Game.Instance.World.GetPlayers();
                _collisionDetector.CheckCollisions(players);
            }

            BroadcastState();
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
                ReplaceTileWithGrass(tileX, tileY);
            }
        }
        private void ReplaceTileWithGrass(int tileX, int tileY)
        {
            Game.Instance.World.Map.SetTile(tileX, tileY, new GrassTile(tileX, tileY));

            eatenCherries.Add((tileX, tileY));

            var tileUpdate = new TileUpdateMessage
            {
                X = tileX,
                Y = tileY,
                TileType = "grass"
            };
            BroadcastToAll(tileUpdate);
            Console.WriteLine($"Replaced tile ({tileX}, {tileY}) with grass - broadcasted to all clients");
        }

        private void CreatePlayerClone(PlayerRole originalPlayer)
        {
            PlayerRole clone = originalPlayer.DeepCopy();
            clone.Id = GetNextPlayerId();
            clone.X = originalPlayer.X + 64;
            clone.Y = originalPlayer.Y + 64;

            Game.Instance.World.AddEntity(clone);

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
                    .Select(p => new PlayerDto
                    {
                        Id = p.Id,
                        X = p.X,
                        Y = p.Y,
                        Health = p.Health,
                        RoleType = p.RoleType, // or a RoleType property
                        RoleColor = p.RoleColor.Name
                    })
                    .ToList()
            };
            SendMessage(client, snapshot);
            SendCurrentMapStateTo(client);
        }
        private void SendCurrentMapStateTo(TcpClient client)
        {
            var mapState = new MapStateMessage
            {
                Width = Game.Instance.World.Map.Width,
                Height = Game.Instance.World.Map.Height,
                Tiles = new List<MapTileDto>()
            };

            for (int x = 0; x < Game.Instance.World.Map.Width; x++)
            {
                for (int y = 0; y < Game.Instance.World.Map.Height; y++)
                {
                    var tile = Game.Instance.World.Map.GetTile(x, y);

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
                    var player = Game.Instance.World.GetPlayer(id);
                    if (player != null)
                        Game.Instance.World.RemoveEntity(player);

                    clientRoles.Remove(id);
                }
                BroadcastState();
            }
        }
        private void BroadcastState()
        {
            StateMessage state;
            lock (locker)
            {
                var worldPlayers = Game.Instance.World.GetPlayers();
                Console.WriteLine($"BroadcastState: World has {worldPlayers.Count} players");

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
                        .ToList()
                };
            }

            Console.WriteLine($"Broadcasting state with {state.Players.Count} players to {clients.Count} clients");
            foreach (var clientId in clients.Keys)
            {
                Console.WriteLine($"Sending to client {clientId}");
            }

            foreach (var client in clients.Values)
            {
                SendMessage(client, state);
            }
        }

        private void SendMessage<T>(TcpClient client, T msg)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(msg) + "\n";
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            client.GetStream().Write(bytes, 0, bytes.Length);
        }
    }
}
