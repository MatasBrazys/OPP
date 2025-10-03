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
using GameShared.Map;
using GameShared.Types;
using GameClient.Rendering;

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
        private Map map;
        private List<TileRenderer> tileRenderers = new();

        private Image grassSprite = Image.FromFile("../assets/grass.png");
        private Image treeSprite = Image.FromFile("../assets/tree.png");
        private Image houseSprite = Image.FromFile("../assets/house.png");
        private int TileSize = 128;

        

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
            // --- Load map and tile renderers ---
            map = new Map();
            var json = File.ReadAllText("../assets/map.json");
            map.LoadFromJson(json);

            tileRenderers.Clear();
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);
                    Image sprite = tile switch
                    {
                        GrassTile => grassSprite,
                        TreeTile => treeSprite,
                        HouseTile => houseSprite,
                        _ => grassSprite
                    };
                    tileRenderers.Add(new TileRenderer(tile, sprite, TileSize));
                }
            }

            // --- Connect to server ---
            try
            {
                client = new TcpClient("25.55.216.17", 5000);
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
            using var reader = new StreamReader(stream, Encoding.UTF8);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
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

                        case "error":
                            var error = JsonSerializer.Deserialize<ErrorMessage>(line);
                            MessageBox.Show($"Server error: {error?.Detail}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;

                        case "goodbye":
                            MessageBox.Show("Disconnected: Server closed connection.", "Goodbye", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Application.Exit();
                            break;
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
            if (stream != null && myId != 0)
            {
                var inputMsg = new InputMessage { Dx = dx, Dy = dy };
                var json = JsonSerializer.Serialize(inputMsg) + "\n";
                var data = Encoding.UTF8.GetBytes(json);
                try { stream.Write(data, 0, data.Length); } catch { }
            }

            Invalidate(); // trigger redraw
        }
        

        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            // Draw tiles
            foreach (var r in tileRenderers)
                r.Draw(e.Graphics);

            // Draw players
            lock (players)
            {
                foreach (var p in players.Values)
                {
                    p.Draw(e.Graphics);

                    e.Graphics.DrawString(
                        $"ID:{p.Id} ({p.X / TileSize},{p.Y / TileSize})",
                        SystemFonts.DefaultFont,
                        Brushes.Black,
                        p.X, p.Y - 25
                    );
                }
            }
            using (var pen = new Pen(Color.Black, 1))
            {
                for (int x = 0; x <= map.Width; x++)
                    e.Graphics.DrawLine(pen, x * TileSize, 0, x * TileSize, map.Height * TileSize);

                for (int y = 0; y <= map.Height; y++)
                    e.Graphics.DrawLine(pen, 0, y * TileSize, map.Width * TileSize, y * TileSize);
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
