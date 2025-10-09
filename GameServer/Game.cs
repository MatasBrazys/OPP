using GameShared.Types;
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
            // Find a passable tile to spawn the player
            var (x, y) = FindPassableTile();

            var player = PlayerFactory.CreatePlayer(roleType, id, x, y);
            World.AddEntity(player);
            return player;
        }

        // Helper method to find a free, passable tile
        private (int x, int y) FindPassableTile()
        {
            const int TileSize = 128;

            for (int ty = 0; ty < World.Map.Height; ty++)
            {
                for (int tx = 0; tx < World.Map.Width; tx++)
                {
                    var tile = World.Map.GetTile(tx, ty);
                    if (tile.Passable)
                    {
                        return (tx * TileSize, ty * TileSize);
                    }
                }
            }

            // Fallback if no passable tile found
            return (0, 0);
        }

        public void Tick(int dt)
        {
            //World.Update();
        }
    }
}