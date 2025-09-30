using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.IO;

class PlayerState
{
    public int Id { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

class Server
{
    private static Server? _instance;
    private static readonly object locker = new object();

    private TcpListener server;
    private Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();
    private Dictionary<int, PlayerState> players = new Dictionary<int, PlayerState>();
    private int nextId = 1;

    public static Server Instance
    {
        get
        {
            lock (locker)
            {
                return _instance ??= new Server();
            }
        }
    }

    private Server()
    {
        server = new TcpListener(IPAddress.Any, 5000);
    }

    public void Start()
    {
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

    private void HandleClient(int id, TcpClient client)
    {
        using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
        string? line;

        try
        {
            while ((line = reader.ReadLine()) != null)
            {
                PlayerState updated = JsonSerializer.Deserialize<PlayerState>(line)!;

                lock (locker)
                {
                    players[id].X = updated.X;
                    players[id].Y = updated.Y;
                }

                BroadcastGameState();
            }
        }
        catch
        {
            // client disconnected
        }

        lock (locker)
        {
            clients.Remove(id);
            players.Remove(id);
        }

        Console.WriteLine($"Client {id} disconnected.");
        BroadcastGameState();
    }

    private void BroadcastGameState()
    {
        string json;
        lock (locker)
        {
            json = JsonSerializer.Serialize(players.Values) + "\n";
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
