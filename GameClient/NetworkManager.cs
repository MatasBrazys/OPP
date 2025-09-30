using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace GameClient
{
    public class NetworkManager
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private Thread? receiveThread;

        public event Action<List<NetworkMessage>>? OnGameStateReceived;

        public bool IsConnected => client != null && client.Connected;

        public void Connect(string ip, int port)
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        public void Send(NetworkMessage msg)
        {
            if (stream == null) return;

            string json = JsonSerializer.Serialize(msg) + "\n";
            byte[] data = Encoding.UTF8.GetBytes(json);

            try
            {
                stream.Write(data, 0, data.Length);
            }
            catch
            {
                // connection lost
            }
        }

        private void ReceiveData()
        {
            try
            {
                if (stream == null) return;
                using var reader = new StreamReader(stream, Encoding.UTF8);

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var states = JsonSerializer.Deserialize<List<NetworkMessage>>(line);
                    if (states != null)
                        OnGameStateReceived?.Invoke(states);
                }
            }
            catch
            {
                // disconnected
            }
        }
    }
}
