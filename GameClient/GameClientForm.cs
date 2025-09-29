using System;
using System.Collections.Generic;
using System.Drawing;
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
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine($"Received: {line}");
                try
                {
                    var doc = JsonDocument.Parse(line);
                    var type = doc.RootElement.GetProperty("Type").GetString();
                    switch (type)
                    {
                        case "welcome":
                            var welcome = JsonSerializer.Deserialize<WelcomeMessage>(line);
                            myId = welcome.Id;
                            break;
                        case "state":
                            var state = JsonSerializer.Deserialize<StateMessage>(line);
                            lock (players)
                            {
                                players.Clear();
                                foreach (var ps in state?.Players)
                                {
                                    players[ps.Id] = new Player(ps.Id, ps.X, ps.Y, ps.Id == myId ? Color.Blue : Color.Red);
                                }
                            }
                            Invalidate();
                            break;
                        case "pong":
                            // Optionally handle pong
                            break;
                        case "error":
                            var error = JsonSerializer.Deserialize<ErrorMessage>(line);
                            MessageBox.Show($"Server error: {error?.Detail}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        case "goodbye":
                            MessageBox.Show("Disconnected: Server closed connection.", "Goodbye", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Application.Exit();
                            break;
                        // Add more cases as needed
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ReceiveLoop error: " + ex);
                }
            }
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            // Only send input if connected and have a valid id
            if (stream != null && myId != 0)
            {
                var inputMsg = new InputMessage { Dx = dx, Dy = dy };
                var json = JsonSerializer.Serialize(inputMsg) + "\n";
                var data = Encoding.UTF8.GetBytes(json);
                try { stream.Write(data, 0, data.Length); } catch { }
            }
        }

        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.DrawString($"MyID: {myId}", SystemFonts.DefaultFont, Brushes.Black, 10, 10);
            e.Graphics.DrawString($"Players: {players.Count}", SystemFonts.DefaultFont, Brushes.Black, 10, 30);
            lock (players)
            {
                foreach (var p in players.Values)
                {
                    p.Draw(e.Graphics);

                    e.Graphics.DrawString($"ID:{p.Id} ({p.X},{p.Y})", 
                        SystemFonts.DefaultFont, Brushes.Black, p.X, p.Y - 20);
                }
            }
        }

        private void GameClientForm_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W: dy = -1; break;
                case Keys.S: dy = 1; break;
                case Keys.A: dx = -1; break;
                case Keys.D: dx = 1; break;
            }
        }

        private void GameClientForm_KeyUp(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.W:
                case Keys.S: dy = 0; break;
                case Keys.A:
                case Keys.D: dx = 0; break;
            }
        }
    }
}
