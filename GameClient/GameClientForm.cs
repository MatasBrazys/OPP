using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using GameShared.Messages;

namespace GameClient
{
    public partial class GameClientForm : BaseForm
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private Thread? receiveThread;
        private int myId;
        private Dictionary<int, Player> players = new();
        private int dx, dy; // Movement direction
        private System.Windows.Forms.Timer gameTimer;

        public GameClientForm()
        {
            this.KeyDown += GameClientForm_KeyDown;
            this.KeyUp += GameClientForm_KeyUp;
            this.Paint += GameClientForm_Paint;
            this.Load += GameClientForm_Load;

            gameTimer = new System.Windows.Forms.Timer();
            gameTimer.Interval = 30;
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
        }

        private void GameClientForm_Load(object? sender, EventArgs e)
        {
            try
            {
                client = new TcpClient("127.0.0.1", 5000);
                stream = client.GetStream();
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
                receiveThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
            }
        }

        private void ReceiveLoop()
        {
            Console.WriteLine("Client started receive loop");
            using var reader = new StreamReader(stream!, Encoding.UTF8);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine($"Received: {line}");
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var type = doc.RootElement.GetProperty("Type").GetString();
                    switch (type)
                    {
                        case "welcome":
                            var welcome = JsonSerializer.Deserialize<WelcomeMessage>(line);
                            myId = welcome!.Id;
                            break;
                        case "state":
                            var state = JsonSerializer.Deserialize<StateMessage>(line);
                            lock (players)
                            {
                                players.Clear();
                                foreach (var ps in state!.Players)
                                    players[ps.Id] = new Player(ps.Id, ps.X, ps.Y, ps.Id == myId ? Color.Blue : Color.Red);
                            }
                            Invalidate();
                            break;
                        case "pong":
                            break;
                        case "error":
