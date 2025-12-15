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
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

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

        // Auto-harvest cancellation token (client-driven walking + harvest)
        private CancellationTokenSource? _autoHarvestCts;
        private bool _autoHarvestActive = false;

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

                    case "auto_harvest_plan":
                        var plan = JsonSerializer.Deserialize<AutoHarvestPlanMessage>(raw);
                        if (plan != null)
                        {
                            StartAutoHarvestRoutine(plan.Targets);
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

                    case "task_completed":
                        var taskCompleted = JsonSerializer.Deserialize<GameShared.Messages.TaskCompletedMessage>(raw);
                        if (taskCompleted != null)
                        {
                            BeginInvoke((Action)(() =>
                            {
                                Console.WriteLine($"  !!!!!!!!!!!!!!!!! TASK COMPLETED: {taskCompleted.Description.PadRight(30)} !!!!!!!!!!!!!!!!!!!!!!!!!!!\n");

                            }));
                        }
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

            // ===== AUTO-HARVEST (T key) =====
            if (e.KeyCode == Keys.T)
            {
                // Toggle auto-harvest routine which moves player and harvests wheat tiles sequentially
                ToggleAutoHarvest();
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

        /// <summary>
        /// Toggle and start auto-harvest routine: move to each Wheat tile and harvest it.
        /// Pressing W will start the routine; pressing W again cancels it.
        /// </summary>
        private void ToggleAutoHarvest()
        {
            if (_autoHarvestCts != null)
            {
                Console.WriteLine("[AUTO] Cancelling auto-harvest...");
                _autoHarvestCts.Cancel();
                _autoHarvestCts.Dispose();
                _autoHarvestCts = null;
                _autoHarvestActive = false;
                return;
            }

            _autoHarvestCts = new CancellationTokenSource();
            _autoHarvestActive = true;
            var request = new GameShared.Messages.AutoHarvestMessage { PlayerId = _myId };
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            _connection.SendRaw(json);
            Console.WriteLine("[AUTO] Requested auto-harvest plan from server");
        }

        private void StartAutoHarvestRoutine(List<AutoHarvestTarget> targets)
        {
            if (targets == null || targets.Count == 0)
            {
                Console.WriteLine("[AUTO] No mature plants to harvest");
                _autoHarvestCts?.Dispose();
                _autoHarvestCts = null;
                return;
            }

            // If user cancelled before plan arrived, skip
            if (_autoHarvestCts == null)
            {
                Console.WriteLine("[AUTO] Plan ignored (no active request)");
                return;
            }

            var token = _autoHarvestCts.Token;

                Task.Run(async () =>
                {
                    int lastX = -1, lastY = -1;
                    try
                    {
                    // Helper to get player pixel position on UI thread
                    (int px, int py) GetPlayerPos()
                    {
                        (int, int) pos = (0, 0);
                        this.Invoke((Action)(() =>
                        {
                            var pr = _entityManager.GetPlayerRenderer(_myId);
                            if (pr != null) pos = ((int)pr.Position.X, (int)pr.Position.Y);
                        }));
                        return pos;
                    }

                    // Start order: nearest from current position
                    var ordered = targets.ToList();
                    var startPos = GetPlayerPos();
                    int startTileX = startPos.px / TileSize;
                    int startTileY = startPos.py / TileSize;
                    ordered.Sort((a, b) => Math.Abs(a.X - startTileX) + Math.Abs(a.Y - startTileY)
                                         - (Math.Abs(b.X - startTileX) + Math.Abs(b.Y - startTileY)));

                    foreach (var t in ordered)
                    {
                        if (token.IsCancellationRequested) break;

                        Console.WriteLine($"[AUTO] Targeting {t.PlantType} at ({t.X}, {t.Y})");

                        int targetPx = t.X * TileSize + TileSize / 2;
                        int targetPy = t.Y * TileSize + TileSize / 2;

                        // Move until within threshold of tile center
                        while (!token.IsCancellationRequested)
                        {
                            var (cx, cy) = GetPlayerPos();
                            int dx = targetPx - cx;
                            int dy = targetPy - cy;
                            int distSq = dx * dx + dy * dy;
                            if (distSq <= (TileSize / 2) * (TileSize / 2))
                            {
                                break; // arrived
                            }

                            int stepX = Math.Sign(dx);
                            int stepY = Math.Sign(dy);

                            var input = new InputMessage { Dx = stepX, Dy = stepY };
                            var inputJson = System.Text.Json.JsonSerializer.Serialize(input);
                            _connection.SendRaw(inputJson);

                            await Task.Delay(80, token);
                        }

                        if (token.IsCancellationRequested) break;

                        var harvest = new HarvestActionMessage { PlayerId = _myId, TileX = t.X, TileY = t.Y };
                        var harvestJson = System.Text.Json.JsonSerializer.Serialize(harvest);
                        _connection.SendRaw(harvestJson);
                        Console.WriteLine($"[AUTO] Sent harvest for ({t.X}, {t.Y})");

                        await Task.Delay(250, token);

                        lastX = t.X;
                        lastY = t.Y;
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Console.WriteLine("[AUTO] Error: " + ex);
                }
                finally
                {
                    // Stop residual movement by sending a zero input once
                    var stop = new InputMessage { Dx = 0, Dy = 0 };
                    var stopJson = System.Text.Json.JsonSerializer.Serialize(stop);
                    _connection.SendRaw(stopJson);
                    Console.WriteLine("[AUTO] Sent zero-input to stop movement");
                    _autoHarvestCts?.Dispose();
                    _autoHarvestCts = null;
                    _autoHarvestActive = false;
                    // Force a repaint so sprites/tiles settle after the sequence
                    BeginInvoke((Action)(Invalidate));
                    Console.WriteLine("[AUTO] Auto-harvest routine finished");
                }
            }, token);
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