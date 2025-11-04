using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Drawing;
using System.Windows.Forms;
using GameShared.Messages;
using GameShared.Types.Map;
using GameClient.Rendering;
using GameClient.Adapters;
using GameShared.Messages;
using GameShared.Types.Map;
using GameShared.Types.DTOs;
using GameClient.Theming;

namespace GameClient
{
    public partial class GameClientForm : BaseForm
    {
        // Core collaborators
        private readonly ServerConnection _connection;
        private readonly EntityManager _entityManager;
        private readonly TileManager _tileManager;
        private readonly AnimationManager _animManager;
        private readonly InputHandler _inputHandler;
        private readonly CommandInvoker _commandInvoker;

        private int _myId;
        private readonly System.Windows.Forms.Timer _gameTimer;
        private Map _map = new();

        private const int TileSize = GameConstants.TILE_SIZE;

        private readonly CommandInvoker commandInvoker = new();

        private readonly IGameThemeFactory summerFactory;
        private readonly IGameThemeFactory winterFactory;
        private ThemeMode currentTheme;

        private ITileSpriteSet tileSpriteSet;
        private IPlayerSpriteSet playerSpriteSet;
        private IEnemySpriteSet enemySpriteSet;
        private IUiPalette uiPalette;
        private Image defaultEnemySprite;

        private readonly List<SlashEffect> activeSlashes = new();
        private readonly List<MageFireballEffect> activeFireballs = new();
        private readonly List<ArrowEffect> activeArrows = new();




        public GameClientForm() : this(new SummerGameThemeFactory(), new WinterGameThemeFactory())
        {
        }

        public GameClientForm(IGameThemeFactory summerFactory, IGameThemeFactory winterFactory)
        {
            this.summerFactory = summerFactory;
            this.winterFactory = winterFactory;
            ApplyTheme(ThemeMode.Winter, refreshSprites: false);

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Load += GameClientForm_Load;
            Paint += GameClientForm_Paint;
        }

        private void ApplyTheme(ThemeMode mode, bool refreshSprites)
        {
            currentTheme = mode;
            var factory = mode == ThemeMode.Summer ? summerFactory : winterFactory;

            tileSpriteSet = factory.CreateTileSpriteSet();
            playerSpriteSet = factory.CreatePlayerSpriteSet();
            enemySpriteSet = factory.CreateEnemySpriteSet();
            uiPalette = factory.CreateUiPalette();
            defaultEnemySprite = enemySpriteSet.Sprites.TryGetValue("Slime", out var slime)
                ? slime
                : ThemeSpriteLoader.LoadEnemySprite("../assets/slime.png", Color.DarkSeaGreen);

            UpdateWindowTitle();

            if (!refreshSprites)
                return;

            SpriteRegistry.Clear();
            RegisterThemeSprites();
            RefreshPlayerRenderers();
            Invalidate();
        }

        private void RegisterThemeSprites()
        {
            foreach (var kvp in tileSpriteSet.Sprites)
            {
                SpriteRegistry.Register(kvp.Key, kvp.Value);
            }

            foreach (var kvp in playerSpriteSet.Sprites)
            {
                SpriteRegistry.Register(kvp.Key, kvp.Value);
            }

            foreach (var kvp in enemySpriteSet.Sprites)
            {
                SpriteRegistry.Register(kvp.Key, kvp.Value);
            }
        }

        private void RefreshPlayerRenderers()
        {
            lock (playerRenderers)
            {
                foreach (var renderer in playerRenderers.Values)
                {
                    var sprite = SpriteRegistry.GetSprite(renderer.Role);
                    renderer.UpdateTheme(sprite, uiPalette.PlayerLabelColor, uiPalette.LocalPlayerRingColor);
                }
            }
        }

        private void ToggleTheme()
        {
            var nextMode = currentTheme == ThemeMode.Summer ? ThemeMode.Winter : ThemeMode.Summer;
            ApplyTheme(nextMode, refreshSprites: true);
        }

        private void UpdateWindowTitle()
        {
            Text = $"Game Client - {currentTheme} Mode";
        }

        private void GameClientForm_Load(object? sender, EventArgs e)
        {
            RegisterThemeSprites();

        private void HandleRawMessage(string raw)
        {
            // Single central place to parse messages and route to managers
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var type = doc.RootElement.GetProperty("Type").GetString();

                switch (type)
                {
                    case "welcome":
                        var welcome = JsonSerializer.Deserialize<WelcomeMessage>(raw);
                        if (welcome != null) _myId = welcome.Id;
                        break;

                    case "tile_update":
                        var tileUpdate = JsonSerializer.Deserialize<TileUpdateMessage>(raw);
                        if (tileUpdate != null)
                            BeginInvoke((Action)(() => UpdateTile(tileUpdate.X, tileUpdate.Y, tileUpdate.TileType)));
                        break;

                    case "map_state":
                        var mapState = JsonSerializer.Deserialize<MapStateMessage>(raw);
                        if (mapState != null)
                        {
                            BeginInvoke((Action)(() =>
                            {
                                _map = new Map();
                                _tileManager.Clear();
                                _map.LoadFromDimensions(mapState.Width, mapState.Height);
                                foreach (var t in mapState.Tiles)
                                {
                                    // Build TileData instances using same switch logic as before
                                    UpdateTile(t.X, t.Y, t.TileType);
                                }
                                Invalidate();
                            }));
                        }
                        break;

                    case "state":
                        var state = JsonSerializer.Deserialize<StateMessage>(raw);
                        if (state != null)
                        {
                            BeginInvoke((Action)(() =>
                            {
                                _entityManager.UpdatePlayers(state.Players, _myId);
                                _entityManager.UpdateEnemies(state.Enemies);
                                Invalidate();
                            }));
                        }
                        break;

                    case "attack_animation":
                        var anim = JsonSerializer.Deserialize<AttackAnimationMessage>(raw);
                        if (anim != null)
                        {
                            // If arrow, we need start position on client (player renderer center)
                            if (anim.AttackType == "arrow" && anim.PlayerId != 0)
                            {
                                var pr = _entityManager.GetPlayerRenderer(anim.PlayerId);
                                if (pr != null)
                                {
                                    var (px, py) = pr.Position;
                                    _animManager.AddArrow(px, py, anim.AnimX, anim.AnimY, float.TryParse(anim.Direction, out var r) ? r : 0f);
                                }
                            }
                            else
                            {
                                _animManager.HandleAttackAnimation(anim);
                            }
                            BeginInvoke((Action)(() => Invalidate()));
                        }
                        break;

                    case "error":
                        var err = JsonSerializer.Deserialize<ErrorMessage>(raw);
                        if (err != null)
                            MessageBox.Show($"Server error: {err.Detail}");
                        break;

                    case "collision":
                        // handle if needed
                        break;

                    case "goodbye":
                        MessageBox.Show("Server closed connection.");
                        Application.Exit();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleRawMessage parse error: " + ex);
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

                                switch (animMsg.AttackType)
                                {
                                    case "slash":
                                        lock (activeSlashes)
                                            activeSlashes.Add(new SlashEffect(animX, animY, radius, rotation));
                                        break;
                                    case "fireball":
                                        lock (activeFireballs)
                                            activeFireballs.Add(new MageFireballEffect(animX, animY));
                                        break;
                                    case "arrow":
                                        // For arrow, use player's position as start if available
                                        if (playerRenderers.TryGetValue(animMsg.PlayerId, out var pr))
                                        {
                                            var (px, py) = pr.Position;
                                            lock (activeArrows)
                                                activeArrows.Add(new ArrowEffect(px, py, animX, animY, rotation));
                                        }
                                        break;
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
                        var renderer = new PlayerRenderer(
                            ps.Id,
                            ps.RoleType,
                            ps.X,
                            ps.Y,
                            sprite,
                            isLocal,
                            uiPalette.PlayerLabelColor,
                            uiPalette.LocalPlayerRingColor);
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
                        var sprite = SpriteRegistry.GetSprite(es.EnemyType) ?? defaultEnemySprite;
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

            _map.SetTile(x, y, newTile);
            _tileManager.SetTile(newTile);
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!_connection.IsConnected || _myId == 0) return;

            _inputHandler.UpdateMovement();

            // create walk command and enqueue
            var walkCommand = new WalkCommand( _connection, _myId, _inputHandler.MoveX, _inputHandler.MoveY);
          
            _commandInvoker.AddCommand(walkCommand);
            _commandInvoker.ExecuteCommands();

            Invalidate();
        }

        private void OnMouseClick(object? sender, MouseEventArgs e)
        {
            if (_myId == 0 || !_connection.IsConnected) return;
            var pr = _entityManager.GetPlayerRenderer(_myId);
            if (pr == null) return;

            // send raw click coords and attack type
            var attackCommand = new AttackCommand(_connection, _myId, e.X, e.Y, "slash"); // adapt signatures if needed
            _commandInvoker.AddCommand(attackCommand);
            // do not spawn local animation; server will broadcast
        }

        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            // draw tiles
            _tileManager.DrawAll(e.Graphics);

            // draw entities
            _entityManager.DrawAll(e.Graphics);

            // draw animations
            _animManager.DrawAll(e.Graphics);

            using var pen = new Pen(uiPalette.GridLineColor, 1);
            for (int x = 0; x <= map.Width; x++)
                e.Graphics.DrawLine(pen, x * TileSize, 0, x * TileSize, map.Height * TileSize);
            for (int y = 0; y <= map.Height; y++)
                e.Graphics.DrawLine(pen, 0, y * TileSize, map.Width * TileSize, y * TileSize);
        }

        private void GameClientForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                ToggleTheme();
                return;
            }

            pressedKeys.Add(e.KeyCode);
        }
        private void GameClientForm_KeyUp(object? sender, KeyEventArgs e) => pressedKeys.Remove(e.KeyCode);
    }
}
