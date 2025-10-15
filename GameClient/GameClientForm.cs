// File: GameClient/GameClientForm.cs
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameShared.Messages;
using GameShared.Types.Map;
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
        private readonly Dictionary<int, Player> players = new();
        private int dx, dy;
        private readonly System.Windows.Forms.Timer gameTimer;
        private Map map = new();
        private readonly List<TileRenderer> tileRenderers = new();

        private readonly Image grassSprite = Image.FromFile("../assets/grass.png");
        private readonly Image treeSprite = Image.FromFile("../assets/tree.png");
        private readonly Image houseSprite = Image.FromFile("../assets/house.png");
        private readonly Image appleSprite = Image.FromFile("../assets/apple.png");
        private readonly Image fishSprite = Image.FromFile("../assets/fish.png");
        private readonly Image waterSprite = Image.FromFile("../assets/water.png");
        private readonly Image sandSprite = Image.FromFile("../assets/sand.png");
        private const int TileSize = 128;

        public GameClientForm()
        {
            KeyDown += GameClientForm_KeyDown;
            KeyUp += GameClientForm_KeyUp;
            Paint += GameClientForm_Paint;
            Load += GameClientForm_Load;

            gameTimer = new System.Windows.Forms.Timer { Interval = 30 };
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
        }

        private void GameClientForm_Load(object? sender, EventArgs e)
        {
            map.LoadFromText("../assets/map.txt");

            tileRenderers.Clear();
            for (int x = 0; x < map.Width; x++)
            for (int y = 0; y < map.Height; y++)
            {
                var tile = map.GetTile(x, y);
                Image sprite = tile switch
                {
                    GrassTile => grassSprite,
                    TreeTile => treeSprite,
                    HouseTile => houseSprite,
                    AppleTile => appleSprite,
                    FishTile => fishSprite,
                    WaterTile => waterSprite,
                    SandTile => sandSprite,
                    _ => grassSprite
                };
                tileRenderers.Add(new TileRenderer(tile, sprite, TileSize));
            }

            // Connect to server
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
            if (stream == null) return;

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
                            if (state == null) break;

                            lock (players)
                            {
                                players.Clear();
                                foreach (var ps in state.Players)
                                {
                                    players[ps.Id] = new Player(
                                        ps.Id,
                                        ps.X,
                                        ps.Y,
                                        ps.Id == myId ? Color.Blue : Color.Red);
                                }
                            }
                            Invalidate();
                            break;

                        case "error":
                            var error = JsonSerializer.Deserialize<ErrorMessage>(line);
                            MessageBox.Show($"Server error: {error?.Detail}");
                            break;

                        case "collision":
                            var collision = JsonSerializer.Deserialize<CollisionMessage>(line);
                            Console.WriteLine($"Collision occured at : {collision.X} {collision.Y}");
                            break;

                        case "goodbye":
                            MessageBox.Show("Server closed connection.");
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
            if (stream == null || myId == 0) return;

            var msg = new InputMessage { Dx = dx, Dy = dy };
            var json = JsonSerializer.Serialize(msg) + "\n";
            var data = Encoding.UTF8.GetBytes(json);

            try { stream.Write(data, 0, data.Length); } catch { }

            Invalidate();
        }

        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            foreach (var r in tileRenderers)
                r.Draw(e.Graphics);

            lock (players)
            {
                foreach (var p in players.Values)
                {
                    p.Draw(e.Graphics);
                    e.Graphics.DrawString($"ID:{p.Id} ({p.X / TileSize},{p.Y / TileSize})",
                        SystemFonts.DefaultFont, Brushes.Black, p.X, p.Y - 25);
                }
            }

            using var pen = new Pen(Color.Black, 1);
            for (int x = 0; x <= map.Width; x++)
                e.Graphics.DrawLine(pen, x * TileSize, 0, x * TileSize, map.Height * TileSize);

            for (int y = 0; y <= map.Height; y++)
                e.Graphics.DrawLine(pen, 0, y * TileSize, map.Width * TileSize, y * TileSize);
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
