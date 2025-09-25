using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

class PlayerState
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

class Server
{
    static TcpListener server;
    static Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();
    static Dictionary<int, PlayerState> players = new Dictionary<int, PlayerState>();
    static int nextId = 1;
    static object locker = new object();

    static void Main(string[] args)
    {
        server = new TcpListener(IPAddress.Any, 5000);
        server.Start();
        Console.WriteLine("Server started on port 5000...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            int id;
            lock (locker)
            {
                id = nextId++;
                clients[id] = client;
                players[id] = new PlayerState { Id = id, X = 100, Y = 100 };
            }
            Console.WriteLine($"Client {id} connected!");

            Thread t = new Thread(() => HandleClient(id, client));
            t.Start();
        }
    }

    static void HandleClient(int id, TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            try
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                PlayerState updated = JsonSerializer.Deserialize<PlayerState>(msg);

                lock (locker)
                {
                    players[id].X = updated.X;
                    players[id].Y = updated.Y;
                }

                BroadcastGameState();
            }
            catch
            {
                break;
            }
        }

        lock (locker)
        {
            clients.Remove(id);
            players.Remove(id);
        }

        Console.WriteLine($"Client {id} disconnected.");
        BroadcastGameState();
    }

    static void BroadcastGameState()
    {
        string json;
        lock (locker)
        {
            json = JsonSerializer.Serialize(players.Values);
        }

        byte[] data = Encoding.UTF8.GetBytes(json);
        foreach (var kv in clients)
        {
            try
            {
                kv.Value.GetStream().Write(data, 0, data.Length);
            }
            catch { }
        }
    }
}
