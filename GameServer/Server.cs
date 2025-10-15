using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameShared.Messages;
using GameShared.Types.DTOs;
using GameServer.Events;
using GameServer.Commands;
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

        static readonly string[] AllRoles = new[] { "hunter", "mage", "defender" };

        private CollisionDetector _collisionDetector;
        private List<CommandHandler> _commandHandlers;

        public void Start(int port)
        {
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
                var currentTile = Game.Instance.World.Map.GetTile(currentTileX, currentTileY);

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
                    player.X = newX;
                    player.Y = newY;

                    player.OnMoveTile(targetTile);
                }

                var players = Game.Instance.World.GetPlayers();
                _collisionDetector.CheckCollisions(players);
            }

            BroadcastState();
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
                Console.WriteLine($"Server.BroadcastState reading from world: {Game.Instance.World.GetHashCode()}");
                state = new StateMessage
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
            }
            Console.WriteLine($"Broadcasting state with {state.Players.Count} players");
            foreach (var player in state.Players)
            {
                Console.WriteLine($"Player {player.Id} at ({player.X}, {player.Y})");
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