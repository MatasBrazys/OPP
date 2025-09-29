using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameServer.messages;
using GameServer.types;

namespace GameServer {

    class Server
    {
        static TcpListener listener;
        static Dictionary<int, TcpClient> clients = new();
        static Dictionary<int, PlayerState> players = new();
        static int nextId = 1;
        static object locker = new object();

        public void Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"Server started on port {port}");

            while (true)
            {
                var client = listener.AcceptTcpClient();
                int id;
                lock (locker)
                {
                    id = nextId++;
                    clients[id] = client;
                    players[id] = new PlayerState { Id = id, X = 100, Y = 100 };
                }
                SendMessage(client, new WelcomeMessage { Id = id, Type = "welcome" });
                SendStateTo(client);

                var thread = new Thread(() => HandleClient(id, client));
                thread.Start();
            }
        }
        private void SendStateTo(TcpClient client)
        {
            var snapshot = new StateMessage
            {
                ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Players = players.Select(kv => new PlayerState
                {
                    Id = kv.Key,
                    X = kv.Value.X,
                    Y = kv.Value.Y
                }).ToList()
            };
            SendMessage(client, snapshot); // make sure this includes "type":"state"
        }

        private void HandleClient(int id, TcpClient client)
        {
            Console.WriteLine($"Started handling client {id}");
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            string? line;
            try
            {
                while ((line = reader.ReadLine()) != null)
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
                        // Add more cases as needed
                        default:
                            SendMessage(client, new ErrorMessage { Code = "bad_message", Detail = $"unknown type: {type}" });
                            break;
                    }
                }
            }
            catch
            {
                // Handle disconnect
            }
            finally
            {
                lock (locker)
                {
                    clients.Remove(id);
                    players.Remove(id);
                }
                BroadcastState();
            }
        }

        private void HandleInput(int id, InputMessage input)
        {
            lock (locker)
            {
                if (players.TryGetValue(id, out var player))
                {
                    player.X += input.Dx * 5;
                    player.Y += input.Dy * 5;
                }
            }
            BroadcastState();
        }

        private void BroadcastState()
        {
            StateMessage state;
            lock (locker)
            {
                state = new StateMessage
                {
                    ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Players = players.Values.ToList()
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

















    //    static void Main(string[] args)
    //    {
    //        server = new TcpListener(IPAddress.Any, 5000);
    //        server.Start();
    //        Console.WriteLine("Server started on port 5000...");

    //        while (true)
    //        {
    //            TcpClient client = server.AcceptTcpClient();
    //            int id;
    //            lock (locker)
    //            {
    //                id = nextId++;
    //                clients[id] = client;
    //                players[id] = new PlayerState { Id = id, X = 100, Y = 100 };
    //            }
    //            Console.WriteLine($"Client {id} connected!");

    //            Thread t = new Thread(() => HandleClient(id, client));
    //            t.Start();
    //        }
    //    }

    //    static void HandleClient(int id, TcpClient client)
    //    {
    //        using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
    //        string? line;

    //        try
    //        {
    //            while ((line = reader.ReadLine()) != null)
    //            {
    //                PlayerState updated = JsonSerializer.Deserialize<PlayerState>(line)!;

    //                lock (locker)
    //                {
    //                    players[id].X = updated.X;
    //                    players[id].Y = updated.Y;
    //                }

    //                BroadcastGameState();
    //            }
    //        }
    //        catch
    //        {
    //            // client disconnected
    //        }

    //        lock (locker)
    //        {
    //            clients.Remove(id);
    //            players.Remove(id);
    //        }

    //        Console.WriteLine($"Client {id} disconnected.");
    //        BroadcastGameState();
    //    }

    //    static void BroadcastGameState()
    //    {
    //        string json;
    //        lock (locker)
    //        {
    //            json = JsonSerializer.Serialize(players.Values) + "\n"; // <-- add newline
    //        }

    //        byte[] data = Encoding.UTF8.GetBytes(json);
    //        foreach (var kv in clients)
    //        {
    //            try
    //            {
    //                kv.Value.GetStream().Write(data, 0, data.Length);
    //            }
    //            catch { }
    //        }
    //    }
    //}
}