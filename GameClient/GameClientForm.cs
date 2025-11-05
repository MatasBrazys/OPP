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

        // Theme fields
        private readonly IGameThemeFactory _summerFactory;
        private readonly IGameThemeFactory _winterFactory;
        private ThemeMode _currentTheme;
        private ITileSpriteSet _tileSpriteSet;
        private IPlayerSpriteSet _playerSpriteSet;
        private IEnemySpriteSet _enemySpriteSet;
        private IUiPalette _uiPalette;
        private Image _defaultEnemySprite;

        private IRenderer _standardRenderer;
        private IRenderer _antiAliasedRenderer;
        private IRenderer _debugRenderer;
        private int _currentRendererMode = 0; // 0=standard, 1=antialiased, 2=debug
        private readonly CursorRenderer _cursorRenderer;

        public GameClientForm()
        {
            InitializeComponentMinimal();

            _connection = new ServerConnection("127.0.0.1", 5000);
            _connection.OnRawMessageReceived += HandleRawMessage;
            _connection.OnConnectionError += ex => MessageBox.Show("Connection error: " + ex.Message);

            SpriteManager.RegisterDefaultSprites();

            _summerFactory = new SummerGameThemeFactory();
            _winterFactory = new WinterGameThemeFactory();

            // BRIDGE: Initialize renderers
            _standardRenderer = new StandardRenderer();
            _antiAliasedRenderer = new AntiAliasedRenderer();
            _debugRenderer = new DebugRenderer();

            // BRIDGE: Pass renderer to EntityManager
            _entityManager = new EntityManager(_defaultEnemySprite, _standardRenderer);

            // apply default theme
            ApplyTheme(ThemeMode.Winter, refreshSprites: true);

            _tileManager = new TileManager(TileSize);
            _animManager = new AnimationManager();

            //input
            _inputManager = new InputManager(this, ClientSize);
            _commandInvoker = new CommandInvoker();

            _gameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();

            KeyDown += GameClientForm_KeyDown;

            
            // this.MouseDown += (s, e) => Console.WriteLine($"[DEBUG] Form MouseDown: {e.Button} at ({e.X}, {e.Y})");

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
                                RefreshPlayerRenderers();
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
                _ => new GrassTile(x, y)
            };

            _map.SetTile(x, y, newTile);
            _tileManager.SetTile(newTile);
        }

        private void GameLoop(object? sender, EventArgs e)
        {
            if (!_connection.IsConnected || _myId == 0) return;

            _inputManager.Update();

            bool isGamepad = _inputManager.ActiveAdapter?.InputMethodName.Contains("Controller") ?? false;
            _cursorRenderer.Visible = isGamepad;
            if (isGamepad)
            {
                _cursorRenderer.Position = _inputManager.GetAimPosition();
            }
            var (dx, dy) = _inputManager.GetMovementInput();
            //Console.WriteLine($"[INPUT] Movement Input: dx={dx}, dy={dy}");
            var walkCommand = new WalkCommand(_connection, _myId, dx, dy);
            _commandInvoker.AddCommand(walkCommand);
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
            Invalidate();
        }



        private void GameClientForm_Paint(object? sender, PaintEventArgs e)
        {
            _tileManager.DrawAll(e.Graphics);
            _entityManager.DrawAll(e.Graphics);
            _animManager.DrawAll(e.Graphics);

            using var pen = new Pen(Color.Black, 1);
            for (int x = 0; x <= _map.Width; x++)
                e.Graphics.DrawLine(pen, x * TileSize, 0, x * TileSize, _map.Height * TileSize);
            for (int y = 0; y <= _map.Height; y++)
                e.Graphics.DrawLine(pen, 0, y * TileSize, _map.Width * TileSize, y * TileSize);
            
            _cursorRenderer.Draw(e.Graphics);
        }

        private void ApplyTheme(ThemeMode mode, bool refreshSprites)
        {
            Console.WriteLine("Applying theme: " + mode);
            _currentTheme = mode;
            var factory = mode == ThemeMode.Summer ? _summerFactory : _winterFactory;

            _tileSpriteSet = factory.CreateTileSpriteSet();
            _playerSpriteSet = factory.CreatePlayerSpriteSet();
            _enemySpriteSet = factory.CreateEnemySpriteSet();
            _uiPalette = factory.CreateUiPalette();

            _defaultEnemySprite = _enemySpriteSet.Sprites.TryGetValue("Slime", out var slime)
                ? slime
                : ThemeSpriteLoader.LoadEnemySprite("../assets/slime.png", Color.DarkSeaGreen);

            if (_entityManager != null)
                _entityManager.SetDefaultEnemySprite(_defaultEnemySprite);

            if (!refreshSprites) return;

            SpriteRegistry.Clear();
            RegisterThemeSprites();
            RefreshPlayerRenderers();
            Invalidate();
        }

        private void RegisterThemeSprites()
        {
            foreach (var kvp in _tileSpriteSet.Sprites) SpriteRegistry.Register(kvp.Key, kvp.Value);
            foreach (var kvp in _playerSpriteSet.Sprites) SpriteRegistry.Register(kvp.Key, kvp.Value);
            foreach (var kvp in _enemySpriteSet.Sprites) SpriteRegistry.Register(kvp.Key, kvp.Value);
        }

        private void RefreshPlayerRenderers()
        {
            foreach (var pr in _entityManager.GetAllPlayerRenderers())
            {
                var sprite = SpriteRegistry.GetSprite(pr.Role);
                pr.UpdateTheme(sprite, _uiPalette.PlayerLabelColor, _uiPalette.LocalPlayerRingColor);
            }
        }

        private void GameClientForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                var nextMode = _currentTheme == ThemeMode.Summer ? ThemeMode.Winter : ThemeMode.Summer;
                ApplyTheme(nextMode, refreshSprites: true);
            }

            // BRIDGE: F7 to cycle through renderers
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
            // F6 to cycle input adapters
            if (e.KeyCode == Keys.F6)
            {
                _inputManager.CycleInputAdapter();
                Console.WriteLine($"Input Method: {_inputManager.CurrentInputMethod}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _connection.Dispose();
                _gameTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}