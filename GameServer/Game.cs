// ./GameServer/Game.cs
using GameServer.Facades;
using GameServer.Factories;
using GameShared.Types.Map;
using GameShared.Types.Players;
using GameShared.Strategies;
using GameShared.Messages;
using System.Threading;


namespace GameServer
{
    // SINGLETON (private constructor, static Instance property for global access and Lazy<T> ensures that instance is created only once (and thread-safe))
    public class Game
    {
        private static readonly Lazy<Game> _instance = new(() => new Game());
        public static Game Instance => _instance.Value;

        public Server Server { get; private set; }
        public World World { get; private set; }
        public IPlayerFactory PlayerFactory { get; private set; }
        public IEnemyFactory EnemyFactory { get; private set; }
        public GameWorldFacade WorldFacade { get; private set; }

        private Thread? _tickThread;
        private bool _running;

        // Private constructor
        private Game()
        {
            Server = new Server();
            World = new World();
            PlayerFactory = new PlayerFactory();
            EnemyFactory = new EnemyFactory();
            WorldFacade = new GameWorldFacade(World, PlayerFactory, EnemyFactory);

            // Subscribe to plant growth events
            WorldFacade.OnPlantGrew += HandlePlantGrowth;
        }

        public void Start()
        {
            Console.WriteLine("Game.Start: calling InitializeWorld");
            InitializeWorld();

            // Start the world tick loop in a separate thread
            _running = true;
            _tickThread = new Thread(GameLoop) { IsBackground = true };
            _tickThread.Start();

            Console.WriteLine("Game.Start: calling Server.Start");
            Console.WriteLine($"Game.World instance: {World.GetHashCode()}");
            Server.Start(5000); // server blocks here for client connections
        }

        private void InitializeWorld()
        {
            // Create initial game objects or enemies
            var slime = EnemyFactory.CreateEnemy("slime", 9001, 400, 800);
            slime.RoamingAI = new LeftRightRoam(slime.X, 200, 2); // 200px roam, 2px per tick
            World.AddEntity(slime);


            var slime1 = EnemyFactory.CreateEnemy("slime", 9002, 850, 400);
            slime1.RoamingAI = new LeftRightRoam(slime1.X, 100, 2); // 200px roam, 2px per tick
            World.AddEntity(slime1);

            // ===== DEMO: Plant some wheat for testing =====
            Console.WriteLine("\nPlanting demo wheat seeds...");
            WorldFacade.PlantSeed(5, 5, "Wheat");
            WorldFacade.PlantSeed(6, 6, "Wheat");
            WorldFacade.PlantSeed(7, 5, "Wheat");
            Console.WriteLine($"Total plants: {WorldFacade.GetAllPlants().Count}\n");
            // ===============================================

            // Add more enemies or objects as needed
        }

        private void GameLoop()
        {
            const int tickRateMs = 50; // 20 ticks per second
            while (_running)
            {
                var start = DateTime.UtcNow;

                Tick(tickRateMs);

                var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
                Thread.Sleep(Math.Max(0, tickRateMs - (int)elapsed));
            }
        }

        public void Tick(int dt)
        {
            // Update plant growth
            var grownPlants = WorldFacade.UpdatePlantGrowth();

            WorldFacade.UpdateWorld();
            Server.BroadcastState();
        }

        /// <summary>
        /// Handle plant growth events and broadcast to clients
        /// </summary>
        private void HandlePlantGrowth(object? plant, string tileType)
        {
            if (plant is GameShared.Types.GameObjects.Plant p)
            {
                var message = new PlantUpdateMessage
                {
                    PlantId = p.Id,
                    X = p.X,
                    Y = p.Y,
                    Stage = p.CurrentStage,
                    TileType = tileType,
                    PlantType = p.PlantType
                };

                // Broadcast to all clients
                Server.BroadcastMessage(message);
            }
        }

        public void Stop()
        {
            _running = false;
            _tickThread?.Join();
        }
    }
}
