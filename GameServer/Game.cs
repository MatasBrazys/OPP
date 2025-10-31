﻿// ./GameServer/Game.cs
using GameShared.Facades;
using GameShared.Factories;
using GameShared.Types.Map;
using GameShared.Types.Players;
using GameShared.Strategies;
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
        public GameObjectFactory GameObjectFactory { get; private set; }
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
            GameObjectFactory = new GameObjectFactory();
            WorldFacade = new GameWorldFacade(World, PlayerFactory, GameObjectFactory, EnemyFactory);
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
            slime.RoamingAI = new Strategies.LeftRightRoam(slime.X, 200, 2); // 200px roam, 2px per tick
            World.AddEntity(slime);


            var slime1 = EnemyFactory.CreateEnemy("slime", 9002, 850, 400);
            slime1.RoamingAI = new Strategies.LeftRightRoam(slime1.X, 100, 2); // 200px roam, 2px per tick
            World.AddEntity(slime1);

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
            WorldFacade.UpdateWorld();
        }

        public void Stop()
        {
            _running = false;
            _tickThread?.Join();
        }
    }
}
