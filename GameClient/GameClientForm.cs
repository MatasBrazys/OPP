// File: GameClient/GameClientForm.cs
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameShared.Messages;
using GameShared.Types.Map;
using GameClient.Rendering;
using GameClient.Adapters;

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
        private readonly Dictionary<(int x, int y), TileRenderer> tileRenderers = new();
        private readonly Image grassSprite = Image.FromFile("../assets/grass.png");
        private readonly Image treeSprite = Image.FromFile("../assets/tree.png");
        private readonly Image houseSprite = Image.FromFile("../assets/house.png");
        private readonly Image appleSprite = Image.FromFile("../assets/apple.png");
        private readonly Image fishSprite = Image.FromFile("../assets/fish.png");
        private readonly Image waterSprite = Image.FromFile("../assets/water.png");
        private readonly Image sandSprite = Image.FromFile("../assets/sand.png");
        private readonly Image cherrySprite = Image.FromFile("../assets/cherry.jpg");
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
            // Register all tile sprites
            SpriteRegistry.Register("Grass", grassSprite);
            SpriteRegistry.Register("Tree", treeSprite);
            SpriteRegistry.Register("House", houseSprite);
            SpriteRegistry.Register("Apple", appleSprite);
            SpriteRegistry.Register("Fish", fishSprite);
            SpriteRegistry.Register("Water", waterSprite);
            SpriteRegistry.Register("Sand", sandSprite);
            SpriteRegistry.Register("Cherry", cherrySprite);

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

                        case "tile_update":
                            var tileUpdate = JsonSerializer.Deserialize<TileUpdateMessage>(line);
                            if (tileUpdate != null)
                            {
                                UpdateTile(tileUpdate.X, tileUpdate.Y, tileUpdate.TileType);
                            }
                            break;

                        case "copy_made":
                            var copyMessage = JsonSerializer.Deserialize<CopyMadeMessage>(line);
                            if (copyMessage != null)
                            {
                                Console.WriteLine($"=== PLAYER COPY CREATED ===");
                                Console.WriteLine($"Original Player: ID {copyMessage.OriginalPlayerId}, Role: {copyMessage.OriginalRole}");
                                Console.WriteLine($"New Clone: ID {copyMessage.NewPlayerId}, Role: {copyMessage.NewRole}");
                                Console.WriteLine($"Copy Type: {copyMessage.CopyType} Copy");
                                Console.WriteLine($"=== COPY COMPLETE ===");
                            }
                            break;

                        case "map_state":
                            var mapState = JsonSerializer.Deserialize<MapStateMessage>(line);
                            if (mapState != null)
                            {
                                // Clear existing map and tile renderers
                                map = new Map();
                                tileRenderers.Clear();

                                // Initialize map with correct dimensions
                                map.LoadFromDimensions(mapState.Width, mapState.Height);

                                // Update all tiles from server state
                                foreach (var tileDto in mapState.Tiles)
                                {
                                    UpdateTile(tileDto.X, tileDto.Y, tileDto.TileType);
                                }
                                Invalidate();
                                Console.WriteLine($"Received updated map state with {mapState.Tiles.Count} tiles");
                            }
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
        private void UpdateTile(int x, int y, string tileType)
        {
            TileData newTile = tileType.ToLower() switch
            {
                "grass" => new GrassTile(x, y),
                "cherry" => new CherryTile(x, y),
                "tree" => new TreeTile(x, y),
                "house" => new HouseTile(x, y),
                "apple" => new AppleTile(x, y),
                "fish" => new FishTile(x, y),
                "water" => new WaterTile(x, y),
                "sand" => new SandTile(x, y),
                _ => new GrassTile(x, y)
            };

            // Update the map
            map.SetTile(x, y, newTile);

            var renderableTile = new TileDataAdapter(newTile);
            tileRenderers[(x, y)] = new TileRenderer(renderableTile, TileSize);

            // Redraw
            Invalidate();
        }

        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            foreach (var renderer in tileRenderers.Values)
            {
                renderer.Draw(e.Graphics);
            }

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
