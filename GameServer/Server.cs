//./GameServer/Server.cs
using GameShared.Messages;
using GameShared.Types.DTOs;
using GameShared.Types.Map;
using GameShared.Types.Map.Decorators;
using GameShared.Types.Players;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Linq;
using GameShared;
using GameServer.Mediator;
using GameServer.Facades;

namespace GameServer
{
    public class Server : IClientNotifier, IServerParticipant
    {
        static TcpListener? listener;
        static Dictionary<int, TcpClient> clients = new();
        static Dictionary<int, string> clientRoles = new();
        static int nextId = 1;
        static object locker = new object();
        private static int nextPlayerId = 1000;
        private static readonly object IdLock = new object();

        static readonly string[] AllRoles = new[] { "mage", "defender", "hunter" };
        private const bool EnableTileLogging = false;

        private IGameMediator? _mediator;


        public void Start(int port)
        {
            TileLogSink.IsEnabled = EnableTileLogging;
            TileLogSink.Logger = EnableTileLogging ? message => Console.WriteLine(message) : null;

            if (_mediator == null)
                throw new InvalidOperationException("Mediator has not been initialized. Call InitializeMediator before Start.");

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

        public void InitializeMediator(GameWorldFacade facade)
        {
            // create mediator (do not assign to _mediator yet)
            var mediator = new GameMediator(facade, this);

            // participant-driven subscription: ask each participant to subscribe themselves
            // they will call mediator.RegisterParticipant(this) and then receive OnMediatorAttached
            facade.SubscribeToMediator(mediator);
            SubscribeToMediator(mediator);
        }

        // Participant-side helper: call this to subscribe the server to the mediator.
        // Do NOT set _mediator here — OnMediatorAttached will be called by the mediator and should set it.
        public void SubscribeToMediator(IGameMediator mediator)
        {
            mediator.RegisterParticipant(this);
        }

        // IMediatorParticipant implementation
        public void OnMediatorAttached(IGameMediator mediator)
        {
            // mediator instance supplied by registry — keep reference
            _mediator = mediator;
        }

        public void OnMediatorDetached()
        {
            _mediator = null;
        }

        private void HandleInput(int id, InputMessage input)
        {
            lock (locker)
            {
                _mediator?.HandleInput(id, input);
            }
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

        /// <summary>
        /// Broadcast a message to all connected clients
        /// </summary>
        public void BroadcastMessage<T>(T message)
        {
            BroadcastToAll(message);
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
                    var tileType = tile?.TileType ?? "grass";

                    if (_mediator != null && _mediator.IsTileReplacedWithGrass(x, y))
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
                            TileType = tileType
                        });
                    }
                }
            }
            SendMessage(client, mapState);
            Console.WriteLine("Sent current map state to new client (with grass overrides applied)");
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
                            if (input != null)
                                HandleInput(id, input);
                            break;
                        case "attack":
                            var attack = JsonSerializer.Deserialize<AttackMessage>(line);
                            if (attack != null)
                            {
                                if (attack.PlayerId == 0) attack.PlayerId = id;
                                _mediator?.HandleAttack(attack);
                            }
                            break;
                        case "ping":
                            var ping = JsonSerializer.Deserialize<PingMessage>(line);
                            if (ping != null)
                            {
                                SendMessage(client, new PongMessage { T = ping.T });
                            }
                            break;
                        case "position_restore":
                            _mediator?.UndoLastMove(id);
                            break;

                        case "plant_action":
                            var plantAction = JsonSerializer.Deserialize<PlantActionMessage>(line);
                            if (plantAction != null)
                            {
                                _mediator?.HandlePlantAction(plantAction);
                            }
                            break;

                        case "harvest_action":
                            var harvestAction = JsonSerializer.Deserialize<HarvestActionMessage>(line);
                            if (harvestAction != null)
                            {
                                _mediator?.HandleHarvestAction(harvestAction);
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

                // Immediate broadcast needed when player disconnects
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

        public void SendToClient<T>(int playerId, T message)
        {
            TcpClient? targetClient = null;
            lock (locker)
            {
                clients.TryGetValue(playerId, out targetClient);
            }

            if (targetClient != null)
            {
                SendMessage(targetClient, message);
            }
            else
            {
                Console.WriteLine($"Could not find client for player {playerId}");
            }
        }

        internal static int GetNextPlayerIdStatic()
        {
            lock (IdLock)
            {
                return nextPlayerId++;
            }
        }
    }
}
