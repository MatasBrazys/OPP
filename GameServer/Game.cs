using System;
using GameShared.Types;
using GameShared.Messages;

namespace GameServer
{
    // SINGLETON
    public class Game
    {
        private static readonly Lazy<Game> _instance = new(() => new Game());
        public static Game Instance => _instance.Value;

        public Server Server { get; private set; }
        public World World { get; private set; }

        // Private constructor
        private Game()
        {
            Server = new Server();
            World = new World();
        }

        public void Start()
        {
            Server.Start(5000);
        }

        public void Tick(int dt)
        {
            World.Update();
        }
    }
}