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
using GameClient.States;
using GameShared.Types.Tasks;
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
        private PlayerStateManager? _stateManager;

        // Random task generator for Working state
        private static readonly Random _taskRandom = new();
        private static readonly (string Name, float DurationMs)[] _availableTasks = new[]
        {
            ("Harvest 3 Wheat", 4000f),
            ("Harvest 3 Carrot", 4000f),
            ("Harvest 2 Potato", 3000f)
        };

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

        // Track all plants known to the client (demonstrates Composite pattern - typed collections)
        private readonly Dictionary<int, AutoHarvestTarget> _knownWheat = new();
        private readonly Dictionary<int, AutoHarvestTarget> _knownCarrots = new();
        private readonly Dictionary<int, AutoHarvestTarget> _knownPotatoes = new();

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

            // State Manager will be fully initialized after we have myId
            // For now, create placeholder (will be re-initialized when myId is set)

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
                        if (welcome != null)
                        {
                            _myId = welcome.Id;
                            InitializeStateManager();
                        }
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

                    case "plant_planted":
                        var planted = JsonSerializer.Deserialize<PlantPlantedMessage>(raw);
                        if (planted != null)
                        {
                            var target = new AutoHarvestTarget
                            {
                                X = planted.X,
                                Y = planted.Y,
                                PlantType = planted.PlantType
                            };
                            // Add to appropriate typed list
                            if (planted.PlantType.Equals("wheat", StringComparison.OrdinalIgnoreCase))
                                _knownWheat[planted.PlantId] = target;
                            else if (planted.PlantType.Equals("carrot", StringComparison.OrdinalIgnoreCase))
                                _knownCarrots[planted.PlantId] = target;
                            else if (planted.PlantType.Equals("potato", StringComparison.OrdinalIgnoreCase))
                                _knownPotatoes[planted.PlantId] = target;
                        }
                        break;

                    case "plant_harvested":
                        var harvested = JsonSerializer.Deserialize<PlantHarvestedMessage>(raw);
                        if (harvested != null)
                        {
                            _knownWheat.Remove(harvested.PlantId);
                            _knownCarrots.Remove(harvested.PlantId);
                            _knownPotatoes.Remove(harvested.PlantId);
                        }
                        break;

                    case "plant_collection":
                        var collection = JsonSerializer.Deserialize<PlantCollectionMessage>(raw);
                        if (collection != null)
                        {
                            BeginInvoke((Action)(() =>
                            {
                                var playerRenderer = _entityManager.GetPlayerRenderer(collection.PlayerId);
                                if (playerRenderer != null)
                                {
                                    playerRenderer.ShowCollectionText(collection.Amount, collection.PlantType);
                                    Invalidate();
                                }
                            }));
                        }
                        break;

                    case "map_state":
                        var mapState = JsonSerializer.Deserialize<MapStateMessage>(raw);
                        if (mapState != null)
                        {
                            BeginInvoke((Action)(() =>
                            {
                                _knownWheat.Clear();
                                _knownCarrots.Clear();
                                _knownPotatoes.Clear();
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
                "carrot" => new CarrotTile(x, y),
                "carrotplant" => new CarrotPlantTile(x, y),
                "potato" => new PotatoTile(x, y),
                "potatoplant" => new PotatoPlantTile(x, y),
                _ => new GrassTile(x, y)
            };

            _map.SetTile(x, y, newTile);
            _tileManager.SetTile(newTile);

            MarkMapCacheDirty();
        }

        /// <summary>
        /// Initializes the State Manager after player ID is received from server.
        /// </summary>
        private void InitializeStateManager()
        {
            if (_stateManager != null) return; // Already initialized

            // Create all states
            var sleepState = new SleepState(null!);  // Will set manager reference after construction
            var attackState = new AttackState(null!, _connection, _myId, _commandInvoker);
            var workingState = new WorkingState(null!);
            var activeState = new ActiveState(
                null!,
                _connection,
                _myId,
                _commandInvoker,
                () => HandlePlantingAction("Wheat"),  // Default plant action
                () => HandleHarvestAction()
            );

            // Create the state manager
            _stateManager = new PlayerStateManager(activeState, attackState, sleepState, workingState);

            // Now inject the state manager reference into each state via reflection
            // (This is a workaround for the circular dependency)
            SetStateManagerReference(activeState);
            SetStateManagerReference(attackState);
            SetStateManagerReference(sleepState);
            SetStateManagerReference(workingState);

            Console.WriteLine($"[STATE] State Manager initialized for player {_myId}");
        }

        /// <summary>
        /// Helper to set the state manager reference via reflection (due to circular dependency).
        /// </summary>
        private void SetStateManagerReference(PlayerStateBase state)
        {
            var field = typeof(PlayerStateBase).GetField("_stateManager", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(state, _stateManager);
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!_connection.IsConnected || _myId == 0) return;

            _inputManager.Update();

            // Update current state (handles timeouts like attack duration, work completion)
            _stateManager?.CurrentState?.Update();

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

                // Route movement through state system
                if (!_autoHarvestActive && (dx != 0 || dy != 0))
                {
                    int dxInt = (int)dx;
                    int dyInt = (int)dy;
                    
                    // Check if state allows movement (Active and Working states allow movement)
                    var currentStateName = _stateManager?.CurrentState?.StateName;
                    if (currentStateName == "Active" || currentStateName == "Working")
                    {
                        _stateManager?.CurrentState?.HandleMovement(dxInt, dyInt);
                        var walkCommand = new WalkCommand(_connection, _myId, dx, dy, currentX, currentY);
                        _commandInvoker.AddCommand(walkCommand);
                    }
                    else
                    {
                        // State blocks movement - just notify
                        _stateManager?.CurrentState?.HandleMovement(dxInt, dyInt);
                    }
                }

                // Route attack through state system
                bool attackPressed = _inputManager.IsAttackPressed();
                if (attackPressed)
                {
                    var pr = _entityManager.GetPlayerRenderer(_myId);
                    if (pr != null)
                    {
                        var aimPos = _inputManager.GetAimPosition();
                        int aimX = (int)aimPos.X;
                        int aimY = (int)aimPos.Y;
                        
                        // Check if state allows attack
                        if (_stateManager?.CurrentState?.StateName == "Active")
                        {
                            _stateManager.CurrentState.HandleAttack(aimX, aimY);
                            var attackCommand = new AttackCommand(_connection, _myId, aimPos.X, aimPos.Y, "slash");
                            _commandInvoker.AddCommand(attackCommand);
                        }
                        else
                        {
                            // State blocks attack - just notify
                            _stateManager?.CurrentState?.HandleAttack(aimX, aimY);
                        }
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

            // ===== STATE SYSTEM: SLEEP TOGGLE (Z key without modifiers) =====
            if (e.KeyCode == Keys.Z && !e.Control && !e.Shift)
            {
                _stateManager?.CurrentState?.HandleSleepToggle();
            }

            // ===== STATE SYSTEM: TASK ASSIGNMENT (T key) =====
            if (e.KeyCode == Keys.T && !e.Control)
            {
                if (_stateManager?.CurrentState?.StateName == "Active")
                {
                    // Create a PlantTask with requirement (harvest 3 plants)
                    var taskId = _taskRandom.Next(1000);
                    var requiredCount = _taskRandom.Next(2, 5); // 2-4 harvests required
                    var plantTypes = new[] { "Wheat", "Carrot", "Potato" };
                    var plantType = plantTypes[_taskRandom.Next(plantTypes.Length)];
                    
                    var task = new PlantTask(taskId, requiredCount, plantType);
                    
                    var workingState = _stateManager.GetWorkingState();
                    workingState.AssignTask(task);
                    _stateManager.CurrentState.HandleTaskAssignment();
                    
                    Console.WriteLine($"[TASK] Assigned: {task.Description}");
                }
                else
                {
                    Console.WriteLine($"[STATE] Cannot start work - current state: {_stateManager?.CurrentState?.StateName}");
                }
            }

            // ===== STATE SYSTEM: CANCEL WORK (Q key) =====
            if (e.KeyCode == Keys.Q && !e.Control)
            {
                if (_stateManager?.CurrentState?.StateName == "Working")
                {
                    Console.WriteLine($"[TASK] Cancelling current task...");
                    _stateManager.CurrentState.HandleTaskCompletion(); // This will transition back to Active
                }
            }

            // ===== PLANTING SYSTEM (routed through state) =====
            if (e.KeyCode == Keys.I)
            {
                var state = _stateManager?.CurrentState?.StateName;
                if (state == "Active" || state == "Working")
                    HandlePlantingAction("Wheat");
                else
                    _stateManager?.CurrentState?.HandlePlant();
            }
            if (e.KeyCode == Keys.K)
            {
                var state = _stateManager?.CurrentState?.StateName;
                if (state == "Active" || state == "Working")
                    HandlePlantingAction("Carrot");
                else
                    _stateManager?.CurrentState?.HandlePlant();
            }
            if (e.KeyCode == Keys.L)
            {
                var state = _stateManager?.CurrentState?.StateName;
                if (state == "Active" || state == "Working")
                    HandlePlantingAction("Potato");
                else
                    _stateManager?.CurrentState?.HandlePlant();
            }

            // ===== HARVESTING SYSTEM (routed through state) =====
            if (e.KeyCode == Keys.H)
            {
                var state = _stateManager?.CurrentState?.StateName;
                if (state == "Active" || state == "Working")
                {
                    HandleHarvestAction();
                    // If in Working state, update task progress
                    if (state == "Working")
                    {
                        _stateManager?.GetWorkingState().OnHarvestPerformed();
                    }
                }
                else
                {
                    _stateManager?.CurrentState?.HandleHarvest();
                }
            }

            // ===== HARVEST ALL BY TYPE (demonstrates Iterator pattern) =====
            if (e.KeyCode == Keys.D1)
            {
                HandleHarvestAllOfType("Wheat");
            }
            if (e.KeyCode == Keys.D2)
            {
                HandleHarvestAllOfType("Carrot");
            }
            if (e.KeyCode == Keys.D3)
            {
                HandleHarvestAllOfType("Potato");
            }

            // ===== HARVEST ALL PLANTS (demonstrates Composite + Iterator pattern) =====
            if (e.KeyCode == Keys.D4)
            {
                HandleHarvestAllPlants();
            }
        }

        /// <summary>
        /// Handle planting action when player presses I/K/L
        /// Gets player tile position and attempts to plant there
        /// </summary>
        private void HandlePlantingAction(string plantType)
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
                PlantType = plantType
            };

            var json = System.Text.Json.JsonSerializer.Serialize(plantMessage);
            _connection.SendRaw(json);
            Console.WriteLine($"[PLANT] Sent {plantType} plant request at ({tileX}, {tileY})");
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
            var harvestableTypes = new[] { "Wheat", "WheatPlant", "Carrot", "CarrotPlant", "Potato", "PotatoPlant" };
            if (!harvestableTypes.Contains(tile.TileType))
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
        /// Harvest all plants of a specified type (demonstrates Iterator pattern - iterating through typed list)
        /// Player walks to each plant location with animation
        /// </summary>
        private void HandleHarvestAllOfType(string plantType)
        {
            var knownList = plantType switch
            {
                "Wheat" => _knownWheat,
                "Carrot" => _knownCarrots,
                "Potato" => _knownPotatoes,
                _ => null
            };

            if (knownList == null || knownList.Count == 0)
            {
                Console.WriteLine($"[HARVEST ALL {plantType.ToUpper()}] No {plantType} plants to harvest");
                return;
            }

            // Cancel any existing auto-harvest
            if (_autoHarvestCts != null)
            {
                _autoHarvestCts.Cancel();
                _autoHarvestCts.Dispose();
            }

            _autoHarvestCts = new CancellationTokenSource();
            _autoHarvestActive = true;

            var targets = knownList.Values.ToList();
            Console.WriteLine($"[HARVEST ALL {plantType.ToUpper()}] Starting animated harvest of {targets.Count} {plantType} plants (Iterator pattern demonstration)");
            StartAutoHarvestRoutine(targets);
        }

        /// <summary>
        /// Harvest ALL plants from ALL typed lists (demonstrates COMPOSITE + ITERATOR pattern)
        /// This shows how we can treat a collection of collections as a single unit
        /// Player walks to each plant location with animation
        /// </summary>
        private void HandleHarvestAllPlants()
        {
            // Composite: Group all typed lists together
            var allLists = new[]
            {
                ("Wheat", _knownWheat),
                ("Carrot", _knownCarrots),
                ("Potato", _knownPotatoes)
            };

            int totalCount = allLists.Sum(l => l.Item2.Count);
            if (totalCount == 0)
            {
                Console.WriteLine("[HARVEST ALL PLANTS] No plants to harvest");
                return;
            }

            Console.WriteLine($"[HARVEST ALL PLANTS] 🌾 Starting Composite + Iterator pattern demonstration");
            Console.WriteLine($"[HARVEST ALL PLANTS] Total plants across all types: {totalCount}");
            Console.WriteLine($"[HARVEST ALL PLANTS] Iterating through composite of {allLists.Length} typed lists...\n");

            // Cancel any existing auto-harvest
            if (_autoHarvestCts != null)
            {
                _autoHarvestCts.Cancel();
                _autoHarvestCts.Dispose();
            }

            _autoHarvestCts = new CancellationTokenSource();
            _autoHarvestActive = true;

            // Flatten all plants from all typed lists into single collection for animation
            var allTargets = new List<AutoHarvestTarget>();
            foreach (var (typeName, list) in allLists)
            {
                if (list.Count == 0) continue;
                Console.WriteLine($"  📋 Added {list.Count} {typeName} plants to harvest queue");
                allTargets.AddRange(list.Values);
            }

            Console.WriteLine($"\n[HARVEST ALL PLANTS] Walking to and harvesting all {allTargets.Count} plants...");
            StartAutoHarvestRoutine(allTargets);
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
                _autoHarvestActive = false;
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

                        // Move until within threshold of tile center OR player's tile matches target tile
                        int noProgressCount = 0;
                        int prevCx = int.MinValue, prevCy = int.MinValue;
                        int prevDistSq = int.MaxValue;
                        while (!token.IsCancellationRequested)
                        {
                            var (cx, cy) = GetPlayerPos();
                            int curTileX = cx / TileSize;
                            int curTileY = cy / TileSize;
                            if (curTileX == t.X && curTileY == t.Y)
                            {
                                break; // arrived at target tile
                            }
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

                            // Detect lack of progress (blocked path) to avoid infinite drift
                            if (cx == prevCx && cy == prevCy)
                            {
                                noProgressCount++;
                            }
                            else
                            {
                                prevCx = cx;
                                prevCy = cy;
                            }

                            if (distSq >= prevDistSq - 4) // allow small jitter tolerance
                            {
                                noProgressCount++;
                            }
                            else
                            {
                                prevDistSq = distSq;
                            }

                            if (noProgressCount >= 20)
                            {
                                Console.WriteLine("[AUTO] Movement appears blocked; skipping this target");
                                break;
                            }

                            await Task.Delay(80, token);
                        }

                        if (token.IsCancellationRequested) break;

                        // Brief stop before harvesting to end any residual movement
                        var stopBefore = new InputMessage { Dx = 0, Dy = 0 };
                        var stopBeforeJson = System.Text.Json.JsonSerializer.Serialize(stopBefore);
                        _connection.SendRaw(stopBeforeJson);

                        var harvest = new HarvestActionMessage { PlayerId = _myId, TileX = t.X, TileY = t.Y };
                        var harvestJson = System.Text.Json.JsonSerializer.Serialize(harvest);
                        _connection.SendRaw(harvestJson);
                        Console.WriteLine($"[AUTO] Sent harvest for ({t.X}, {t.Y})");

                        await Task.Delay(250, token);

                        // Ensure we stop before moving to the next target
                        var stopBetween = new InputMessage { Dx = 0, Dy = 0 };
                        var stopBetweenJson = System.Text.Json.JsonSerializer.Serialize(stopBetween);
                        _connection.SendRaw(stopBetweenJson);

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
                    // Prefer keyboard after auto to avoid controller drift
                    try { _inputManager.ForceKeyboardAdapter(); } catch { }
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