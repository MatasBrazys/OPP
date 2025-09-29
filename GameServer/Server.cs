using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameShared.Messages;
using GameShared.Types;
using GameShared.Types.DTOs;

namespace GameServer
{
    public class Server
    {
        static TcpListener listener;
        static Dictionary<int, TcpClient> clients = new();
        static Dictionary<int, string> clientRoles = new();
        static int nextId = 1;
        static object locker = new object();

        static readonly string[] AllRoles = new[] { "hunter", "mage", "defender" };

        public void Start(int port)
        {
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
                    var player = Game.Instance.World.GetPlayer(id);
                    if (player != null)
                        Game.Instance.World.RemoveEntity(player);

                    // Free role for reuse
                    clientRoles.Remove(id);
                }
                BroadcastState();
            }
        }

        private void HandleInput(int id, InputMessage input)
        {
            lock (locker)
            {
                var player = Game.Instance.World.GetPlayer(id);
                if (player != null)
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