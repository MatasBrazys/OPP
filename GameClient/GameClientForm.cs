// ./GameClient/GameClientForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using GameClient.Input;
using GameClient.Managers;
using GameClient.Networking;
using GameClient.Rendering;
using GameClient.Adapters;
using GameShared.Messages;
using GameShared.Types.Map;
using GameShared.Types.DTOs;
using GameShared;

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

        public GameClientForm()
        {
            InitializeComponentMinimal();

            _connection = new ServerConnection("127.0.0.1", 5000);
            _connection.OnRawMessageReceived += HandleRawMessage;
            _connection.OnConnectionError += ex => MessageBox.Show("Connection error: " + ex.Message);

            SpriteManager.RegisterDefaultSprites();

            _entityManager = new EntityManager(SpriteRegistry.GetSprite("Slime"));
            _tileManager = new TileManager(TileSize);
            _animManager = new AnimationManager();
            _inputHandler = new InputHandler();
            _commandInvoker = new CommandInvoker();

            _gameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();

            // input events
            KeyDown += _inputHandler.KeyDown;
            KeyUp += _inputHandler.KeyUp;
            MouseClick += OnMouseClick;
        }

        private void InitializeComponentMinimal()
        {
            // keep same DoubleBuffer and style from original
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Load += GameClientForm_Load;
            Paint += GameClientForm_Paint;
        }

        private void GameClientForm_Load(object? sender, EventArgs e)
        {
            // Connect after form load
            _connection.Connect();
        }

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

            // draw grid
            using var pen = new Pen(Color.Black, 1);
            for (int x = 0; x <= _map.Width; x++)
                e.Graphics.DrawLine(pen, x * TileSize, 0, x * TileSize, _map.Height * TileSize);
            for (int y = 0; y <= _map.Height; y++)
                e.Graphics.DrawLine(pen, 0, y * TileSize, _map.Width * TileSize, y * TileSize);
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
