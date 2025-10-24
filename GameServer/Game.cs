using GameShared.Facades;
using GameShared.Factories;
using GameShared.Types.Map;
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
        public GameWorldFacade WorldFacade { get; private set; }

        // Private constructor
        private Game()
        {
            Server = new Server();
            World = new World();
            PlayerFactory = new PlayerFactory();
            GameObjectFactory = new GameObjectFactory();
            WorldFacade = new GameWorldFacade(World, PlayerFactory, GameObjectFactory);
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
            WorldFacade.AddObject(GameObjectFactory.CreateObject("house", 200, 200));
            WorldFacade.AddObject(GameObjectFactory.CreateObject("tree", 300, 300));
        }

        public void Tick(int dt)
        {
            WorldFacade.UpdateWorld();
            //World.Update();
        }
    }
}