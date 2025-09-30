using System;
using GameShared.Types;
using GameShared.Messages;
using GameShared.Factories;
using GameShared.Types.Players;

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
        public GameObjectFactory GameObjectFactory { get; private set; }

        // Private constructor
        private Game()
        {
            Server = new Server();
            World = new World();
            PlayerFactory = new PlayerFactory();
            GameObjectFactory = new GameObjectFactory();
        }

        public void Start()
        {
            Console.WriteLine("Game.Start: calling InitializeWorld");
            InitializeWorld();
            Console.WriteLine("Game.Start: calling Server.Start");
            Console.WriteLine($"Game.World instance: {World.GetHashCode()}");
            Server.Start(5000);
        }

        private void InitializeWorld()
        {
            // Create initial game objects
            World.AddEntity(GameObjectFactory.CreateObject("house", 200, 200));
            World.AddEntity(GameObjectFactory.CreateObject("tree", 300, 300));
        }

        public PlayerRole CreatePlayer(string roleType, int id)
        {
            var xCoord = roleType.ToLower() switch
            {
                "hunter" => 50,
                "mage" => 150,
                "defender" => 250,
                _ => throw new ArgumentException("Invalid player role type")
            };
            var player = PlayerFactory.CreatePlayer(roleType, id, xCoord, 100);
            World.AddEntity(player);
            return player;
        }

        public void Tick(int dt)
        {
            //World.Update();
        }
    }
}