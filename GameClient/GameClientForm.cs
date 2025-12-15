using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;
using GameClient.Input.Adapters;
using GameClient.Managers;
using GameClient.Networking;
using GameClient.Rendering;
using GameShared.Messages;
using GameShared.Types.Map;
using GameShared.Types.DTOs;
using GameShared;
using GameClient.Theming;
using GameClient.Rendering.Bridge;
using GameClient.Rendering.Flyweight;
using GameClient.Animation;

namespace GameClient
{
    public partial class GameClientForm : BaseForm
    {
        // Core collaborators
        private readonly ServerConnection _connection;
        private readonly EntityManager _entityManager;
        private readonly TileManager _tileManager;
        private readonly AnimationManager _animManager;
        private readonly InputManager _inputManager;
        private readonly CommandInvoker _commandInvoker;
       
        private int _myId;
        private readonly System.Windows.Forms.Timer _gameTimer;
        private Map _map = new();

        private const int TileSize = GameConstants.TILE_SIZE;

        // Theme fields - NO MORE _playerSpriteSet!
        private readonly IGameThemeFactory _summerFactory;
        private readonly IGameThemeFactory _winterFactory;
        private ThemeMode _currentTheme;
        private ITileSpriteSet _tileSpriteSet;
        private IEnemySpriteSet _enemySpriteSet;
        private IUiPalette _uiPalette;
        private Image _defaultEnemySprite;

        private IRenderer _standardRenderer;
        private IRenderer _antiAliasedRenderer;
        private IRenderer _debugRenderer;
        private int _currentRendererMode = 0;
        private readonly CursorRenderer _cursorRenderer;
        private DateTime _lastInputSent = DateTime.MinValue;
        private const int InputRateMs = 50;

        private Bitmap? _mapCache;
        private bool _mapCacheDirty = true;

        public GameClientForm()
        {
            InitializeComponentMinimal();

            _connection = new ServerConnection("127.0.0.1", 5000);
            _connection.OnRawMessageReceived += HandleRawMessage;
            _connection.OnConnectionError += ex => MessageBox.Show("Connection error: " + ex.Message);

            SpriteManager.RegisterDefaultSprites();

            _summerFactory = new SummerGameThemeFactory();
            _winterFactory = new WinterGameThemeFactory();

            _standardRenderer = new StandardRenderer();
            _antiAliasedRenderer = new AntiAliasedRenderer();
            _debugRenderer = new DebugRenderer();

            _entityManager = new EntityManager(_defaultEnemySprite, _standardRenderer);

            // Apply default theme
            ApplyTheme(ThemeMode.Winter, refreshSprites: true);

            _tileManager = new TileManager(TileSize);
            _animManager = new AnimationManager();

            _inputManager = new InputManager(this, ClientSize);
            _commandInvoker = new CommandInvoker();

            _gameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();

            KeyDown += GameClientForm_KeyDown;
            WalkSpriteLoader.LoadAllRoleAnimations("../assets/animations/");

            _cursorRenderer = new CursorRenderer();
        }

        private void InitializeComponentMinimal()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Load += GameClientForm_Load;
            Paint += GameClientForm_Paint;
        }

        private void GameClientForm_Load(object? sender, EventArgs e)
        {
            PerformanceBenchmark.RunBenchmark(entityCount: 100);
            _connection.Connect();
        }

        private void HandleRawMessage(string raw)
        {
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

                    case "plant_update":
                        var plantUpdate = JsonSerializer.Deserialize<PlantUpdateMessage>(raw);
                        if (plantUpdate != null)
                            BeginInvoke((Action)(() => UpdateTile(plantUpdate.X, plantUpdate.Y, plantUpdate.TileType)));
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
                "wheat" => new WheatTile(x, y),
                "wheatplant" => new WheatPlantTile(x, y),
                _ => new GrassTile(x, y)
            };

            _map.SetTile(x, y, newTile);
            _tileManager.SetTile(newTile);

            MarkMapCacheDirty();
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!_connection.IsConnected || _myId == 0) return;

            _inputManager.Update();

            if ((DateTime.UtcNow - _lastInputSent).TotalMilliseconds >= InputRateMs)
            {
                bool isGamepad = _inputManager.ActiveAdapter?.InputMethodName.Contains("Controller") ?? false;
                _cursorRenderer.Visible = isGamepad;
                if (isGamepad)
                {
                    _cursorRenderer.Position = _inputManager.GetAimPosition();
                }

                var (dx, dy) = _inputManager.GetMovementInput();
                var player = _entityManager.GetPlayerRenderer(_myId);
                int currentX = 0, currentY = 0;
                if (player != null)
                {
                    var pos = player.Position;
                    currentX = (int)pos.X;
                    currentY = (int)pos.Y;
                }
                if (dx != 0 || dy != 0)
                {
                    var walkCommand = new WalkCommand(_connection, _myId, dx, dy, currentX, currentY);
                    _commandInvoker.AddCommand(walkCommand);
                }

                bool attackPressed = _inputManager.IsAttackPressed();
                if (attackPressed)
                {
                    var pr = _entityManager.GetPlayerRenderer(_myId);
                    if (pr != null)
                    {
                        var aimPos = _inputManager.GetAimPosition();
                        var attackCommand = new AttackCommand(_connection, _myId, aimPos.X, aimPos.Y, "slash");
                        _commandInvoker.AddCommand(attackCommand);
                    }
                }

                _commandInvoker.ExecuteCommands();
                _lastInputSent = DateTime.UtcNow;
            }

            Invalidate();
        }

        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            EnsureMapCache();
            if (_mapCache != null)
                e.Graphics.DrawImageUnscaled(_mapCache, 0, 0);

            _entityManager.DrawAll(e.Graphics);
            _animManager.DrawAll(e.Graphics);
            _cursorRenderer.Draw(e.Graphics);
        }

        // ✅ FIXED: No more player sprite theming
        private void ApplyTheme(ThemeMode mode, bool refreshSprites)
        {
            Console.WriteLine("Applying theme: " + mode);
            _currentTheme = mode;
            var factory = mode == ThemeMode.Summer ? _summerFactory : _winterFactory;

            _tileSpriteSet = factory.CreateTileSpriteSet();
            _enemySpriteSet = factory.CreateEnemySpriteSet();
            _uiPalette = factory.CreateUiPalette();

            _defaultEnemySprite = _enemySpriteSet.Sprites.TryGetValue("Slime", out var slime)
                ? slime
                : ThemeSpriteLoader.LoadEnemySprite("../assets/slime.png", Color.DarkSeaGreen);

            if (_entityManager != null)
                _entityManager.SetDefaultEnemySprite(_defaultEnemySprite);

            if (!refreshSprites) return;

            // Clear and re-register only tile and enemy sprites
            SpriteRegistry.Clear();
            RegisterThemeSprites();
            
            // Re-register player animation sprites (theme-independent)
            WalkSpriteLoader.LoadAllRoleAnimations("../assets/animations/");
            
            // Update existing enemy sprites
            RefreshEnemySprites();
            
            MarkMapCacheDirty();
            Invalidate();
        }

        // ✅ FIXED: Only register tiles and enemies
        private void RegisterThemeSprites()
        {
            foreach (var kvp in _tileSpriteSet.Sprites) 
                SpriteRegistry.Register(kvp.Key, kvp.Value);
            foreach (var kvp in _enemySpriteSet.Sprites) 
                SpriteRegistry.Register(kvp.Key, kvp.Value);
        }

        // ✅ NEW: Update enemy sprites when theme changes
        private void RefreshEnemySprites()
        {
            foreach (var er in _entityManager.GetAllEnemyRenderers())
            {
                var sprite = SpriteRegistry.GetSprite(er.EnemyType) ?? _defaultEnemySprite;
                er.UpdateSprite(sprite);
            }
        }

        private void GameClientForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                var nextMode = _currentTheme == ThemeMode.Summer ? ThemeMode.Winter : ThemeMode.Summer;
                ApplyTheme(nextMode, refreshSprites: true);
            }

            if (e.KeyCode == Keys.F7)
            {
                _currentRendererMode = (_currentRendererMode + 1) % 3;

                IRenderer newRenderer = _currentRendererMode switch
                {
                    0 => _standardRenderer,
                    1 => _antiAliasedRenderer,
                    2 => _debugRenderer,
                    _ => _standardRenderer
                };

                _entityManager.SetRenderer(newRenderer);

                string modeName = _currentRendererMode switch
                {
                    0 => "Standard",
                    1 => "Anti-Aliased",
                    2 => "Debug",
                    _ => "Unknown"
                };

                Console.WriteLine($"[BRIDGE] Switched to {modeName} renderer");
                Invalidate();
            }

            if (e.KeyCode == Keys.F6)
            {
                _inputManager.CycleInputAdapter();
                Console.WriteLine($"Input Method: {_inputManager.CurrentInputMethod}");
            }

            if (e.KeyCode == Keys.F8)
            {
                _commandInvoker.UndoEnabled = !_commandInvoker.UndoEnabled;
                Console.WriteLine($"[UNDO] Undo system: {(_commandInvoker.UndoEnabled ? "ENABLED" : "DISABLED")}");

                if (!_commandInvoker.UndoEnabled)
                {
                    _commandInvoker.ClearHistory();
                }
            }

            if (e.Control && e.KeyCode == Keys.Z)
            {
                _lastInputSent = DateTime.UtcNow.AddMilliseconds(100);
                _commandInvoker.UndoLastCommand();
            }

            if (e.Control && e.Shift && e.KeyCode == Keys.Z)
            {
                _commandInvoker.ClearHistory();
            }

            if (e.KeyCode == Keys.F9)
            {
                SpriteCache.Instance.PrintReport();
            }

            // ===== PLANTING SYSTEM =====
            if (e.KeyCode == Keys.I)
            {
                HandlePlantingAction();
            }

            // ===== HARVESTING SYSTEM =====
            if (e.KeyCode == Keys.H)
            {
                HandleHarvestAction();
            }
        }

        /// <summary>
        /// Handle planting action when player presses "I"
        /// Gets player tile position and attempts to plant there
        /// </summary>
        private void HandlePlantingAction()
        {
            var player = _entityManager.GetPlayerRenderer(_myId);
            if (player == null)
            {
                Console.WriteLine("[PLANT] No player found");
                return;
            }

            // Get player position in pixels
            var (px, py) = player.Position;
            
            // Convert to tile coordinates
            int tileX = (int)(px / TileSize);
            int tileY = (int)(py / TileSize);

            Console.WriteLine($"[PLANT] DEBUG: Player at pixel ({px}, {py}) -> tile ({tileX}, {tileY})");

            // Validate tile coordinates
            if (tileX < 0 || tileY < 0 || tileX >= _map.Width || tileY >= _map.Height)
            {
                Console.WriteLine($"[PLANT] Invalid tile position: ({tileX}, {tileY})");
                return;
            }

            // Get the tile
            var tile = _map.GetTile(tileX, tileY);
            if (tile == null)
            {
                Console.WriteLine($"[PLANT] No tile at ({tileX}, {tileY})");
                return;
            }

            // Check if tile is plantable
            if (!tile.Plantable)
            {
                Console.WriteLine($"[PLANT] ❌ Tile {tile.TileType} at ({tileX}, {tileY}) is not plantable");
                return;
            }

            // Send plant action message to server
            var plantMessage = new PlantActionMessage
            {
                PlayerId = _myId,
                TileX = tileX,
                TileY = tileY,
                PlantType = "Wheat"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(plantMessage);
            _connection.SendRaw(json);
            Console.WriteLine($"[PLANT] Sent plant request at ({tileX}, {tileY})");
        }

        /// <summary>
        /// Handle harvesting action when player presses "H"
        /// Gets player tile position and attempts to harvest there
        /// </summary>
        private void HandleHarvestAction()
        {
            var player = _entityManager.GetPlayerRenderer(_myId);
            if (player == null)
            {
                Console.WriteLine("[HARVEST] No player found");
                return;
            }

            // Get player position in pixels
            var (px, py) = player.Position;
            
            // Convert to tile coordinates
            int tileX = (int)(px / TileSize);
            int tileY = (int)(py / TileSize);

            Console.WriteLine($"[HARVEST] DEBUG: Player at pixel ({px}, {py}) -> tile ({tileX}, {tileY})");

            // Validate tile coordinates
            if (tileX < 0 || tileY < 0 || tileX >= _map.Width || tileY >= _map.Height)
            {
                Console.WriteLine($"[HARVEST] Invalid tile position: ({tileX}, {tileY})");
                return;
            }

            // Get the tile
            var tile = _map.GetTile(tileX, tileY);
            if (tile == null)
            {
                Console.WriteLine($"[HARVEST] No tile at ({tileX}, {tileY})");
                return;
            }

            // Check if there's a harvestable plant at this location
            if (tile.TileType != "Wheat" && tile.TileType != "WheatPlant")
            {
                Console.WriteLine($"[HARVEST] No harvestable plant at ({tileX}, {tileY}), found: {tile.TileType}");
                return;
            }

            // Send harvest action message to server
            var harvestMessage = new HarvestActionMessage
            {
                PlayerId = _myId,
                TileX = tileX,
                TileY = tileY
            };

            var json = System.Text.Json.JsonSerializer.Serialize(harvestMessage);
            _connection.SendRaw(json);
            Console.WriteLine($"[HARVEST] Sent harvest request at ({tileX}, {tileY})");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.Dispose();
                _gameTimer.Dispose();
                _mapCache?.Dispose();
                _mapCache = null;
            }
            base.Dispose(disposing);
        }

        private void MarkMapCacheDirty() => _mapCacheDirty = true;

        private void EnsureMapCache()
        {
            if (!_mapCacheDirty) return;
            if (_map.Width == 0 || _map.Height == 0) return;

            _mapCache?.Dispose();

            _mapCache = new Bitmap(_map.Width * TileSize, _map.Height * TileSize);
            using var g = Graphics.FromImage(_mapCache);

            _tileManager.DrawAll(g);
            DrawGrid(g);

            _mapCacheDirty = false;
        }

        private void DrawGrid(Graphics g)
        {
            var gridColor = _uiPalette?.GridLineColor ?? Color.Black;
            using var pen = new Pen(gridColor, 1);

            for (int x = 0; x <= _map.Width; x++)
                g.DrawLine(pen, x * TileSize, 0, x * TileSize, _map.Height * TileSize);
            for (int y = 0; y <= _map.Height; y++)
                g.DrawLine(pen, 0, y * TileSize, _map.Width * TileSize, y * TileSize);
        }
    }
}