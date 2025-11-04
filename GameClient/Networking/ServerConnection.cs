// ./GameClient/Networking/ServerConnection.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace GameClient.Networking
{
    public class ServerConnection : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient? _client;
        private NetworkStream? _stream;
        private Thread? _receiveThread;
        private bool _running;

        public event Action<string>? OnRawMessageReceived;
        public event Action<Exception>? OnConnectionError;

        public ServerConnection(string host = "127.0.0.1", int port = 5000)
        {
            _host = host;
            _port = port;
        }

        public bool IsConnected => _client?.Connected ?? false;

        public void Connect()
        {
            try
            {
                _client = new TcpClient(_host, _port);
                _stream = _client.GetStream();
                _running = true;
                _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                _receiveThread.Start();
            }
            catch (Exception ex)
            {
                OnConnectionError?.Invoke(ex);
                Dispose();
            }
        }

        private void ReceiveLoop()
        {
            if (_stream == null) return;
            using var reader = new StreamReader(_stream, Encoding.UTF8);
            string? line;
            try
            {
                while (_running && (line = reader.ReadLine()) != null)
                {
                    OnRawMessageReceived?.Invoke(line);
                }
            }
            catch (Exception ex)
            {
                OnConnectionError?.Invoke(ex);
            }
            finally
            {
                Dispose();
            }
        }

        public void SendRaw(string json)
        {
            if (_stream == null) return;
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json + "\n");
                _stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                OnConnectionError?.Invoke(ex);
            }
        }

        public void Dispose()
        {
            _running = false;
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
            _receiveThread = null;
        }
    }
}
