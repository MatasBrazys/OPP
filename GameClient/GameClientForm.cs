using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using GameShared.Messages;
using GameShared.Types.Map;
using GameClient.Rendering;
using GameClient.Adapters;
using GameShared;
using GameShared.Types.DTOs;

namespace GameClient
{
    public partial class GameClientForm : BaseForm
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private Thread? receiveThread;
        private int myId;
        private readonly Dictionary<int, PlayerRenderer> playerRenderers = new();
        private readonly Dictionary<int, EnemyRenderer> enemyRenderers = new();
        private readonly HashSet<Keys> pressedKeys = new();
        private float moveX, moveY;
        private const float MoveSpeed = 1f;

        private readonly System.Windows.Forms.Timer gameTimer;
        private Map map = new();
        private readonly Dictionary<(int x, int y), TileRenderer> tileRenderers = new();
        private const int TileSize = GameConstants.TILE_SIZE;

        private readonly CommandInvoker commandInvoker = new();

        // Tile sprites
        private readonly Image grassSprite = Image.FromFile("../assets/grass.png");
        private readonly Image treeSprite = Image.FromFile("../assets/tree.png");
        private readonly Image houseSprite = Image.FromFile("../assets/house.png");
        private readonly Image appleSprite = Image.FromFile("../assets/apple.png");
        private readonly Image fishSprite = Image.FromFile("../assets/fish.png");
        private readonly Image waterSprite = Image.FromFile("../assets/water.png");
        private readonly Image sandSprite = Image.FromFile("../assets/sand.png");
        private readonly Image cherrySprite = Image.FromFile("../assets/cherry.jpg");

        // Enemy sprites
        private readonly Image slimeSprite = Image.FromFile("../assets/slime.png");

        private readonly List<SlashEffect> activeSlashes = new();


        public GameClientForm()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

            KeyDown += GameClientForm_KeyDown;
            KeyUp += GameClientForm_KeyUp;
            MouseClick += GameClientForm_MouseClick;
            Paint += GameClientForm_Paint;
            Load += GameClientForm_Load;

            gameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            gameTimer.Tick += GameLoop;
            gameTimer.Start();
        }

        private void GameClientForm_Load(object? sender, EventArgs e)
        {
            // Register tile sprites
            SpriteRegistry.Register("Grass", grassSprite);
            SpriteRegistry.Register("Tree", treeSprite);
            SpriteRegistry.Register("House", houseSprite);
            SpriteRegistry.Register("Apple", appleSprite);
            SpriteRegistry.Register("Fish", fishSprite);
            SpriteRegistry.Register("Water", waterSprite);
            SpriteRegistry.Register("Sand", sandSprite);
            SpriteRegistry.Register("Cherry", cherrySprite);

            // Register player sprites
            SpriteRegistry.Register("Mage", Image.FromFile("../assets/mage.png"));
            SpriteRegistry.Register("Hunter", Image.FromFile("../assets/hunter.png"));
            SpriteRegistry.Register("Defender", Image.FromFile("../assets/defender.png"));

            // Register enemy sprites
            SpriteRegistry.Register("Slime", slimeSprite);

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
                            if (welcome != null) myId = welcome.Id;
                            break;

                        case "tile_update":
                            var tileUpdate = JsonSerializer.Deserialize<TileUpdateMessage>(line);
                            if (tileUpdate != null) UpdateTile(tileUpdate.X, tileUpdate.Y, tileUpdate.TileType);
                            break;

                        case "map_state":
                            var mapState = JsonSerializer.Deserialize<MapStateMessage>(line);
                            if (mapState != null)
                            {
                                map = new Map();
                                tileRenderers.Clear();
                                map.LoadFromDimensions(mapState.Width, mapState.Height);
                                foreach (var tileDto in mapState.Tiles)
                                    UpdateTile(tileDto.X, tileDto.Y, tileDto.TileType);
                                Invalidate();
                            }
                            break;

                        case "state":
                            var state = JsonSerializer.Deserialize<StateMessage>(line);
                            if (state == null) break;

                            UpdatePlayers(state.Players);
                            UpdateEnemies(state.Enemies);
                            Invalidate();
                            break;

                        case "error":
                            var error = JsonSerializer.Deserialize<ErrorMessage>(line);
                            MessageBox.Show($"Server error: {error?.Detail}");
                            break;

                        case "collision":
                            var collision = JsonSerializer.Deserialize<CollisionMessage>(line);
                            Console.WriteLine($"Collision at: {collision.X}, {collision.Y}");
                            break;
                        case "attack_animation":
                            var animMsg = JsonSerializer.Deserialize<AttackAnimationMessage>(line);
                            if (animMsg != null)
                            {
                                float animX = animMsg.AnimX;
                                float animY = animMsg.AnimY;
                                float rotation = 0f;
                                if (float.TryParse(animMsg.Direction, out var r)) rotation = r;
                                float radius = animMsg.Radius;
                                lock (activeSlashes)
                                {
                                    activeSlashes.Add(new SlashEffect(animX, animY, radius, rotation));
                                }
                            }
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

        private void UpdatePlayers(List<PlayerDto> players)
        {
            lock (playerRenderers)
            {
                foreach (var ps in players)
                {
                    if (!playerRenderers.TryGetValue(ps.Id, out var existing))
                    {
                        var sprite = SpriteRegistry.GetSprite(ps.RoleType);
                        var isLocal = ps.Id == myId;
                        var renderer = new PlayerRenderer(ps.Id, ps.RoleType, ps.X, ps.Y, sprite, isLocal);
                        playerRenderers[ps.Id] = renderer;
                    }
                    else
                    {
                        existing.SetTarget(ps.X, ps.Y);
                    }
                }

                var serverPlayerIds = players.Select(x => x.Id).ToHashSet();
                var toRemovePlayers = playerRenderers.Keys.Where(k => !serverPlayerIds.Contains(k)).ToList();
                foreach (var rem in toRemovePlayers) playerRenderers.Remove(rem);
            }
        }

        private void UpdateEnemies(List<EnemyDto> enemies)
        {
            lock (enemyRenderers)
            {
                foreach (var es in enemies)
                {
                    if (!enemyRenderers.TryGetValue(es.Id, out var existing))
                    {
                        var sprite = SpriteRegistry.GetSprite(es.EnemyType) ?? slimeSprite;
                        var renderer = new EnemyRenderer(es.Id, es.EnemyType, es.X, es.Y, sprite, es.Health, es.MaxHealth);
                        enemyRenderers[es.Id] = renderer;
                    }
                    else
                    {
                        existing.SetTarget(es.X, es.Y);
                        existing.CurrentHP = es.Health;
                        existing.MaxHP = es.MaxHealth;
                    }
                }

                var serverEnemyIds = enemies.Select(x => x.Id).ToHashSet();
                var toRemoveEnemies = enemyRenderers.Keys.Where(k => !serverEnemyIds.Contains(k)).ToList();
                foreach (var rem in toRemoveEnemies) enemyRenderers.Remove(rem);
            }
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (client == null || myId == 0) return;

            UpdateMovement();

            // Send WalkCommand
            var walkCommand = new WalkCommand(client, myId, moveX, moveY);
            commandInvoker.AddCommand(walkCommand);

            // Execute queued commands
            commandInvoker.ExecuteCommands();

            Invalidate();
        }

        private void UpdateMovement()
        {
            float dx = 0f, dy = 0f;

            if (pressedKeys.Contains(Keys.W)) dy -= 1f;
            if (pressedKeys.Contains(Keys.S)) dy += 1f;
            if (pressedKeys.Contains(Keys.A)) dx -= 1f;
            if (pressedKeys.Contains(Keys.D)) dx += 1f;

            float magnitude = (float)Math.Sqrt(dx * dx + dy * dy);
            if (magnitude > 0)
            {
                dx /= magnitude;
                dy /= magnitude;
            }

            moveX = dx * MoveSpeed;
            moveY = dy * MoveSpeed;
        }

        private void GameClientForm_MouseClick(object sender, MouseEventArgs e)
        {
            if (myId == 0 || client == null) return;
            if (!playerRenderers.TryGetValue(myId, out var renderer)) return;

            // Send raw pixel click coords to server
            float clickX = e.X;
            float clickY = e.Y;

            var attackCommand = new AttackCommand(client, myId, clickX, clickY, "slash");
            commandInvoker.AddCommand(attackCommand);

            // Do NOT spawn local slash — server will broadcast animation to everyone (including self).
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

            map.SetTile(x, y, newTile);
            tileRenderers[(x, y)] = new TileRenderer(new TileDataAdapter(newTile), TileSize);
            Invalidate();
        }

        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            foreach (var renderer in tileRenderers.Values) renderer.Draw(e.Graphics);

            lock (playerRenderers)
                foreach (var renderer in playerRenderers.Values) renderer.Draw(e.Graphics);

            lock (enemyRenderers)
                foreach (var renderer in enemyRenderers.Values) renderer.Draw(e.Graphics);


            lock (activeSlashes)
            {
                for (int i = activeSlashes.Count - 1; i >= 0; i--)
                {
                    var s = activeSlashes[i];
                    s.Draw(e.Graphics);
                    if (s.IsFinished) activeSlashes.RemoveAt(i);
                }
            }

            using var pen = new Pen(Color.Black, 1);
            for (int x = 0; x <= map.Width; x++)
                e.Graphics.DrawLine(pen, x * TileSize, 0, x * TileSize, map.Height * TileSize);
            for (int y = 0; y <= map.Height; y++)
                e.Graphics.DrawLine(pen, 0, y * TileSize, map.Width * TileSize, y * TileSize);
        }

        private void GameClientForm_KeyDown(object? sender, KeyEventArgs e) => pressedKeys.Add(e.KeyCode);
        private void GameClientForm_KeyUp(object? sender, KeyEventArgs e) => pressedKeys.Remove(e.KeyCode);
    }
}
